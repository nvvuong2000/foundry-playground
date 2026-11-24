// Ví dụ: request và response của Responses API khi dùng conversation trên Foundry.
//
// Chạy: dotnet run --project src/Examples.ConversationStorage
//
// Chương trình gọi trực tiếp REST của project (không qua Agent Framework) để bạn thấy
// đúng JSON gửi đi và nhận về. Toàn bộ request/response được ghi ra conversation-sample-output.md
// cạnh thư mục project để tham khảo.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using MasteringAgentFramework.Shared;

EnvLoader.LoadRepoRootEnv();

string endpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("Missing AZURE_AI_PROJECT_ENDPOINT in .env");
string model = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4.1-mini";

// Token cho Foundry lấy từ đăng nhập az (cùng cơ chế với Ch03).
AccessToken token = await new AzureCliCredential().GetTokenAsync(
    new TokenRequestContext(["https://ai.azure.com/.default"]), default);

using HttpClient http = new() { BaseAddress = new Uri(endpoint.TrimEnd('/') + "/") };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

StringBuilder log = new();
log.AppendLine("# Conversation sample: request và response");
log.AppendLine();
log.AppendLine($"Endpoint: {endpoint}");
log.AppendLine($"Model: {model}");
log.AppendLine($"API version: {ApiVersion}");
log.AppendLine();

// 1) Tạo conversation: một đối tượng server-side để gom các response lại thành một hội thoại.
JsonElement conversation = await Call(http, log, HttpMethod.Post, "openai/conversations", new { });
string conversationId = conversation.GetProperty("id").GetString()!;

// 2) Response đầu tiên: đưa ra một thông tin cần nhớ.
JsonElement first = await Call(http, log, HttpMethod.Post, "openai/responses", new
{
    model,
    instructions = "You are a short-answer assistant.",
    input = "My name is Alex and I prefer morning appointments. Acknowledge in one sentence.",
    conversation = conversationId,
});

// 3) Response thứ hai trong CÙNG conversation: không gửi lại câu trước, nhưng model vẫn trả lời được
//    vì platform đã lưu lịch sử trong conversation.
JsonElement second = await Call(http, log, HttpMethod.Post, "openai/responses", new
{
    model,
    input = "What is my name and when do I prefer appointments?",
    conversation = conversationId,
});

// 4) Đọc lại các item đã lưu trong conversation.
JsonElement items = await Call(http, log, HttpMethod.Get, $"openai/conversations/{conversationId}/items", null);

string outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "conversation-sample-output.md"));
await File.WriteAllTextAsync(outputPath, log.ToString());

Console.WriteLine($"Conversation: {conversationId}");
Console.WriteLine($"Response 1: {FirstText(first)}");
Console.WriteLine($"Response 2: {FirstText(second)}");
Console.WriteLine($"Items stored: {items.GetProperty("data").GetArrayLength()}");
Console.WriteLine($"Full request/response log: {outputPath}");

static async Task<JsonElement> Call(HttpClient http, StringBuilder log, HttpMethod method, string path, object? body)
{
    string url = $"{path}?api-version={ApiVersion}";
    string? requestJson = body is null ? null : JsonSerializer.Serialize(body, Pretty);

    log.AppendLine($"## {method} {path}");
    log.AppendLine();
    if (requestJson is not null)
    {
        log.AppendLine("Request body:");
        log.AppendLine("```json");
        log.AppendLine(requestJson);
        log.AppendLine("```");
    }

    using HttpRequestMessage request = new(method, url)
    {
        Content = requestJson is null ? null : new StringContent(requestJson, Encoding.UTF8, "application/json"),
    };

    using HttpResponseMessage response = await http.SendAsync(request);
    string responseText = await response.Content.ReadAsStringAsync();

    using JsonDocument doc = JsonDocument.Parse(responseText);
    string responsePretty = JsonSerializer.Serialize(doc.RootElement, Pretty);

    log.AppendLine($"Response: HTTP {(int)response.StatusCode}");
    log.AppendLine("```json");
    log.AppendLine(responsePretty);
    log.AppendLine("```");
    log.AppendLine();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"HTTP {(int)response.StatusCode} for {method} {path}: {responsePretty}");
    }

    return doc.RootElement.Clone();
}

static string FirstText(JsonElement response)
{
    foreach (JsonElement output in response.GetProperty("output").EnumerateArray())
    {
        if (output.TryGetProperty("content", out JsonElement content))
        {
            foreach (JsonElement part in content.EnumerateArray())
            {
                if (part.TryGetProperty("text", out JsonElement text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }
        }
    }

    return string.Empty;
}


public partial class Program
{
    private const string ApiVersion = "2025-05-15-preview";

    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
}
