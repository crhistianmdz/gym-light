namespace GymFlow.WebAPI.Extensions;

using System.Text;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Auth;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence.Repositories;
using GymFlow.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

public static class AuthExtensions
{
    public static IServiceCollection AddGymFlowAuth(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddScoped<IUserRepository, UserRepository>();
         services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUseCase>();
        
        // Use Cases — Sales & Products (HU-03)
        services.AddScoped<GetProductsUseCase>();
        services.AddScoped<GetProductByIdUseCase>();
        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<UpdateProductUseCase>();
        services.AddScoped<DeleteProductUseCase>();
        services.AddScoped<CreateSaleUseCase>();
        services.AddScoped<CancelSaleUseCase>();
        services.AddScoped<GetAccessLogsUseCase>();
        services.AddScoped<ExportAccessLogsUseCase>();

        var secret   = config["Jwt:Secret"]   ?? throw new InvalidOperationException("Jwt:Secret not configured.");
        var issuer   = config["Jwt:Issuer"]   ?? "gymflow";
        var audience = config["Jwt:Audience"] ?? "gymflow-client";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = issuer,
                    ValidAudience            = audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew                = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();
        return services;
    }
}