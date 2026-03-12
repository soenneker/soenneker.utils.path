using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.Path.Abstract;

namespace Soenneker.Utils.Path.Registrars;

/// <summary>
/// A utility library for directory path related operations
/// </summary>
public static class PathUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IPathUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddPathUtilAsSingleton(this IServiceCollection services)
    {
        services.TryAddSingleton<IPathUtil, PathUtil>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="IPathUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddPathUtilAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IPathUtil, PathUtil>();
        return services;
    }
}