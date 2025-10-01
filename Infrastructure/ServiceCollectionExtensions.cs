using CRUDAPI.Application.UseCases;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using CRUDAPI.Infrastructure.Persistence.context;
using CRUDAPI.Infrastructure.repositories;
using CRUDAPI.Infrastructure.Security;
using CRUDAPI.Infrastructure.services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CRUDAPI.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services,IConfiguration configuration)
        {
            //Conexion a la base de datos
            services.AddDbContextPool<BaseContext>(options =>
             options.UseSqlServer(configuration.GetConnectionString("conexion")));
            //servicios
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IPasswordHashingService, PasswordHashingService>();
            services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
            services.AddScoped<IHttpContextService, HttpContextService>();
            services.AddScoped<ITokenValidationService, TokenValidationService>();
            services.AddScoped<IUsuarioDeletionService, UsuarioDeletionService>();
            services.AddScoped<IUsuarioValidationService, UsuarioValidationService>();
            //Repository
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddTransient<IEnviar, EnvioEmail>();
            // Use Cases
            services.AddScoped<ChangePasswordUseCase>();
            services.AddScoped<CleanupUserTokensUseCase>();
            services.AddScoped<CreateUsuarioUseCase>();
            services.AddScoped<DecodeTokenUseCase>();
            services.AddScoped<DeleteUsuarioUseCase>();
            services.AddScoped<GetUsuarioUseCase>();
            services.AddScoped<LoginUsuarioUseCase>();
            services.AddScoped<RefreshTokenUseCase>();
            services.AddScoped<RevokeAllUserTokensUseCase>();
            services.AddScoped<RevokeTokenUseCase>();
            services.AddScoped<UpdateUsuarioUseCase>();
            services.AddScoped<ValidateTokenUseCase>();
            return services;
        }
    }
}
