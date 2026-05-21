// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.DeepSeek.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.DeepSeek.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumDeepSeek_WithConfiguration_RegistersIAIProvider()
    {
        // Arrange
        var configValues = new Dictionary<string, string?>
        {
            ["DeepSeek:ApiKey"] = "ds-test",
            ["DeepSeek:DefaultModel"] = "deepseek-chat"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumDeepSeek(config);
        var provider = services.BuildServiceProvider();

        // Assert
        var aiProvider = provider.GetService<IAIProvider>();
        aiProvider.Should().NotBeNull();
        aiProvider!.ProviderId.Should().Be("deepseek");
        provider.GetRequiredService<IOptions<DeepSeekOptions>>().Value.ApiKey.Should().Be("ds-test");
    }

    [Fact]
    public void AddCompendiumDeepSeek_WithActionConfigure_RegistersIAIProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumDeepSeek(opt =>
        {
            opt.ApiKey = "ds-action";
            opt.DefaultModel = "deepseek-reasoner";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var aiProvider = provider.GetRequiredService<IAIProvider>();
        aiProvider.ProviderId.Should().Be("deepseek");
        provider.GetRequiredService<IOptions<DeepSeekOptions>>().Value.DefaultModel.Should().Be("deepseek-reasoner");
    }

    [Fact]
    public void AddCompendiumDeepSeek_RegistersTypedHttpClientAndIAIProviderAreSameInstance()
    {
        // Arrange — verify the DI graph wires the typed HttpClient to DeepSeekAIProvider and
        // exposes the same singleton as IAIProvider. Direct HttpClient timeout observation is not
        // possible via IHttpClientFactory once Microsoft.Extensions.Http.Resilience wraps the
        // pipeline, but we can prove the registration path works end-to-end.
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumDeepSeek(opt =>
        {
            opt.ApiKey = "k";
            opt.TimeoutSeconds = 42;
            opt.DefaultModel = "deepseek-reasoner";
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var providerA = provider.GetRequiredService<IAIProvider>();
        var providerB = provider.GetRequiredService<IAIProvider>();
        providerA.Should().BeSameAs(providerB);
        providerA.ProviderId.Should().Be("deepseek");
        provider.GetRequiredService<IOptions<DeepSeekOptions>>().Value.TimeoutSeconds.Should().Be(42);
    }

    [Fact]
    public void AddCompendiumDeepSeek_NullServices_Throws()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddCompendiumDeepSeek(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumDeepSeek_NullConfiguration_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddCompendiumDeepSeek((IConfiguration)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumDeepSeek_NullAction_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddCompendiumDeepSeek((Action<DeepSeekOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumDeepSeek_NullServicesWithConfiguration_Throws()
    {
        // Arrange
        IServiceCollection? services = null;
        var config = new ConfigurationBuilder().Build();

        // Act
        var act = () => services!.AddCompendiumDeepSeek(config);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
