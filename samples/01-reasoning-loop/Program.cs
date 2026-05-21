// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Models;
using Compendium.Adapters.DeepSeek.Configuration;
using Compendium.Adapters.DeepSeek.DependencyInjection;
using Compendium.Adapters.DeepSeek.Reasoning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Demonstrates DeepSeek's deepseek-reasoner model: a single completion call that exposes both
// the visible answer (CompletionResponse.Content) and the chain-of-thought trace
// (CompletionResponse.GetReasoningContent()).
var apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.Error.WriteLine("Set DEEPSEEK_API_KEY first.");
    return 1;
}

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddCompendiumDeepSeek(opt =>
{
    opt.ApiKey = apiKey;
    opt.DefaultModel = DeepSeekOptions.ReasonerModel;
    // Try `true` to see <think>...</think> tags merged into Content instead.
    opt.InlineReasoningInContent = false;
});

await using var sp = services.BuildServiceProvider();
var provider = sp.GetRequiredService<IAIProvider>();

var request = new CompletionRequest
{
    Model = DeepSeekOptions.ReasonerModel,
    SystemPrompt = "You are a careful logic puzzle solver. Show your reasoning before answering.",
    Messages = new List<Message>
    {
        Message.User("Three boxes A, B, C. One contains gold. A says 'gold is in B'. "
            + "B says 'gold is not here'. C says 'gold is not in A'. Exactly one box tells the truth. "
            + "Where is the gold?")
    },
    MaxTokens = 1024
};

var result = await provider.CompleteAsync(request);
if (result.IsFailure)
{
    Console.Error.WriteLine($"Error: {result.Error.Code} - {result.Error.Message}");
    return 1;
}

Console.WriteLine("=== Reasoning trace ===");
Console.WriteLine(result.Value.GetReasoningContent() ?? "<none — model returned no reasoning_content>");
Console.WriteLine();
Console.WriteLine("=== Final answer ===");
Console.WriteLine(result.Value.Content);
Console.WriteLine();
Console.WriteLine($"Tokens — prompt: {result.Value.Usage.PromptTokens}, completion: {result.Value.Usage.CompletionTokens}");
Console.WriteLine($"Finish reason: {result.Value.FinishReason}");

return 0;
