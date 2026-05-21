// -----------------------------------------------------------------------
// <copyright file="TestFactories.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.DeepSeek.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.DeepSeek.Tests.TestSupport;

internal static class TestFactories
{
    public const string DefaultBaseUrl = "https://api.deepseek.com";
    public const string DefaultApiKey = "ds-test-key";

    public static DeepSeekOptions DefaultOptions(Action<DeepSeekOptions>? configure = null)
    {
        var options = new DeepSeekOptions
        {
            ApiKey = DefaultApiKey,
            BaseUrl = DefaultBaseUrl,
            DefaultModel = DeepSeekOptions.DefaultChatModel,
            DefaultMaxTokens = 4096,
            TimeoutSeconds = 120,
            EnableLogging = false
        };
        configure?.Invoke(options);
        return options;
    }

    public static (DeepSeekHttpClient Client, MockHttpMessageHandler Handler) CreateHttpClient(
        Action<DeepSeekOptions>? configure = null)
    {
        var handler = new MockHttpMessageHandler();
        var options = DefaultOptions(configure);
        var httpClient = new HttpClient(handler);
        var sut = new DeepSeekHttpClient(
            httpClient,
            Options.Create(options),
            NullLogger<DeepSeekHttpClient>.Instance);
        return (sut, handler);
    }

    public static DeepSeekAIProvider CreateProvider(
        DeepSeekHttpClient httpClient,
        Action<DeepSeekOptions>? configure = null)
    {
        var options = DefaultOptions(configure);
        return new DeepSeekAIProvider(
            httpClient,
            Options.Create(options),
            NullLogger<DeepSeekAIProvider>.Instance);
    }

    public static CompletionRequest SimpleCompletionRequest(string? model = null) =>
        new()
        {
            Model = model ?? DeepSeekOptions.DefaultChatModel,
            Messages = new List<Message> { Message.User("Hello") }
        };
}
