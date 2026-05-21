# Changelog

All notable changes to `Compendium.Adapters.DeepSeek` are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). Versions are computed by
[MinVer](https://github.com/adamralph/minver) from git tags (`v*` prefix).

## [Unreleased]

### Added

- Initial implementation of `IAIProvider` against DeepSeek's REST API
  (`https://api.deepseek.com`). DeepSeek is OpenAI-compatible, so the wire format mirrors
  `compendium-adapter-openai`.
- `DeepSeekAIProvider` — chat completions, streaming SSE, list models, health check.
- Tool / function calling round-trip via `DeepSeekToolCallingExtensions.WithTools(...)` and
  `CompletionResponse.GetToolCalls()`.
- Reasoning-model support: `deepseek-reasoner` emits a `reasoning_content` field alongside
  `content`. The trace is surfaced via `CompletionResponse.Metadata["deepseek.reasoning_content"]`
  (helper `GetReasoningContent()`); opt into inline `<think>…</think>` tagging on `Content` with
  `DeepSeekOptions.InlineReasoningInContent`.
- `DeepSeekOptions` — `ApiKey`, `BaseUrl`, `DefaultModel`, `DefaultMaxTokens`, `TimeoutSeconds`,
  `RetryAttempts`, `InlineReasoningInContent`, `EnableLogging`.
- DI registration: `AddCompendiumDeepSeek(IConfiguration)` and
  `AddCompendiumDeepSeek(Action<DeepSeekOptions>)`. Wires the typed `HttpClient` through
  `Microsoft.Extensions.Http.Resilience`'s standard pipeline with reasoner-friendly timeouts
  (180 s per attempt, 600 s total).
- `samples/01-reasoning-loop` — runnable example that calls `deepseek-reasoner` and prints both
  the reasoning trace and the final answer.
- 81 xUnit tests, 98.5 % line coverage / 92.2 % branch coverage.

### Decided

- **Embeddings**: DeepSeek does not offer a hosted embeddings endpoint; `EmbedAsync` returns
  `Result.Failure(Error.Unavailable("AI.Unsupported", …))` rather than throwing. Use
  `compendium-adapter-openai` or `compendium-adapter-mistral` for embeddings.
- **Reasoning channel**: surfaced via `CompletionResponse.Metadata` rather than concatenated into
  `Content`. This preserves clean answer text by default; inline tagging is opt-in.
- **Streaming reasoning**: `CompletionChunk` in `Compendium.Abstractions.AI` 1.0.1 has no
  `Metadata` dictionary, so pure-reasoning deltas are dropped from the stream unless
  `InlineReasoningInContent` is set (in which case they ship as `<think>token</think>` fragments
  in `ContentDelta`). Use `CompleteAsync` for full reasoning visibility.
