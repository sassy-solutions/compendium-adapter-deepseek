// -----------------------------------------------------------------------
// <copyright file="DeepSeekHttpClientTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.DeepSeek.Http;
using Compendium.Adapters.DeepSeek.Http.Models;
using Compendium.Adapters.DeepSeek.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.DeepSeek.Tests.Http;

public class DeepSeekHttpClientTests
{
    [Fact]
    public void Ctor_SetsBearerTokenAndBaseAddress()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler);
        var options = TestFactories.DefaultOptions(o =>
        {
            o.ApiKey = "ds-bearer";
        });

        // Act
        var sut = new DeepSeekHttpClient(inner, Options.Create(options), NullLogger<DeepSeekHttpClient>.Instance);

        // Assert
        sut.Should().NotBeNull();
        inner.BaseAddress!.ToString().Should().StartWith("https://api.deepseek.com");
        inner.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        inner.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("ds-bearer");
    }

    [Fact]
    public void Ctor_WithoutApiKey_OmitsAuthorizationHeader()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler);
        var options = TestFactories.DefaultOptions(o => o.ApiKey = string.Empty);

        // Act
        _ = new DeepSeekHttpClient(inner, Options.Create(options), NullLogger<DeepSeekHttpClient>.Instance);

        // Assert
        inner.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public void Ctor_DoesNotOverridePreSetBaseAddress()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler) { BaseAddress = new Uri("https://proxy.test/") };
        var options = TestFactories.DefaultOptions();

        // Act
        _ = new DeepSeekHttpClient(inner, Options.Create(options), NullLogger<DeepSeekHttpClient>.Instance);

        // Assert
        inner.BaseAddress!.ToString().Should().Be("https://proxy.test/");
    }

    [Fact]
    public void Ctor_TrimsTrailingSlashFromConfiguredBaseUrl()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler);
        var options = TestFactories.DefaultOptions(o => o.BaseUrl = "https://api.deepseek.com/");

        // Act
        _ = new DeepSeekHttpClient(inner, Options.Create(options), NullLogger<DeepSeekHttpClient>.Instance);

        // Assert — the client should always have exactly one trailing slash.
        inner.BaseAddress!.ToString().Should().Be("https://api.deepseek.com/");
    }

    [Fact]
    public async Task LogsRequestAndResponse_WhenEnableLoggingTrue()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler);
        var options = TestFactories.DefaultOptions(o => o.EnableLogging = true);
        var logger = new RecordingLogger<DeepSeekHttpClient>();
        var client = new DeepSeekHttpClient(inner, Options.Create(options), logger);

        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        // Act
        await client.CreateChatCompletionAsync(
            new DeepSeekChatCompletionRequest
            {
                Model = "m",
                Messages = new List<DeepSeekChatMessage> { new() { Role = "user", Content = "hi" } }
            },
            CancellationToken.None);

        // Assert
        logger.Entries.Should().Contain(e => e.Message.Contains("DeepSeek request:"));
        logger.Entries.Should().Contain(e => e.Message.Contains("DeepSeek response"));
    }

    [Fact]
    public async Task ListModelsAsync_OnHttpRequestException_ReturnsProviderError()
    {
        // Arrange
        var (client, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Get, "*/models").Throw(new HttpRequestException("net down"));

        // Act
        var result = await client.ListModelsAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
    }

    [Fact]
    public async Task ListModelsAsync_WhenCallerCancels_RethrowsCancellation()
    {
        // Arrange
        var (client, handler) = TestFactories.CreateHttpClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        handler.When(HttpMethod.Get, "*/models").Throw(new TaskCanceledException("user cancel"));

        // Act
        var act = async () => await client.ListModelsAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception), exception));
        private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }
}
