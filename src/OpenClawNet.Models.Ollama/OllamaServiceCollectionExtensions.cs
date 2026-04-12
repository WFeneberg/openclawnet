using Microsoft.Extensions.DependencyInjection;
using OpenClawNet.Models.Abstractions;

namespace OpenClawNet.Models.Ollama;

public static class OllamaServiceCollectionExtensions
{
    public static IServiceCollection AddOllama(this IServiceCollection services, Action<OllamaOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<OllamaOptions>(_ => { });

        services.AddHttpClient<OllamaModelClient>();
        services.AddSingleton<IModelClient>(sp => sp.GetRequiredService<OllamaModelClient>());

        return services;
    }
}
