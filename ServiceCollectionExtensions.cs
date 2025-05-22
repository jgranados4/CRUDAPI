using CRUDAPI.Domain.DataSources;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Infrastructure.datasources;
using CRUDAPI.Infrastructure.repositories;

namespace CRUDAPI
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenDataSource, TokenService>();
            services.AddScoped<ItokenRepository, tokenRepository>();
            services.AddScoped<IUtilidadesService, UtilidadesService>();
            services.AddScoped<IRefreshTokensource, RefreshTokenService>();
            return services;
        }
    }
}
