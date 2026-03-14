using QuanLyKhoNguyenLieuPizza.Core.Interfaces;

namespace QuanLyKhoNguyenLieuPizza.Services;

/// <summary>
/// Simple service locator for dependency resolution
/// In production, consider using Microsoft.Extensions.DependencyInjection
/// </summary>
public class ServiceLocator
{
    private static ServiceLocator? _instance;
    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<Type, Func<object>> _factories = new();

    public static ServiceLocator Instance => _instance ??= new ServiceLocator();

    private ServiceLocator()
    {
        RegisterDefaultServices();
    }

    private void RegisterDefaultServices()
    {
        // Register services
        RegisterSingleton<IDatabaseService>(new DatabaseService());
        RegisterSingleton<ConfigurationService>(ConfigurationService.Instance);
    }

    public void RegisterSingleton<TService>(TService implementation) where TService : class
    {
        _services[typeof(TService)] = implementation;
    }

    public void RegisterFactory<TService>(Func<TService> factory) where TService : class
    {
        _factories[typeof(TService)] = () => factory();
    }

    public TService GetService<TService>() where TService : class
    {
        var type = typeof(TService);

        if (_services.TryGetValue(type, out var service))
        {
            return (TService)service;
        }

        if (_factories.TryGetValue(type, out var factory))
        {
            return (TService)factory();
        }

        throw new InvalidOperationException($"Service of type {type.Name} is not registered");
    }

    public TService? TryGetService<TService>() where TService : class
    {
        try
        {
            return GetService<TService>();
        }
        catch
        {
            return null;
        }
    }
}
