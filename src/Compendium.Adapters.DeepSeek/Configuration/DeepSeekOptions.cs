// -----------------------------------------------------------------------
// <copyright file="DeepSeekOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.DeepSeek.Configuration;

/// <summary>
/// Configuration options for the DeepSeek AI provider.
/// </summary>
/// <remarks>
/// DeepSeek is OpenAI-compatible at the wire level — most fields here mirror the OpenAI adapter
/// verbatim. The two production-grade DeepSeek models are <c>deepseek-chat</c> (V3, the default;
/// general-purpose) and <c>deepseek-reasoner</c> (R1, surfaces a <c>reasoning_content</c> channel
/// alongside <c>content</c>).
/// </remarks>
public sealed class DeepSeekOptions
{
    /// <summary>
    /// Default chat model — DeepSeek's flagship general-purpose model.
    /// </summary>
    public const string DefaultChatModel = "deepseek-chat";

    /// <summary>
    /// Reasoning model — exposes intermediate <c>reasoning_content</c> alongside the answer.
    /// </summary>
    public const string ReasonerModel = "deepseek-reasoner";

    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "DeepSeek";

    /// <summary>
    /// Gets or sets the DeepSeek API key. Required.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL for the DeepSeek API.
    /// Default is <c>https://api.deepseek.com</c>. The adapter posts to <c>/chat/completions</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.deepseek.com";

    /// <summary>
    /// Gets or sets the default chat model. Default is <see cref="DefaultChatModel"/>.
    /// </summary>
    public string DefaultModel { get; set; } = DefaultChatModel;

    /// <summary>
    /// Gets or sets the default sampling temperature.
    /// </summary>
    public float DefaultTemperature { get; set; } = 0.7f;

    /// <summary>
    /// Gets or sets the default maximum tokens for chat completions.
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds. DeepSeek reasoner traces can be long;
    /// keep the default generous.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// Gets or sets the number of retry attempts for transient failures.
    /// Applied via Microsoft.Extensions.Http.Resilience's standard pipeline.
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to enable verbose request/response logging at debug level.
    /// </summary>
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Gets or sets whether <c>reasoning_content</c> emitted by reasoning models should be
    /// prepended to <see cref="CompletionResponse.Content"/> wrapped in
    /// <c>&lt;think&gt;...&lt;/think&gt;</c> tags. When <c>false</c> (the default) the reasoning is
    /// only surfaced via <see cref="CompletionResponse.Metadata"/> under the
    /// <c>deepseek.reasoning_content</c> key and the visible content stays clean.
    /// </summary>
    public bool InlineReasoningInContent { get; set; }
}
