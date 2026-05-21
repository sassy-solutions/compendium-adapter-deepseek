# Compendium.Adapters.DeepSeek

[![CI](https://github.com/sassy-solutions/compendium-adapter-deepseek/actions/workflows/ci.yml/badge.svg)](https://github.com/sassy-solutions/compendium-adapter-deepseek/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Compendium.Adapters.DeepSeek.svg)](https://www.nuget.org/packages/Compendium.Adapters.DeepSeek)

Direct API adapter that wires [DeepSeek's](https://api-docs.deepseek.com/) chat and reasoning
models into the [Compendium](https://github.com/sassy-solutions/compendium) `IAIProvider`
abstraction. DeepSeek's wire surface is OpenAI-compatible — this adapter implements the same
contract as `compendium-adapter-openai` so swapping providers is a one-line change.

```csharp
services.AddCompendiumDeepSeek(opt =>
{
    opt.ApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")!;
    opt.DefaultModel = DeepSeekOptions.DefaultChatModel;  // "deepseek-chat"
});
```

## What's supported

| Surface                       | Status | Notes                                                                 |
|-------------------------------|:------:|-----------------------------------------------------------------------|
| Chat completions (`/chat/completions`) |  yes   | Default model `deepseek-chat`.                                        |
| Streaming SSE                 |  yes   | `StreamCompleteAsync`. `usage` arrives on the final chunk.            |
| Tool / function calling       |  yes   | OpenAI-compatible. `deepseek-chat` only — see notes below.            |
| Reasoning model (`deepseek-reasoner`) |  yes   | `reasoning_content` exposed via `GetReasoningContent()`.              |
| List models / health check    |  yes   | `/models`.                                                            |
| Embeddings                    |  no    | DeepSeek does not expose a hosted embeddings endpoint.                |
| Vision                        |  no    | Not a DeepSeek capability today.                                      |

`EmbedAsync` returns `Result.Failure(Error.Unavailable("AI.Unsupported", …))` instead of throwing —
plug `compendium-adapter-openai` or `compendium-adapter-mistral` for embeddings.

## Quick start

```bash
dotnet add package Compendium.Adapters.DeepSeek
```

```csharp
using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Models;
using Compendium.Adapters.DeepSeek.Configuration;
using Compendium.Adapters.DeepSeek.DependencyInjection;
using Compendium.Adapters.DeepSeek.Reasoning;

var services = new ServiceCollection();
services.AddLogging();
services.AddCompendiumDeepSeek(opt =>
{
    opt.ApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")!;
});

await using var sp = services.BuildServiceProvider();
var provider = sp.GetRequiredService<IAIProvider>();

var result = await provider.CompleteAsync(new CompletionRequest
{
    Model = DeepSeekOptions.DefaultChatModel,
    Messages = [ Message.User("Explain hexagonal architecture in one paragraph.") ]
});

Console.WriteLine(result.IsSuccess ? result.Value.Content : result.Error.Message);
```

For a reasoning-model walkthrough see [`samples/01-reasoning-loop`](samples/01-reasoning-loop/Program.cs).

## Options

Configure via `IConfiguration` (binds the `DeepSeek` section) or an `Action<DeepSeekOptions>`:

| Option                       | Default                          | Notes                                                                                                 |
|------------------------------|----------------------------------|-------------------------------------------------------------------------------------------------------|
| `ApiKey`                     | _required_                       | Personal API key from <https://platform.deepseek.com>.                                                |
| `BaseUrl`                    | `https://api.deepseek.com`       | DeepSeek-hosted endpoint. China-hosted — see EU caveats below.                                       |
| `DefaultModel`               | `deepseek-chat`                  | `deepseek-chat` (V3, general-purpose) or `deepseek-reasoner` (R1, exposes reasoning trace).            |
| `DefaultTemperature`         | `0.7`                            | Default sampling temperature.                                                                         |
| `DefaultMaxTokens`           | `4096`                           | Applied when the request omits `MaxTokens`.                                                           |
| `TimeoutSeconds`             | `180`                            | DeepSeek reasoner traces can run for minutes — keep generous.                                         |
| `RetryAttempts`              | `3`                              | Forwarded into the standard `Microsoft.Extensions.Http.Resilience` pipeline.                          |
| `InlineReasoningInContent`   | `false`                          | When `true`, prepend `<think>…</think>` to `Content` for reasoner responses.                          |
| `EnableLogging`              | `false`                          | Verbose request/response logging at `Debug` level. Off by default — request bodies can carry secrets. |

The `Microsoft.Extensions.Http.Resilience` standard pipeline is wired with relaxed timeouts
(attempt: 180 s, total: 600 s) so reasoner traces aren't cut off mid-thought.

## Model selection — `deepseek-chat` vs `deepseek-reasoner`

| Model                | Class            | Strengths                                                | Tool calling | Reasoning trace |
|----------------------|------------------|----------------------------------------------------------|:------------:|:---------------:|
| `deepseek-chat`      | DeepSeek-V3      | Fast, cheap, broad knowledge. Default.                   | yes          | no              |
| `deepseek-reasoner`  | DeepSeek-R1      | Multi-step reasoning, math, code, logic puzzles.         | no (today)    | yes             |

Pick `deepseek-chat` for chat assistants, summarisation, drafting, content classification.
Pick `deepseek-reasoner` when the question benefits from a visible chain-of-thought — debugging,
proof-style answers, code review.

## Reasoning-content handling

`deepseek-reasoner` returns two assistant fields: `content` (the final answer) and
`reasoning_content` (the chain-of-thought trace). The Compendium AI abstractions (1.0.1) do not
have a first-class reasoning channel, so this adapter surfaces it as follows:

### Non-streaming (`CompleteAsync`)

The full reasoning trace lands in `CompletionResponse.Metadata` under the well-known key
`deepseek.reasoning_content`. Read it via the helper:

```csharp
using Compendium.Adapters.DeepSeek.Reasoning;

var result = await provider.CompleteAsync(request);
var reasoning = result.Value.GetReasoningContent(); // null when the model is deepseek-chat
var answer = result.Value.Content;
```

Set `DeepSeekOptions.InlineReasoningInContent = true` to additionally prepend the trace into
`Content` wrapped in `<think>…</think>` tags. Useful when piping through code that only inspects
`Content` (logging sinks, audit trails).

### Streaming (`StreamCompleteAsync`)

`CompletionChunk` in the 1.0.1 abstractions has no side-channel `Metadata` dictionary. To stay
within the contract the adapter defaults to **dropping pure-reasoning chunks** from the stream so
`ContentDelta` carries only the visible answer. The final aggregated trace is not available
mid-stream in default mode — use `CompleteAsync` for full visibility.

When `InlineReasoningInContent = true`, reasoning deltas are streamed as `<think>token</think>`
fragments in `ContentDelta` so a downstream parser can still partition the stream.

> **Future work**: when Compendium 1.1 ships `CompletionChunk.Metadata`, we'll surface deltas via
> `chunk.GetReasoningContentDelta()` so consumers can multiplex the two streams cleanly.

## Cost at a glance

DeepSeek's hosted pricing is dramatically lower than the western incumbents:

| Provider / model        | Input  $/M tokens | Output $/M tokens | Cached input $/M |
|-------------------------|-------------------|-------------------|------------------|
| `deepseek-chat` (V3)    | $0.27             | $1.10             | $0.07            |
| `deepseek-reasoner` (R1)| $0.55             | $2.19             | $0.14            |
| OpenAI `gpt-4o`         | $2.50             | $10.00            | $1.25            |
| OpenAI `gpt-4o-mini`    | $0.15             | $0.60             | $0.075           |
| OpenAI `o1`             | $15.00            | $60.00            | $7.50            |
| Anthropic `claude-4.5-sonnet` | $3.00       | $15.00            | $0.30            |

Numbers from the providers' public pricing pages at the time of release; they can change without
notice. Use the [DeepSeek pricing page](https://api-docs.deepseek.com/quick_start/pricing) for the
authoritative current figures.

The implication: `deepseek-reasoner` is roughly **25–30× cheaper than `o1`** for comparable
reasoning depth. That changes the unit economics of agent loops that fire many reasoning calls
per task.

## EU / data-residency caveats

The hosted DeepSeek endpoint at `api.deepseek.com` is operated from China. Before adopting it for
production:

- **GDPR**: a transfer of EU personal data to DeepSeek constitutes an international transfer to a
  country without an EU adequacy decision. You need standard contractual clauses + a TIA, and you
  should be prepared to lose the ability to use the service if a regulator objects.
- **Sensitive content**: do not send PII, source code under NDA, or regulated data (health,
  finance, public-sector) through the hosted API without legal sign-off.
- **Self-hosting**: DeepSeek's open-weight models (DeepSeek-V3, DeepSeek-R1) can be self-hosted on
  EU infrastructure (e.g. via vLLM or Ollama). Use `compendium-adapter-ollama` or a custom
  `BaseUrl` pointing at a vLLM endpoint to keep data in-region.

## Production checklist

- [ ] `ApiKey` injected via `IConfiguration` / `KeyVault` / Compendium secret store — **never**
      committed to source.
- [ ] Set a request `UserId` so DeepSeek can attribute requests to a specific tenant for abuse
      throttling.
- [ ] Decide whether to inline reasoning into `Content` — affects what downstream consumers see.
- [ ] Override `TimeoutSeconds` for tight SLOs; default `180s` is sized for `deepseek-reasoner`.
- [ ] Wire an integration test gated on `DEEPSEEK_API_KEY` so a future model rename trips CI.
- [ ] Review data-residency before sending EU personal data.
- [ ] Monitor `Result.Error.Code = "AI.RateLimitExceeded"` and back off accordingly — the standard
      resilience pipeline retries on 5xx but does not on 429 by default.

## Versioning

Released as `Compendium.Adapters.DeepSeek` on NuGet. Version is computed from git tags by
[MinVer](https://github.com/adamralph/minver); first tag `v1.0.0-preview.0` is cut by the
orchestrator after this PR lands.

## License

MIT — same as Compendium itself.
