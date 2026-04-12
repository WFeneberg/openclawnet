using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OpenClawNet.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddOpenClawStorage(this IServiceCollection services, string? connectionString = null)
    {
        connectionString ??= "Data Source=openclawnet.db";
        
        services.AddDbContextFactory<OpenClawDbContext>(options =>
            options.UseSqlite(connectionString));
        
        services.AddScoped<IConversationStore, ConversationStore>();
        
        return services;
    }
}
