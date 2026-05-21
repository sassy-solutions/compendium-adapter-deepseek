// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.DeepSeek.Configuration;
using Compendium.Adapters.DeepSeek.Http;
using Compendium.Adapters.DeepSeek.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.DeepSeek.DependencyInjection;

/// <summary>
/// DI extensions for the DeepSeek Compendium adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers DeepSeek as the <see cref="IAIProvider"/> with options bound from
    /// <paramref name="configuration"/> at section <see cref="DeepSeekOptions.SectionName"/>.
    /// </summary>
    public static IServiceCollection AddCompendiumDeepSeek(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DeepSeekOptions>(configuration.GetSection(DeepSeekOptions.SectionName));
        return services.AddCompendiumDeepSeekCore();
    }

    /// <summary>
    /// Registers DeepSeek as the <see cref="IAIProvider"/> with options configured inline.
    /// </summary>
    public static IServiceCollection AddCompendiumDeepSeek(
        this IServiceCollection services,
        Action<DeepSeekOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return services.AddCompendiumDeepSeekCore();
    }

    private static IServiceCollection AddCompendiumDeepSeekCore(this IServiceCollection services)
    {
        services.AddHttpClient<DeepSeekHttpClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DeepSeekOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddStandardResilienceHandler(o =>
        {
            // DeepSeek reasoner traces can run for several minutes; stretch the per-attempt and
            // total timeouts so we don't cancel mid-thinking. Retries/circuit-breaker stay default.
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(180);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(600);
            o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(360);
        });

        services.AddSingleton<DeepSeekAIProvider>();
        services.AddSingleton<IAIProvider>(sp => sp.GetRequiredService<DeepSeekAIProvider>());

        return services;
    }
}
