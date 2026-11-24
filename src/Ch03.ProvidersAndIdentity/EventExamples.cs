using System.ComponentModel;
using System.Text.Json;
using MasteringAgentFramework.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MasteringAgentFramework.Ch03.ProvidersAndIdentity;

// Ví dụ các loại event (AIContent) mà agent phát ra khi chạy streaming.
//
// Mỗi AgentResponseUpdate có một danh sách Contents. Mỗi phần tử là một AIContent,
// và kiểu của nó cho biết agent vừa làm gì: nói chữ, gọi tool, xin duyệt, báo usage...
// Vòng lặp dưới đây xử lý từng loại bằng pattern matching; loại nào không quan tâm thì log tên kiểu.
public static class EventExamples
{
    public static async Task RunAllAsync()
    {
        await Section1_TextAndUsage();
        await Section2_FunctionCallAndResult();
        await Section3_ToolApproval();
        await Section4_CodeInterpreter();
        Section5_OtherEventsDocumentedOnly();
    }

    // ---------------------------------------------------------------------
    // 1) TextContent + UsageContent
    //    TextContent: mẩu text của câu trả lời, đến từng phần.
    //    UsageContent: số token của MỘT lượt gọi model. Một câu hỏi có tool có thể có nhiều UsageContent
    //    (mỗi lần model được gọi một lần). Cộng dồn để tính tổng chi phí.
    // ---------------------------------------------------------------------
    static async Task Section1_TextAndUsage()
    {
        Header("1) TextContent + UsageContent");
        AIAgent agent = BuildAgent(tools: null);

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(
            "In one sentence, what does a customer-support triage agent do?"))
        {
            foreach (AIContent content in update.Contents)
            {
                Dump(content);
            }
        }
        Console.WriteLine();
    }

    // ---------------------------------------------------------------------
    // 2) FunctionCallContent + FunctionResultContent
    //    FunctionCallContent: model yêu cầu chạy một tool, kèm tên và tham số.
    //    FunctionResultContent: kết quả tool trả về, do framework chạy và đưa lại model.
    //    Đây là hai event xuất hiện khi agent "dùng tool" (Ch04).
    // ---------------------------------------------------------------------
    static async Task Section2_FunctionCallAndResult()
    {
        Header("2) FunctionCallContent + FunctionResultContent");
        AIAgent agent = BuildAgent(tools: [AIFunctionFactory.Create(GetOrderStatus)]);

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(
            "Where is my order ORD-1001?"))
        {
            foreach (AIContent content in update.Contents)
            {
                Dump(content);
            }
        }
        Console.WriteLine();
    }

    // ---------------------------------------------------------------------
    // 3) ToolApprovalRequestContent + ToolApprovalResponseContent
    //    ApprovalRequiredAIFunction bọc một tool nguy hiểm (hủy đơn).
    //    Khi model muốn gọi tool này, framework KHÔNG chạy ngay mà phát ToolApprovalRequestContent.
    //    Ứng dụng hỏi người dùng, rồi gửi ToolApprovalResponseContent (qua CreateResponse) để tiếp tục.
    //    Quy tắc: vòng lặp phải tiếp tục cho đến khi không còn yêu cầu duyệt nào (cùng session).
    // ---------------------------------------------------------------------
    static async Task Section3_ToolApproval()
    {
        Header("3) ToolApprovalRequestContent -> ToolApprovalResponseContent");
        AIAgent agent = BuildAgent(tools:
        [
            AIFunctionFactory.Create(GetOrderStatus),
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(CancelOrder)),
        ]);

        AgentSession session = await agent.CreateSessionAsync();
        IEnumerable<ChatMessage> input = [new ChatMessage(ChatRole.User, "Please cancel order ORD-1001.")];

        while (true)
        {
            List<ToolApprovalRequestContent> requests = [];

            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(input, session))
            {
                foreach (AIContent content in update.Contents)
                {
                    Dump(content);
                    if (content is ToolApprovalRequestContent request)
                    {
                        requests.Add(request);
                    }
                }
            }
            Console.WriteLine();

            if (requests.Count == 0)
            {
                break;
            }

            // Demo: đọc y/n từ console. Không nhập gì thì coi như từ chối (an toàn mặc định).
            List<AIContent> responses = [];
            foreach (ToolApprovalRequestContent request in requests)
            {
                Console.Write("Approve? (y/n): ");
                bool approved = string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase);
                responses.Add(request.CreateResponse(approved, approved ? "Approved by reviewer." : "Rejected by reviewer."));
            }

            input = [new ChatMessage(ChatRole.User, responses)];
        }
        Console.WriteLine();
    }

    // ---------------------------------------------------------------------
    // 4) CodeInterpreterToolCallContent + CodeInterpreterToolResultContent
    //    HostedCodeInterpreterTool là tool chạy phía server (Foundry chạy code Python hộ agent).
    //    Event tương ứng: phần code được gọi (Call) và phần output (Result). Không cần tự chạy code.
    // ---------------------------------------------------------------------
    static async Task Section4_CodeInterpreter()
    {
        Header("4) CodeInterpreterToolCallContent + ToolResultContent (hosted tool)");
        AIAgent agent = BuildAgent(tools: [new HostedCodeInterpreterTool()]);

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(
            // Yêu cầu rõ ràng dùng code: model có thể tự trả lời mà không gọi tool, khi đó sẽ không có event code.
            "You must run Python code in the code interpreter to compute the 20th Fibonacci number (F(1)=F(2)=1). Do not answer from memory."))
        {
            foreach (AIContent content in update.Contents)
            {
                Dump(content);
            }
        }
        Console.WriteLine();
    }

    // ---------------------------------------------------------------------
    // 5) Các event chưa chạy trong ví dụ này (chỉ mô tả, không gọi thật).
    // ---------------------------------------------------------------------
    static void Section5_OtherEventsDocumentedOnly()
    {
        Header("5) Các event khác (mô tả)");
        Console.WriteLine("""
          ErrorContent
            Lỗi phát sinh trong luồng (ví dụ quota, timeout). Cách dùng: log và báo cho người dùng,
            đừng coi luồng là thành công chỉ vì không có exception.

          TextReasoningContent
            Phần lý luận của model (chỉ có với reasoning model như gpt-5/o-series).
            Cách dùng: ẩn mặc định; chỉ hiện khi debug vì có thể chứa thông tin nội bộ.

          InputRequestContent / InputResponseContent
            Agent cần thêm thông tin từ người dùng trước khi làm tiếp. Cách dùng: hỏi,
            rồi trả lời bằng InputResponseContent, tương tự luồng approval.

          DataContent / UriContent
            File hoặc ảnh trong phản hồi. Cách dùng: lưu file hoặc hiển thị ảnh.

          HostedWebSearchTool -> WebSearchToolCallContent / WebSearchToolResultContent
          HostedFileSearchTool -> HostedFileContent / HostedVectorStoreContent
            Tool tìm kiếm phía server. Cần cấu hình tài nguyên tương ứng trong Foundry.
          """);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    // Mọi section dùng chung một cách tạo agent, chỉ khác tools.
    static AIAgent BuildAgent(IList<AITool>? tools) =>
        AgentClientFactory.CreateFoundryProjectAgent(
            "You are a customer-support assistant. Use tools when they help. Keep replies short.",
            name: "EventDemoAgent",
            tools: tools);

    // In một event theo kiểu. Pattern matching: loại không biết thì in tên kiểu, không crash.
    static void Dump(AIContent content)
    {
        switch (content)
        {
            case TextContent text:
                Console.Write(text.Text);
                break;

            case FunctionCallContent call:
                Console.WriteLine($"\n  [FunctionCall] {call.Name}({JsonSerializer.Serialize(call.Arguments)})");
                break;

            case FunctionResultContent result:
                Console.WriteLine($"  [FunctionResult] {result.Result}");
                break;

            case ToolApprovalRequestContent approval when approval.ToolCall is FunctionCallContent pending:
                Console.WriteLine($"\n  [ApprovalRequest] {pending.Name} cần người duyệt");
                break;

            // Code do server chạy: Inputs là code (từng mẩu), Outputs là kết quả chạy code.
            case CodeInterpreterToolCallContent codeCall:
                foreach (AIContent input in codeCall.Inputs ?? [])
                {
                    if (input is TextContent code)
                    {
                        Console.Write(code.Text);
                    }
                }
                break;

            case CodeInterpreterToolResultContent codeResult:
                foreach (AIContent output in codeResult.Outputs ?? [])
                {
                    if (output is TextContent result)
                    {
                        Console.WriteLine($"\n  [CodeInterpreter.Output] {result.Text}");
                    }
                }
                break;

            case UsageContent usage:
                Console.WriteLine($"\n  [Usage] total tokens: {usage.Details.TotalTokenCount}");
                break;

            case ErrorContent error:
                Console.WriteLine($"\n  [Error] {error.Message}");
                break;

            default:
                // Event chưa xử lý: in tên để thấy nó xuất hiện, không làm gì thêm.
                Console.WriteLine($"\n  [{content.GetType().Name}]");
                break;
        }
    }

    static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"===== {title} =====");
    }

    [Description("Get the current shipping status of an order by its id.")]
    static string GetOrderStatus([Description("Order id, e.g. ORD-1001")] string orderId) =>
        $"Order {orderId}: shipped, arriving in 2 days.";

    [Description("Cancel an order. This cannot be undone.")]
    static string CancelOrder([Description("Order id, e.g. ORD-1001")] string orderId) =>
        $"Order {orderId} has been cancelled.";
}
