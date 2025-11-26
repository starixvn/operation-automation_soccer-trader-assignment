using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoccerLST.TraderAssignment.Core.Services;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M1;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M2;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M3;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M4;
using SoccerLST.TraderAssignment.Core.Services.Strategies.MJ;

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
        services.AddScoped<IMatchAssignmentStrategy, MinorThreeMatchAssignmentStrategy>();
        services.AddScoped<IMatchAssignmentStrategy, MinorFourMatchAssignmentStrategy>();
        // Register main service
        services.AddScoped<TraderAssignmentService>();

        return services;
    }
}
