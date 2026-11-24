# Vid02.InputsOutputs

Implementation of [docs/video-2-inputs-outputs-technical-notes.md](../../docs/video-2-inputs-outputs-technical-notes.md)
(Episode 2: Inputs & Outputs). The `Vid02.` prefix marks projects that follow a video, as opposed to the book's `ChNN.` projects.

## Run

From the repository root, after `az login` (or with `AZURE_OPENAI_API_KEY` set for development):

```powershell
dotnet run --project src/Vid02.InputsOutputs -- path\to\file.pdf path\to\image.png
```

The PDF and image are optional. They can also be set with `VID02_PDF_FILE` and `VID02_IMAGE_FILE`; without them the
multimodal sections print "Skipped". `VID02_DEPLOYMENT` points the demo at a different chat deployment.

## Map from the notes to the code

| Notes | Where in `Program.cs` | What it shows |
|---|---|---|
| Step 2 | `[Step 2]` section | `RunAsync<IEnumerable<LargestCity>>` returns typed objects in `response.Result` |
| Step 3 | `PrintUsage` | `UsageDetails`: total, input, output and reasoning token counts |
| Step 4 | `minimalReasoning` and `[Step 4]` | `ChatClientAgentRunOptions` with `RawRepresentationFactory` setting `ReasoningEffortLevel.Minimal` |
| Step 5 | `StreamAsync` | `RunStreamingAsync`; `UsageContent` arrives with the token counts |
| Step 6 | `RunMultimodalAsync` | `DataContent` (`application/pdf`) next to a `TextContent` question in one `ChatMessage` |
| Exercise 1 | `[Exercise 1]` | `RunAsync<BookInfo>` with a record containing a `string[]` |
| Exercise 2 | `[Exercise 2]` | Same helper with `image/png` or `image/jpeg` |

## Differences from the video

- **Provider:** the video uses OpenAI directly with `gpt-5-nano`. This repo reaches Azure OpenAI through `IChatClient`
  (`AgentClientFactory.CreateChatClient`), so the same agent code runs on whatever deployment `.env` names.
  The framework is .NET 10 like the video (set in `global.json` and `Directory.Build.props`).
- **Reasoning effort:** only reasoning models (`gpt-5*`, `o1`, `o3`, `o4`) accept it. On a non-reasoning deployment such as
  `gpt-4.1-mini`, Step 4 is skipped and `ReasoningTokenCount` is 0 everywhere. To compare Step 2 and Step 4, deploy a reasoning
  model and set `VID02_DEPLOYMENT`. OpenAI marks `ReasoningEffortLevel` as evaluation-only, so the warning is suppressed on that block alone.
- **Streaming:** the video inspects only `Contents.FirstOrDefault()`. This version checks every content item and tolerates
  null or empty `Contents`, as the notes recommend under "Engineering Insights".
- **Packages:** versions come from `Directory.Packages.props`, not the release candidate named in the video.
