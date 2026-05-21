// -----------------------------------------------------------------------
// <copyright file="DeepSeekReasoningExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.DeepSeek.Reasoning;

namespace Compendium.Adapters.DeepSeek.Tests.Reasoning;

public class DeepSeekReasoningExtensionsTests
{
    [Fact]
    public void GetReasoningContent_NullResponse_Throws()
    {
        // Arrange
        CompletionResponse? response = null;

        // Act
        var act = () => response!.GetReasoningContent();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetReasoningContent_WhenMetadataAbsent_ReturnsNull()
    {
        // Arrange
        var response = BuildResponse(metadata: null);

        // Act
        var reasoning = response.GetReasoningContent();

        // Assert
        reasoning.Should().BeNull();
    }

    [Fact]
    public void GetReasoningContent_WrongShape_ReturnsNull()
    {
        // Arrange
        var response = BuildResponse(new Dictionary<string, object>
        {
            [DeepSeekReasoningExtensions.ReasoningContentKey] = 42
        });

        // Act
        var reasoning = response.GetReasoningContent();

        // Assert
        reasoning.Should().BeNull();
    }

    [Fact]
    public void GetReasoningContent_WhenPresent_ReturnsValue()
    {
        // Arrange
        var response = BuildResponse(new Dictionary<string, object>
        {
            [DeepSeekReasoningExtensions.ReasoningContentKey] = "step 1...step 2"
        });

        // Act
        var reasoning = response.GetReasoningContent();

        // Assert
        reasoning.Should().Be("step 1...step 2");
    }

    [Fact]
    public void ThinkTags_AreStableConstants()
    {
        DeepSeekReasoningExtensions.ThinkOpenTag.Should().Be("<think>");
        DeepSeekReasoningExtensions.ThinkCloseTag.Should().Be("</think>");
        DeepSeekReasoningExtensions.ReasoningContentKey.Should().Be("deepseek.reasoning_content");
    }

    private static CompletionResponse BuildResponse(Dictionary<string, object>? metadata) =>
        new()
        {
            Id = "x",
            Model = "deepseek-reasoner",
            Content = "y",
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 0, CompletionTokens = 0 },
            Metadata = metadata
        };
}
