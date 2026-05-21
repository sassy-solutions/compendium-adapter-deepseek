// -----------------------------------------------------------------------
// <copyright file="DeepSeekToolCallingExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.DeepSeek.Tools;

namespace Compendium.Adapters.DeepSeek.Tests.Tools;

public class DeepSeekToolCallingExtensionsTests
{
    [Fact]
    public void WithTools_AddsToolsToAdditionalParameters()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "m",
            Messages = new List<Message> { Message.User("hi") }
        };
        var tools = new List<AgentTool> { new("foo", "bar") };

        // Act
        var updated = request.WithTools(tools, "auto");

        // Assert
        var added = updated.AdditionalParameters!;
        added.Should().ContainKey(DeepSeekToolCallingExtensions.ToolsKey);
        added[DeepSeekToolCallingExtensions.ToolsKey].Should().BeSameAs(tools);
        added[DeepSeekToolCallingExtensions.ToolChoiceKey].Should().Be("auto");
    }

    [Fact]
    public void WithTools_PreservesExistingAdditionalParameters()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "m",
            Messages = new List<Message> { Message.User("hi") },
            AdditionalParameters = new Dictionary<string, object> { ["pre-existing"] = 42 }
        };

        // Act
        var updated = request.WithTools(new List<AgentTool> { new("foo", "bar") });

        // Assert
        updated.AdditionalParameters!["pre-existing"].Should().Be(42);
    }

    [Fact]
    public void WithTools_WithoutToolChoice_OmitsToolChoiceKey()
    {
        // Arrange
        var request = new CompletionRequest { Model = "m", Messages = new List<Message>() };

        // Act
        var updated = request.WithTools(new List<AgentTool> { new("foo", "bar") });

        // Assert
        updated.AdditionalParameters!.ContainsKey(DeepSeekToolCallingExtensions.ToolChoiceKey).Should().BeFalse();
    }

    [Fact]
    public void WithTools_NullRequest_Throws()
    {
        // Arrange
        CompletionRequest? request = null;

        // Act
        var act = () => request!.WithTools(new List<AgentTool>());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithTools_NullTools_Throws()
    {
        // Arrange
        var request = new CompletionRequest { Model = "m", Messages = new List<Message>() };

        // Act
        var act = () => request.WithTools(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetToolCalls_NullResponse_Throws()
    {
        // Arrange
        CompletionResponse? response = null;

        // Act
        var act = () => response!.GetToolCalls();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetToolCalls_WhenMetadataAbsent_ReturnsEmpty()
    {
        // Arrange
        var response = new CompletionResponse
        {
            Id = "x",
            Model = "m",
            Content = "y",
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 0, CompletionTokens = 0 }
        };

        // Act
        var calls = response.GetToolCalls();

        // Assert
        calls.Should().BeEmpty();
    }

    [Fact]
    public void GetToolCalls_WrongMetadataShape_ReturnsEmpty()
    {
        // Arrange
        var response = new CompletionResponse
        {
            Id = "x",
            Model = "m",
            Content = "y",
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 0, CompletionTokens = 0 },
            Metadata = new Dictionary<string, object>
            {
                [DeepSeekToolCallingExtensions.ToolCallsMetadataKey] = "not-a-list"
            }
        };

        // Act
        var calls = response.GetToolCalls();

        // Assert
        calls.Should().BeEmpty();
    }
}
