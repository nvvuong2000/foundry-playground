// Vid02 - Microsoft Agent Framework: Inputs & Outputs
//
// Implements docs/video-2-inputs-outputs-technical-notes.md, section "04. Implementation Walkthrough":
//   Step 2  RunAsync<T>          structured output into C# records
//   Step 3  PrintUsage           token telemetry from UsageDetails
//   Step 4  Reasoning effort     ChatClientAgentRunOptions + RawRepresentationFactory
//   Step 5  RunStreamingAsync    streaming and the UsageContent at the end of the stream
//   Step 6  Multimodal           DataContent (PDF) inside a ChatMessage
//   Exercise 1  RunAsync<BookInfo> with minimal reasoning
//   Exercise 2  image analysis with streaming
//
// Run (from the repository root):
//   dotnet run --project src/Vid02.InputsOutputs -- [path-to.pdf] [path-to.png|jpg]
// The PDF and image can also come from VID02_PDF_FILE and VID02_IMAGE_FILE.
// Set VID02_DEPLOYMENT to point the demo at a different chat deployment (for example a reasoning model).

using System.Diagnostics;
using MasteringAgentFramework.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

Console.OutputEncoding = System.Text.Encoding.UTF8;
EnvLoader.LoadRepoRootEnv();

// Deviation from the video: the video calls OpenAI directly with gpt-5-nano. This repo reaches the model
// through IChatClient (Azure OpenAI), so the same agent code runs against whatever deployment .env names.
string? deploymentOverride = Environment.GetEnvironmentVariable("VID02_DEPLOYMENT");
if (!string.IsNullOrWhiteSpace(deploymentOverride))
{
    Environment.SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT", deploymentOverride);
}

string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "(unset)";
bool isReasoningModel = IsReasoningModel(deployment);

Console.WriteLine("=== Vid02: Inputs & Outputs ===");
Console.WriteLine($"Deployment: {deployment} | Reasoning model: {isReasoningModel}");

// ---- Step 2: create the agent --------------------------------------------------------------------------
IChatClient chatClient = AgentClientFactory.CreateChatClient();
AIAgent agent = chatClient.AsAIAgent(instructions: "You are a helpful assistant.", name: "CityAgent");

const string prompt = "What are the 5 most populated cities in the world? Respond only with name and total population.";
ChatMessage chatMessage = new(ChatRole.User, prompt);

// ---- Step 4: run-level options that lower reasoning effort ---------------------------------------------
// ChatClientAgentRunOptions overrides/merges onto the agent-level options for this single run.
// RawRepresentationFactory reaches the vendor SDK type (OpenAI ChatCompletionOptions) directly.
// Only reasoning models accept this setting, so a non-reasoning deployment skips it.
#pragma warning disable OPENAI001 // OpenAI marks ReasoningEffortLevel as evaluation-only and subject to change.
ChatClientAgentRunOptions? minimalReasoning = isReasoningModel
    ? new ChatClientAgentRunOptions(new ChatOptions
    {
        RawRepresentationFactory = _ => new OpenAI.Chat.ChatCompletionOptions
        {
            ReasoningEffortLevel = OpenAI.Chat.ChatReasoningEffortLevel.Minimal
        }
    })
    : null;
#pragma warning restore OPENAI001

// ---- Step 2: RunAsync<T> (structured output) -----------------------------------------------------------
await Section("[Step 2] RunAsync<IEnumerable<LargestCity>>  (structured output)", async () =>
{
    Stopwatch sw = Stopwatch.StartNew();
    AgentResponse<IEnumerable<LargestCity>> response = await agent.RunAsync<IEnumerable<LargestCity>>(chatMessage);
    sw.Stop();

    foreach (LargestCity city in response.Result ?? [])
    {
        Console.WriteLine($"City: {city.Name} | Population: {city.Population:N0}");
    }

    Console.WriteLine($"Elapsed: {sw.Elapsed.TotalSeconds:F1}s");
    PrintUsage(response.Usage);
});

// ---- Step 4: same call with minimal reasoning ----------------------------------------------------------
await Section("[Step 4] RunAsync<T> with ReasoningEffortLevel.Minimal", async () =>
{
    if (minimalReasoning is null)
    {
        Console.WriteLine($"Skipped: '{deployment}' is not a reasoning model, so there is no reasoning effort to lower.");
        Console.WriteLine("Set VID02_DEPLOYMENT to a reasoning deployment (gpt-5*, o1, o3, o4) to compare Step 2 and Step 4.");
        return;
    }

    Stopwatch sw = Stopwatch.StartNew();
    AgentResponse<IEnumerable<LargestCity>> response =
        await agent.RunAsync<IEnumerable<LargestCity>>(chatMessage, options: minimalReasoning);
    sw.Stop();

    foreach (LargestCity city in response.Result ?? [])
    {
        Console.WriteLine($"City: {city.Name} | Population: {city.Population:N0}");
    }

    Console.WriteLine($"Elapsed: {sw.Elapsed.TotalSeconds:F1}s");
    PrintUsage(response.Usage); // ReasoningTokenCount is expected to drop towards 0
});

// ---- Step 5: RunStreamingAsync + UsageContent ----------------------------------------------------------
await Section("[Step 5] RunStreamingAsync  (streaming + UsageContent)", async () =>
{
    await StreamAsync(agent.RunStreamingAsync(chatMessage, options: minimalReasoning), "Streaming Completed");
});

