// -----------------------------------------------------------------------
// <copyright file="DeepSeekOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.DeepSeek.Tests.Configuration;

public class DeepSeekOptionsTests
{
    [Fact]
    public void Defaults_AreProductionSafe()
    {
        // Arrange / Act
        var options = new DeepSeekOptions();

        // Assert
        DeepSeekOptions.SectionName.Should().Be("DeepSeek");
        options.BaseUrl.Should().Be("https://api.deepseek.com");
        options.DefaultModel.Should().Be("deepseek-chat");
        options.DefaultTemperature.Should().BeApproximately(0.7f, 0.0001f);
        options.DefaultMaxTokens.Should().Be(4096);
        options.TimeoutSeconds.Should().Be(180);
        options.RetryAttempts.Should().Be(3);
        options.EnableLogging.Should().BeFalse();
        options.InlineReasoningInContent.Should().BeFalse();
        options.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void ModelConstants_ExposeBothFlagshipModels()
    {
        DeepSeekOptions.DefaultChatModel.Should().Be("deepseek-chat");
        DeepSeekOptions.ReasonerModel.Should().Be("deepseek-reasoner");
    }

    [Fact]
    public void SectionName_IsStableConstant()
    {
        // The section name must not silently change because consumers bind config to it.
        DeepSeekOptions.SectionName.Should().Be("DeepSeek");
    }
}
