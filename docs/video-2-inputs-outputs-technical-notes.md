# Technical Learning Notes – Video 2: Inputs & Outputs Explained

Tài liệu phân tích kỹ thuật chuyên sâu dựa trên bài giảng **Episode 2 (Session 2)** thuộc chuỗi video *"Microsoft Agent Framework for C# Devs"* do tác giả Rodrigo trình bày. Tài liệu này bóc tách chi tiết bản chất kiến trúc, cơ chế xử lý I/O (Inputs/Outputs), tối ưu hóa Reasoning Effort, tiêu thụ luồng dữ liệu (Streaming), và kỹ thuật xử lý đa phương thức (Multimodal Ingestion) trong môi trường .NET 10 / C# 14.

---

## 01. What You Will Learn

### Mục tiêu kỹ thuật
- **Nắm vững các Primitives I/O cốt lõi**: Hiểu rõ cấu trúc và vòng đời của `ChatMessage`, `AgentResponse`, `AgentResponseUpdate` và mối liên hệ với không gian tên `Microsoft.Extensions.AI`.
- **Làm chủ 3 chế độ thực thi (Execution Modes)**: Phân biệt và làm chủ `RunAsync`, `RunAsync<T>` (Structured Output) và `RunStreamingAsync` (Streaming mode).
- **Kiểm soát & Tối ưu hóa Reasoning Effort**: Giải quyết bài toán độ trễ (latency) và lãng phí token khi làm việc với các mô hình suy luận (Reasoning Models như `gpt-5-nano` hoặc họ mô hình `o1`/`o3`) bằng cách cấu hình `ChatReasoningEffortLevel.Minimal` thông qua `RawRepresentationFactory`.
- **Xử lý Đa phương thức (Multimodal Processing)**: Xây dựng thông điệp chứa nội dung hỗn hợp (văn bản + file nhị phân PDF/Hình ảnh) thông qua lớp `DataContent` và truyền trực tiếp vào Context Window của LLM.

### Những vấn đề bài giảng giải quyết
1. **Hiện tượng phản hồi chậm và tiêu tốn token vô ích**: Khi gọi mô hình suy luận (Reasoning Models) cho các câu hỏi tra cứu dữ liệu đơn giản, mô hình mặc định sinh ra hàng ngàn `ReasoningTokenCount` gây ra độ trễ từ 8–10 giây.
2. **Khả năng bóc tách dữ liệu có cấu trúc (Structured Output)**: Ép kiểu phản hồi từ LLM về các C# `record` hoặc `class` mạnh mẽ thay vì nhận chuỗi văn bản thô (Plain Text/JSON).
3. **Bắt telemetry sử dụng token trong chế độ Streaming**: Trích xuất chi tiết token (`UsageDetails`) khi dữ liệu trả về theo dạng luồng `IAsyncEnumerable<AgentResponseUpdate>` bằng cách kiểm tra kiểu `UsageContent`.
4. **Đưa tài liệu nhị phân dung lượng lớn vào LLM**: Nạp trực tiếp file PDF (ví dụ tài liệu 9 MB) vào tham số thông điệp mà không cần thiết lập hạ tầng Vector Database hay RAG phức tạp.

### Kiến thức và kỹ năng đạt được
- Khả năng thiết lập cấu hình tùy chỉnh ở cả hai cấp độ: **Agent Level** (`ChatClientAgentOptions`) và **Run Level** (`ChatClientAgentRunOptions`).
- Kỹ năng kiểm soát luồng bất đồng bộ với `await foreach` kết hợp kiểm tra polymorphic của mảng `IList<AIContent>`.
- Khả năng đọc và phân tích bảng số liệu `UsageDetails` (`InputTokenCount`, `OutputTokenCount`, `ReasoningTokenCount`, `TotalTokenCount`).

---

## 02. Core Concepts

