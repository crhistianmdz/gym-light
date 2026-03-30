using GymFlow.Application.UseCases.BodyMeasurements;
using GymFlow.Application.Validators;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence.Repositories;

namespace GymFlow.WebAPI.Extensions;

/// <summary>
/// Extension methods for registering services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds services related to body measurements.
    /// </summary>
    public static IServiceCollection AddBodyMeasurementServices(this IServiceCollection services)
    {
        services.AddScoped<IBodyMeasurementRepository, BodyMeasurementRepository>();
        services.AddScoped<AddBodyMeasurementUseCase>();
        services.AddScoped<GetBodyMeasurementsUseCase>();
        services.AddScoped<AddBodyMeasurementValidator>();

        return services;
    }
}