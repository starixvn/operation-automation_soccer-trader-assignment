using Microsoft.Extensions.DependencyInjection;
using SoccerLST.TraderAssignment.Core.Services;
using SoccerLST.TraderAssignment.Core.Services.Strategies;

namespace SoccerLST.TraderAssignment.Core.Extensions;

/// <summary>
/// Extension methods for configuring trader assignment services in DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all trader assignment services and strategies
    /// </summary>
    public static IServiceCollection AddTraderAssignmentServices(this IServiceCollection services)
    {
        // Register all assignment strategies
        services.AddScoped<IMatchAssignmentStrategy, MajorMatchAssignmentStrategy>();
        services.AddScoped<IMatchAssignmentStrategy, MinorOneMatchAssignmentStrategy>();
        services.AddScoped<IMatchAssignmentStrategy, MinorTwoMatchAssignmentStrategy>();

        // Register main service
        services.AddScoped<TraderAssignmentService>();

        return services;
    }
}