### 1. `ChatMessage` và `ChatRole`
Trong Microsoft Agent Framework, `ChatMessage` đại diện cho đối tượng đầu vào cơ bản nhất truyền tới Agent. 
- **Cấu trúc**: Mỗi `ChatMessage` bao gồm một thuộc tính vai trò `Role` (`ChatRole`) và danh sách các phần tử nội dung `Contents` (`IList<AIContent>`).
- **Các vai trò chuẩn (`ChatRole`)**:
  - `ChatRole.User`: Đầu vào do người dùng cuối hoặc hệ thống caller gửi tới.
  - `ChatRole.System`: Câu lệnh hệ thống (System Prompt/Meta Prompt) định hình nhân dạng, hành vi và quy tắc của Agent.
  - `ChatRole.Assistant`: Phản hồi được sinh ra từ mô hình ngôn ngữ lớn.

### 2. Các chế độ thực thi của Agent (`Execution Modes`)
Agent Framework hỗ trợ 3 phương thức thực thi chính trên lớp `AIAgent`:
- **`RunAsync(ChatMessage message)`**: Thực thi đồng bộ hóa bộ đệm (buffered execution). Phản hồi toàn bộ văn bản sau khi LLM hoàn tất quá trình sinh.
- **`RunAsync<T>(ChatMessage message)`**: Thực thi ép kiểu có cấu trúc (Structured Output). Mô hình bắt buộc phải tuân theo JSON Schema được tự động sinh từ kiểu `T` (C# `record` hoặc `class`), sau đó Framework tự động deserialize thành đối tượng C#.
- **`RunStreamingAsync(ChatMessage message)`**: Thực thi theo luồng bất đồng bộ. Trả về `IAsyncEnumerable<AgentResponseUpdate>`, cho phép ứng dụng nhận token ngay khi mô hình vừa tạo ra (giao diện gõ chữ thời gian thực).

### 3. Phân biệt `AgentResponse` và `AgentResponseUpdate`
- **`AgentResponse`**: Đối tượng kết quả trả về từ `RunAsync` hoặc `RunAsync<T>`. Đối tượng này đóng gói toàn bộ văn bản (`.Text`), đối tượng dữ liệu đã deserialize (`.Result`), danh sách thông điệp (`.Messages`) và thông tin token telemetry (`.Usage`).
- **`AgentResponseUpdate`**: Phần tử dữ liệu nhỏ (chunk/token) được phát ra từ luồng `RunStreamingAsync`. Mỗi `AgentResponseUpdate` chứa thuộc tính `Contents` (`IList<AIContent>`). 

### 4. `UsageDetails` & Token Telemetry
Đối tượng `UsageDetails` đóng vai trò đo lường và giám sát lượng tài nguyên tiêu thụ trên từng lượt gọi API:
- `InputTokenCount`: Số lượng token đầu vào (bao gồm System Prompt, lịch sử hội thoại, văn bản người dùng và dữ liệu nhị phân đa phương thức).
- `OutputTokenCount`: Số lượng token văn bản được mô hình sinh ra ở đầu ra.
- `ReasoningTokenCount`: Số lượng token mô hình sử dụng nội bộ để "suy luận" (Thinking/Reasoning process) trước khi trả về câu trả lời chính thức.
- `TotalTokenCount`: Tổng số token tiêu thụ (`Input + Output + Reasoning`).

### 5. Quản lý cấu hình Options: Agent Level vs. Run Level
Framework cho phép truyền các thông số cấu hình (`ChatOptions`) tại hai cấp độ:
- **Agent Level (`ChatClientAgentOptions`)**: Áp dụng mặc định cho tất cả các lượt gọi của Agent đó.
- **Run Level (`ChatClientAgentRunOptions`)**: Đè hoặc hợp nhất (merge) cấu hình cho một lượt chạy cụ thể.
Nó cho phép can thiệp sâu vào các tham số của Open API SDK thông qua ủy quyền `RawRepresentationFactory`.

### 6. Điều chỉnh Reasoning Effort (`ReasoningEffortLevel`)
Đối với các dòng mô hình Reasoning (như OpenAI `gpt-5-nano`, `o1`, `o3`), mô hình có xu hướng tự động suy luận sâu ngay cả với câu hỏi tra cứu đơn giản. Việc gán `ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal` thông qua đối tượng `ChatCompletionOptions` giúp vô hiệu hóa hoặc giảm tối thiểu số token suy luận, đưa `ReasoningTokenCount` về 0 và giảm độ trễ từ hàng giây xuống mili-giây.

### 7. Hệ thống lớp `AIContent` (Polymorphic Content Hierarchy)
Tất cả dữ liệu trao đổi trong thông điệp đều kế thừa từ lớp trừu tượng `AIContent` (thuộc namespace `Microsoft.Extensions.AI`):
- `TextContent`: Đại diện cho chuỗi văn bản thông thường.
- `UsageContent`: Đối tượng đặc biệt xuất hiện ở cuối luồng Streaming, chứa thuộc tính `Details` (`UsageDetails`).
- `DataContent`: Đối tượng chứa dữ liệu nhị phân (Binary Data) kèm theo định dạng MIME (`mediaType`).
- `FunctionCallContent` & `FunctionResultContent`: Đại diện cho yêu cầu gọi hàm và kết quả trả về từ hàm (chi tiết ở Episode 5).

### 8. So sánh: Trực tiếp nạp Context Window (Direct Ingestion) vs. RAG (Retrieval-Augmented Generation)

Bảng dưới đây so sánh hai hướng tiếp cận khi xử lý tri thức tài liệu ngoài trong ứng dụng AI Agent:

| Tiêu chí | Trực tiếp nạp Context Window (`DataContent`) | Retrieval-Augmented Generation (RAG) |
| :--- | :--- | :--- |
| **Cơ chế hoạt động** | Truyền toàn bộ dữ liệu file (PDF/Image) vào `ChatMessage` trong một lần gọi API. | Tìm kiếm ngữ cảnh phù hợp qua Vector Store/Embeddings rồi mới nạp vào Prompt. |
| **Độ phức tạp hạ tầng** | Rất thấp (Không cần Vector DB, Chunks, Embeddings model). | Cao (Cần Chunking, Embedding pipeline, Vector Database, Indexing). |
| **Giới hạn dung lượng** | Phụ thuộc vào giới hạn Context Window của LLM (ví dụ `gpt-5-nano` hỗ trợ 400,000 tokens). | Không giới hạn tổng dung lượng tài nguyên (nhờ trích xuất từng đoạn). |
| **Độ chính xác ngữ cảnh** | Tuyệt đối trên toàn bộ tài liệu được nạp (LLM nhìn thấy toàn văn). | Phụ thuộc vào độ chính xác của tìm kiếm ngữ nghĩa (Semantic Search). |
| **Chi phí Token/Lượt gọi** | Cao nếu file dung lượng lớn (ví dụ file 9 MB tiêu tốn ~80,000 Input Tokens). | Thấp trên mỗi lượt gọi (chỉ nạp các chunk liên quan). |

---

## 03. Technical Architecture

### Kiến trúc tổng thể dữ liệu Inputs & Outputs

Luồng xử lý dữ liệu trong Microsoft Agent Framework dựa trên sự kết hợp giữa hai không gian tên: `Microsoft.Agents.AI` (Agent Abstractions & Execution) và `Microsoft.Extensions.AI` (Data Primitives & I/O Contracts).

Các thành phần dữ liệu đi từ mã nguồn ứng dụng qua lớp trừu tượng `AIAgent`, gửi yêu cầu tới dịch vụ LLM Provider, và nhận phản hồi dưới các dạng dữ liệu khác nhau tùy theo phương thức thực thi được lựa chọn.

- **Luồng 1 (Structured Output)**: `ChatMessage` -> `AIAgent.RunAsync<T>()` -> LLM Provider -> `AgentResponse<T>` (Tự động deserialize dữ liệu có cấu trúc).
- **Luồng 2 (Streaming Execution)**: `ChatMessage` -> `AIAgent.RunStreamingAsync()` -> LLM Provider -> `IAsyncEnumerable<AgentResponseUpdate>` (Nhận từng chunk `TextContent` và đóng lại bằng `UsageContent`).
- **Luồng 3 (Multimodal Ingestion)**: `ChatMessage` (chứa `TextContent` + `DataContent`) -> `AIAgent.RunStreamingAsync()` -> LLM Provider (Nạp toàn bộ context) -> Trả về luồng phản hồi dựa trên nội dung file nhị phân.

### Bảng so sánh các chế độ thực thi (Execution Modes Matrix)

| Đặc tính | `RunAsync` | `RunAsync<T>` | `RunStreamingAsync` |
| :--- | :--- | :--- | :--- |
| **Kiểu trả về** | `Task<AgentResponse>` | `Task<AgentResponse<T>>` | `IAsyncEnumerable<AgentResponseUpdate>` |
| **Cơ chế nhận dữ liệu** | Đợi toàn bộ văn bản hoàn tất (Buffered) | Đợi toàn bộ văn bản và Deserialize thành đối tượng `T` | Nhận từng phần tử (Token-by-Token / Chunked) |
| **Trải nghiệm người dùng** | Phù hợp cho batch processing hoặc background tasks | Phù hợp cho API tích hợp, trao đổi dữ liệu JSON giữa các hệ thống | Phù hợp cho UI Chatbot tương tác trực tiếp |
| **Cách đọc Telemetry Usage** | Truy cập trực tiếp qua `response.Usage` | Truy cập trực tiếp qua `response.Usage` | Đọc đối tượng `UsageContent` ở cuối luồng `IAsyncEnumerable` |

---

## 04. Implementation Walkthrough

Dưới đây là từng bước triển khai kỹ thuật chi tiết theo đúng nội dung mã nguồn được trình bày trong Video 2. Tất cả mã nguồn đều được đánh dấu nguồn gốc trực tiếp **From Video**.

### Bước 1: Khởi tạo dự án và cấu hình Package Dependencies
Đảm bảo dự án .NET 10 Console Application tham chiếu đến gói Nuget tương thích.

```csharp
// From Video: Đăng ký package Microsoft.Agents.AI.OpenAI (phiên bản Release Candidate 3)
// File: MyFirstAgent.csproj
/*
<ItemGroup>
    <PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-rc3" />
</ItemGroup>
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
```

---

### Bước 2: Tạo đối tượng Agent và thực thi dữ liệu có cấu trúc (`RunAsync<T>`)
Trong bước này, chúng ta định nghĩa một C# `record` để hứng dữ liệu có cấu trúc và gọi `RunAsync<T>`.

```csharp
// From Video: Định nghĩa Record để hứng kết quả có cấu trúc
public record LargestCity(string Name, long Population);

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Lấy API Key từ biến môi trường
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string model = "gpt-5-nano";

        // 2. Khởi tạo OpenAI Client và ChatClient
        OpenAIClient openAIClient = new OpenAIClient(apiKey);
        ChatClient chatClient = openAIClient.GetChatClient(model);

        // 3. Tạo Agent thông qua Extension Method asAIAgent()
        AIAgent agent = chatClient.AsAIAgent(
            instructions: "You are a helpful assistant.",
            name: "CityAgent"
        );

        // 4. Chuẩn bị prompt và ChatMessage đầu vào
        string prompt = "What are the most populated cities in the world? Respond only with name and total population.";
        ChatMessage chatMessage = new ChatMessage(ChatRole.User, prompt);

        Console.WriteLine("Executing RunAsync<T>...");

        // 5. Gọi RunAsync<T> để yêu cầu LLM trả về đúng schema đại diện bởi IEnumerable<LargestCity>
        AgentResponse<IEnumerable<LargestCity>> response = await agent.RunAsync<IEnumerable<LargestCity>>(chatMessage);

        // 6. Đọc kết quả đã được Deserialize tự động
        foreach (var city in response.Result)
        {
            Console.WriteLine($"City: {city.Name} | Population: {city.Population:N0}");
        }
    }
}
```
- **Input**: Chuỗi prompt chứa yêu cầu danh sách các thành phố đông dân nhất.
- **Output**: Tập hợp `IEnumerable<LargestCity>` với các thuộc tính `Name` và `Population` được parse chính xác.

---

### Bước 3: Đọc dữ liệu Telemetry Token Usage
Hàm trợ giúp in số liệu token tiêu thụ từ đối tượng `UsageDetails`.

```csharp
// From Video: Hàm trợ giúp in chi tiết Telemetry Token
public static void PrintUsage(UsageDetails usage)
{
    if (usage == null) return;

    Console.WriteLine($"Total Token Count:     {usage.TotalTokenCount}");
    Console.WriteLine($"Input Token Count:     {usage.InputTokenCount}");
    Console.WriteLine($"Output Token Count:    {usage.OutputTokenCount}");
    Console.WriteLine($"Reasoning Token Count: {usage.ReasoningTokenCount}");
}
```

---

### Bước 4: Tối ưu hóa độ trễ bằng cách hạ cấp `ReasoningEffortLevel`
Tác giả Rodrigo phát hiện rằng mô hình `gpt-5-nano` tốn nhiều thời gian suy luận (Reasoning Tokens) cho các câu hỏi tra cứu cơ bản. Việc bổ sung `ChatClientAgentRunOptions` với `ReasoningEffortLevel.Minimal` giải quyết triệt để vấn đề này.

```csharp
// From Video: Cấu hình ChatClientAgentRunOptions để vô hiệu hóa / giảm tối thiểu Reasoning Effort
var runOptions = new ChatClientAgentRunOptions
{
    ChatOptions = new ChatOptions
    {
        // Sử dụng RawRepresentationFactory để cấu hình sâu vào tham số của OpenAI SDK
        RawRepresentationFactory = () => new ChatCompletionOptions
        {
            ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal
        }
    }
};

// Thực thi với Options đã được tối ưu
AgentResponse<IEnumerable<LargestCity>> optimizedResponse = await agent.RunAsync<IEnumerable<LargestCity>>(
    chatMessage, 
    options: runOptions
);

// In kết quả và đo lường token
foreach (var city in optimizedResponse.Result)
{
    Console.WriteLine($"City: {city.Name} | Population: {city.Population:N0}");
}

// In số liệu Token: ReasoningTokenCount sẽ giảm về 0, thời gian phản hồi tức thì
PrintUsage(optimizedResponse.Usage);
```

---

### Bước 5: Tiêu thụ luồng dữ liệu (Streaming Mode) & Nhận biết `UsageContent`
Triển khai `RunStreamingAsync` kết hợp với kiểm tra kiểu `UsageContent` để nhận số liệu token khi luồng kết thúc.

```csharp
// From Video: Thực thi luồng RunStreamingAsync và bắt sự kiện UsageContent
Console.WriteLine("\n--- Starting Streaming Mode ---");

await foreach (AgentResponseUpdate item in agent.RunStreamingAsync(chatMessage, options: runOptions))
{
    // Kiểm tra danh sách Contents trong phần tử phát ra
    if (item.Contents != null && item.Contents.Any())
    {
        // Nếu phần tử đầu tiên là UsageContent, tức là luồng vừa kết thúc và gửi thông số Telemetry
        if (item.Contents.FirstOrDefault() is UsageContent usageContent)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n--- Streaming Completed. Token Usage Details ---");
            PrintUsage(usageContent.Details);
            Console.ResetColor();
        }
        else
        {
            // Ngược lại, đây là phần tử văn bản thông thường (TextContent)
            Console.Write(item.Text);
        }
    }
}
```

---

### Bước 6: Xử lý Đa phương thức (Multimodal Payload - File PDF 9 MB)
Tác giả Rodrigo minh họa việc nạp toàn bộ cuốn sách hướng dẫn game *"Eye of the Beholder Clue Book"* (file PDF 9 MB) trực tiếp vào Agent thông qua `DataContent`.

```csharp
// From Video: Xử lý file nhị phân PDF và gửi kèm Prompt trong ChatMessage
string pdfFilePath = "eye_of_the_beholder_clue_book.pdf";

// 1. Đọc toàn bộ mảng byte của file PDF 9 MB
byte[] pdfBytes = await File.ReadAllBytesAsync(pdfFilePath);
ReadOnlyMemory<byte> bytesMemory = new ReadOnlyMemory<byte>(pdfBytes);

// 2. Tạo đối tượng DataContent với MIME Type "application/pdf"
DataContent pdfDataContent = new DataContent(bytesMemory, "application/pdf");

// 3. Tạo câu hỏi liên quan đến nội dung trong sách (nhân vật Tyra)
TextContent questionTextContent = new TextContent("Who is Tyra?");

// 4. Đóng gói cả hai phần tử AIContent vào một ChatMessage duy nhất
ChatMessage multimodalMessage = new ChatMessage(
    ChatRole.User,
    new List<AIContent> { questionTextContent, pdfDataContent }
);

Console.WriteLine($"\nSending Multimodal Payload ({pdfBytes.Length / (1024 * 1024)} MB PDF + Question)...");

// 5. Gửi thông thông điệp đa phương thức qua luồng RunStreamingAsync
await foreach (AgentResponseUpdate item in agent.RunStreamingAsync(multimodalMessage, options: runOptions))
{
    if (item.Contents != null && item.Contents.Any())
    {
        if (item.Contents.FirstOrDefault() is UsageContent usageContent)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n--- Multimodal Stream Completed ---");
            PrintUsage(usageContent.Details);
            Console.ResetColor();
        }
        else
        {
            Console.Write(item.Text);
        }
    }
}
```

#### Kết quả thực thi thực tế từ bài giảng:
- **Câu hỏi**: `"Who is Tyra?"` (Nếu không có file PDF, LLM trả lời không biết Tyra là ai).
- **Phản hồi từ Agent**: Trích xuất chính xác thông tin nhân vật Tyra trong cuốn sách: `"Tyra appears as a character in the Eye of the Beholder clue book. Tyra is a Level 6 Male Elf Ranger, Neutral Good..."`
- **Telemetry Token Output**:
  - `InputTokenCount`: **~80,000 tokens** (do nạp toàn bộ nội dung file PDF 9 MB vào Context Window).
  - `OutputTokenCount`: **~431 tokens**.
  - `ReasoningTokenCount`: **0 tokens** (nhờ đã cấu hình `ReasoningEffortLevel.Minimal`).

---

## 05. Engineering Insights

Phân tích bổ sung từ góc nhìn Software Engineer và Architect đối với các giải pháp được trình bày trong Video 2.

### 1. Tại sao kiến trúc này được lựa chọn?
- **Additional Engineering Insights**: Việc thiết kế `ChatMessage.Contents` dưới dạng `IList<AIContent>` cho phép Agent Framework mở rộng không giới hạn các loại dữ liệu đầu vào và đầu ra. Dù là văn bản, ảnh, âm thanh, video hay file PDF, API xử lý nhất quán mà không phải thay đổi chữ ký hàm (Method Signature).
- **Additional Engineering Insights**: Kế thừa không gian tên `Microsoft.Extensions.AI` giúp mã nguồn C# độc lập với các thư viện SDK của vendor (như OpenAI SDK hay Azure SDK).

### 2. Ưu điểm, Hạn chế và Trade-offs

#### Ưu điểm:
- **Additional Engineering Insights**: Đơn giản hóa hạ tầng khi xử lý tài liệu vừa và nhỏ. Việc nạp thẳng file PDF 9 MB qua `DataContent` loại bỏ hoàn toàn nhu cầu dựng dịch vụ Embeddings, Vector Database hay thuật toán Chunking/Search.
- **Additional Engineering Insights**: Tính linh hoạt cao giữa Streaming và Buffered Execution. Dễ dàng chuyển đổi giữa trả về đối tượng C# có cấu trúc (`RunAsync<T>`) cho API backend và dạng luồng (`RunStreamingAsync`) cho UI frontend.

#### Hạn chế & Trade-offs:
- **Additional Engineering Insights - Chi phí Token khi nạp Context trực tiếp**:
  - *Trade-off*: Nạp file PDF 9 MB tốn gần **80,000 Input Tokens** cho một câu hỏi duy nhất. Nếu người dùng hỏi 10 câu liên tiếp mà không có cơ chế lưu vết (Cache) hoặc RAG, hệ thống sẽ tiêu tốn 800,000 tokens — gây bùng nổ chi phí API.
- **Additional Engineering Insights - Trạng thái bộ nhớ (Memory Allocation)**:
  - Việc đọc toàn bộ file 9 MB vào mảng `byte[]` rồi tạo `ReadOnlyMemory<byte>` cần được quản lý cẩn thận trong các ứng dụng Web API có hàng ngàn request đồng thời để tránh gây áp lực lên Garbage Collector (GC Large Object Heap - LOH).

### 3. Cân nhắc khi đưa vào Production (Production Readiness)
- **Additional Engineering Insights - Quản lý Token Budget & Rate Limits**:
  - Cần đặt ngưỡng cảnh báo hoặc giới hạn dung lượng file nhị phân truyền vào `DataContent`. Với mô hình có Context Window 400,000 tokens (như `gpt-5-nano`), một file 30 MB có thể vượt quá giới hạn token cho phép của một request.
- **Additional Engineering Insights - Tối ưu hóa Cấu hình Options**:
  - Luôn luôn cấu hình `ReasoningEffortLevel = Minimal` đối với các tác vụ tra cứu dữ liệu tĩnh hoặc trích xuất văn bản đơn giản. Chỉ bật `Medium` hoặc `High` đối với các bài toán lập luận logic phức tạp, giải toán hoặc lập trình.
- **Additional Engineering Insights - Bắt lỗi Nullable & Stream Termination trong Streaming**:
  - Thuộc tính `item.Contents` trong `AgentResponseUpdate` có thể chứa phần tử rỗng hoặc không thuộc kiểu `TextContent`/`UsageContent`. Mã sản xuất bắt buộc phải kiểm tra `null` và ép kiểu an toàn (`is UsageContent`) để tránh hiện tượng `NullReferenceException` gây ngắt luồng.
- **Additional Engineering Insights - Bảo mật dữ liệu nhị phân**:
  - Các file PDF/Hình ảnh chứa thông tin PII (Personally Identifiable Information) khi gửi trực tiếp qua `DataContent` tới API của bên thứ ba (OpenAI) cần đảm bảo tuân thủ các quy định về quyền riêng tư và chính sách lưu trữ dữ liệu của doanh nghiệp.

---

## 06. Further Learning

### 1. Kiến thức nền tảng cần hiểu sâu hơn
- **C# Async Streams & `IAsyncEnumerable<T>`**: Nắm vững cơ chế `await foreach` và cách `CancellationToken` truyền qua luồng bất đồng bộ trong C# 8+.
- **Quản lý bộ nhớ tối ưu với `System.Memory`**: Hiểu cách hoạt động của `Span<T>`, `Memory<T>`, `ReadOnlyMemory<byte>` để tối ưu hóa việc đọc file nhị phân dung lượng lớn mà không tạo rác trên Heap.
- **MIME Types & Multimodal Data Specifications**: Tìm hiểu danh sách các MIME types hỗ trợ bởi từng dòng mô hình (ví dụ: OpenAI Vision hỗ trợ `image/png`, `image/jpeg`; Document Processing hỗ trợ `application/pdf`).

### 2. Các chủ đề liên quan nên nghiên cứu tiếp
- **Session 3: State and Memory**: Cách duy trì trạng thái hội thoại qua `AgentSession`, `StateBag` và nén lịch sử qua `Compaction Pipeline`.
- **Session 4: Middleware Explained**: Cách chèn các thành phần kiểm duyệt, ghi log hoặc chặn request/response ở cấp độ Agent và ChatClient.
- **Session 5: Tool Calling / Function Calling**: Cách đăng ký C# methods thành công cụ cho LLM thông qua `AIFunctionFactory`.

---

### 3. Câu hỏi kỹ thuật tự kiểm tra (Technical Self-Check Questions)

#### Câu 1: Làm thế nào để cấu hình giảm thiểu số token suy luận (Reasoning Tokens) cho một lượt thực thi cụ thể của Agent?
*Trả lời*: Khởi tạo đối tượng `ChatClientAgentRunOptions`, gán đối tượng `ChatOptions` với thuộc tính `RawRepresentationFactory` trả về `ChatCompletionOptions` có `ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal`, sau đó truyền `runOptions` vào tham số `options` của phương thức `RunAsync` hoặc `RunStreamingAsync`.

#### Câu 2: Trong chế độ Streaming (`RunStreamingAsync`), làm thế nào để phát hiện luồng đã kết thúc và lấy thông tin tổng số token đã tiêu thụ?
*Trả lời*: Lặp qua luồng `IAsyncEnumerable<AgentResponseUpdate>` bằng `await foreach`. Kiểm tra xem phần tử trong `item.Contents` có phải là kiểu `UsageContent` hay không (`if (item.Contents.FirstOrDefault() is UsageContent usageContent)`). Nếu đúng, đọc thuộc tính `usageContent.Details` (`UsageDetails`) để lấy số liệu token.

#### Câu 3: Điểm khác biệt cơ bản giữa `RunAsync` và `RunAsync<T>` là gì?
*Trả lời*: `RunAsync` trả về văn bản phản hồi dạng chuỗi tự do (Plain Text), trong khi `RunAsync<T>` bắt buộc LLM trả về dữ liệu tuân theo JSON Schema sinh ra từ kiểu `T`, sau đó tự động deserialize thành instance của kiểu `T` trong C#.

#### Câu 4: Khi truyền một file PDF dung lượng 9 MB vào `ChatMessage`, lớp nào được sử dụng để đóng gói dữ liệu nhị phân này?
*Trả lời*: Lớp `DataContent` (kế thừa từ `AIContent`). Nó tiếp nhận mảng byte dưới dạng `ReadOnlyMemory<byte>` và chuỗi MIME type (ví dụ: `"application/pdf"`).

#### Câu 5: Sự đè/hợp nhất cấu hình giữa Agent Level (`ChatClientAgentOptions`) và Run Level (`ChatClientAgentRunOptions`) diễn ra như thế nào?
*Trả lời*: Các cấu hình thiết lập ở Run Level sẽ đè (override) hoặc hợp nhất (merge) với các cấu hình tương ứng đã thiết lập ở Agent Level đối với riêng lượt chạy đó, giúp tùy biến linh hoạt mà không làm thay đổi trạng thái mặc định của Agent.

---

### 4. Bài tập thực hành (Hands-on Exercises)

#### Bài tập 1: Tối ưu hóa API trích xuất JSON thông tin sách
*Yêu cầu*: 
1. Tạo một C# `record BookInfo(string Title, string Author, int PublishedYear, string[] KeyThemes)`.
2. Tạo một Agent sử dụng mô hình `gpt-5-nano`.
3. Cấu hình `ChatClientAgentRunOptions` để ép `ReasoningEffortLevel = Minimal`.
4. Gửi prompt yêu cầu thông tin về một cuốn sách kinh điển (ví dụ: *"Clean Code by Robert C. Martin"*).
5. Sử dụng `RunAsync<BookInfo>` để lấy dữ liệu có cấu trúc và in kết quả telemetry ra màn hình.

#### Bài tập 2: Phân tích file ảnh đa phương thức kết hợp Streaming
*Yêu cầu*:
1. Chuẩn bị một file ảnh (`diagram.png` hoặc `chart.jpg`).
2. Đọc file ảnh thành `DataContent` với MIME type tương ứng (`"image/png"` hoặc `"image/jpeg"`).
3. Tạo `ChatMessage` chứa `TextContent("Explain the architecture shown in this image in detail.")` và `DataContent` vừa tạo.
4. Gọi `RunStreamingAsync`, hiển thị kết quả giải thích dạng gõ chữ thời gian thực.
5. Kiểm tra và in số liệu `InputTokenCount` và `OutputTokenCount` khi gặp `UsageContent` ở cuối luồng.
