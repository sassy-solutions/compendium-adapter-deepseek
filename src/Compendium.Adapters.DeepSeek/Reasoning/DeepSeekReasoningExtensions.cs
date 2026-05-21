// -----------------------------------------------------------------------
// <copyright file="DeepSeekReasoningExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.DeepSeek.Reasoning;

/// <summary>
/// Helpers for reading the chain-of-thought trace surfaced by DeepSeek's reasoning model
/// (<c>deepseek-reasoner</c>).
/// </summary>
/// <remarks>
/// The Compendium AI abstractions (1.0.1) do not have a first-class reasoning channel on
/// <see cref="CompletionResponse"/> nor any <c>Metadata</c> dictionary on
/// <see cref="CompletionChunk"/>. This adapter surfaces the trace on the final response via
/// <see cref="CompletionResponse.Metadata"/> under the well-known key
/// <see cref="ReasoningContentKey"/>. For non-streaming calls (<c>CompleteAsync</c>) the full
/// trace is always available. For streaming calls (<c>StreamCompleteAsync</c>) the abstraction's
/// chunk shape does not carry a side channel, so reasoning tokens are merged into the visible
/// <see cref="CompletionChunk.ContentDelta"/> only when
/// <c>DeepSeekOptions.InlineReasoningInContent</c> is set; otherwise they are dropped from the
/// stream and consumers should use <c>CompleteAsync</c> to retrieve the trace.
/// </remarks>
public static class DeepSeekReasoningExtensions
{
    /// <summary>
    /// Metadata key carrying the full reasoning trace on a <see cref="CompletionResponse"/>.
    /// </summary>
    public const string ReasoningContentKey = "deepseek.reasoning_content";

    /// <summary>
    /// Opening tag used when reasoning is inlined into <see cref="CompletionResponse.Content"/>
    /// (opt-in via <c>DeepSeekOptions.InlineReasoningInContent</c>).
    /// </summary>
    public const string ThinkOpenTag = "<think>";

    /// <summary>
    /// Closing tag used when reasoning is inlined into <see cref="CompletionResponse.Content"/>.
    /// </summary>
    public const string ThinkCloseTag = "</think>";

    /// <summary>
    /// Returns the reasoning trace emitted by <c>deepseek-reasoner</c>, or <c>null</c> when none
    /// was produced (e.g. when using <c>deepseek-chat</c>).
    /// </summary>
    public static string? GetReasoningContent(this CompletionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.Metadata != null
            && response.Metadata.TryGetValue(ReasoningContentKey, out var raw)
            && raw is string s)
        {
            return s;
        }
        return null;
    }
}