// ---- Step 6: multimodal payload (PDF) ------------------------------------------------------------------
await Section("[Step 6] Multimodal: PDF as DataContent", async () =>
{
    string? pdfPath = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault()
        ?? Environment.GetEnvironmentVariable("VID02_PDF_FILE");
    await RunMultimodalAsync(agent, minimalReasoning, pdfPath, "Who is Tyra?", "PDF");
});

// ---- Exercise 1: structured output into BookInfo --------------------------------------------------------
await Section("[Exercise 1] RunAsync<BookInfo>  (structured output + telemetry)", async () =>
{
    AIAgent bookAgent = chatClient.AsAIAgent(instructions: "You are a helpful librarian.", name: "BookAgent");
    ChatMessage bookMessage = new(ChatRole.User,
        "Give me information about the book 'Clean Code' by Robert C. Martin.");

    AgentResponse<BookInfo> response = await bookAgent.RunAsync<BookInfo>(bookMessage, options: minimalReasoning);

    BookInfo? book = response.Result;
    if (book is null)
    {
        Console.WriteLine("No structured result returned.");
        return;
    }

    Console.WriteLine($"Title: {book.Title}");
    Console.WriteLine($"Author: {book.Author}");
    Console.WriteLine($"Published: {book.PublishedYear}");
    Console.WriteLine($"Key themes: {string.Join("; ", book.KeyThemes)}");
    PrintUsage(response.Usage);
});

// ---- Exercise 2: image analysis with streaming ----------------------------------------------------------
await Section("[Exercise 2] Multimodal: image as DataContent", async () =>
{
    string? imagePath = Environment.GetCommandLineArgs().Skip(2).FirstOrDefault()
        ?? Environment.GetEnvironmentVariable("VID02_IMAGE_FILE");
    await RunMultimodalAsync(agent, minimalReasoning,
        imagePath, "Explain the architecture shown in this image in detail.", "image");
});

return 0;

// =========================================================================================================
// Helpers
// =========================================================================================================

// Step 3: print token telemetry. UsageDetails fields are nullable because providers may omit them.
static void PrintUsage(UsageDetails? usage)
{
    if (usage is null)
    {
        Console.WriteLine("(no usage details returned)");
        return;
    }

    Console.WriteLine($"Total Token Count:     {usage.TotalTokenCount}");
    Console.WriteLine($"Input Token Count:     {usage.InputTokenCount}");
    Console.WriteLine($"Output Token Count:    {usage.OutputTokenCount}");
    Console.WriteLine($"Reasoning Token Count: {usage.ReasoningTokenCount}");
}

// Step 5: consume the stream. The video checks only Contents.FirstOrDefault(); this version inspects every
// content item and treats null or empty Contents as normal (see "Engineering Insights" in the notes).
static async Task StreamAsync(IAsyncEnumerable<AgentResponseUpdate> updates, string completedLabel)
{
    await foreach (AgentResponseUpdate item in updates)
    {
        if (item.Contents is null || item.Contents.Count == 0)
        {
            continue;
        }

        foreach (AIContent content in item.Contents)
        {
            if (content is UsageContent usageContent)
            {
                // UsageContent marks the end of the stream and carries the telemetry.
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n\n--- {completedLabel}. Token Usage Details ---");
                PrintUsage(usageContent.Details);
                Console.ResetColor();
            }
            else if (content is TextContent text)
            {
                Console.Write(text.Text);
            }
        }
    }
}

// Step 6 / Exercise 2: read a binary file, wrap it in DataContent and send it with a question.
static async Task RunMultimodalAsync(
    AIAgent agent,
    ChatClientAgentRunOptions? options,
    string? path,
    string question,
    string label)
{
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
    {
        Console.WriteLine($"Skipped: no {label} file. Pass a path as an argument or set the matching environment variable.");
        return;
    }

    string mediaType = Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        string ext => throw new NotSupportedException($"Unsupported file extension '{ext}'.")
    };

    // Production note from the notes: large files pressure the LOH and cost many input tokens.
    byte[] bytes = await File.ReadAllBytesAsync(path);
    DataContent data = new(new ReadOnlyMemory<byte>(bytes), mediaType);

    ChatMessage message = new(ChatRole.User, new List<AIContent> { new TextContent(question), data });

    Console.WriteLine($"Sending multimodal payload ({bytes.Length / 1024.0:F1} KB {label}, {mediaType}) + question...");
    await StreamAsync(agent.RunStreamingAsync(message, options: options), "Multimodal Stream Completed");
}

static bool IsReasoningModel(string deployment)
{
    string id = deployment.Split('/')[^1].ToLowerInvariant();
    return id.StartsWith("gpt-5", StringComparison.Ordinal)
        || id.StartsWith("o1", StringComparison.Ordinal)
        || id.StartsWith("o3", StringComparison.Ordinal)
        || id.StartsWith("o4", StringComparison.Ordinal);
}

static async Task Section(string title, Func<Task> action)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"\n====== {title} ======");
    Console.ResetColor();

    try
    {
        await action();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
        Console.ResetColor();
    }
}

// From the video: record that receives the structured output.
public record LargestCity(string Name, long Population);

// Exercise 1.
public record BookInfo(string Title, string Author, int PublishedYear, string[] KeyThemes);
