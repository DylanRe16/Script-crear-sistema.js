using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlantillaBackend.Data.Persistence;
using PlantillaBackend.Data.Repositories;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.Data;

/// <summary>
/// Registro modular de la capa Data (DbContext y Repositorios)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDataPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();
        services.AddScoped<IRolRepository, RolRepository>();
        services.AddScoped<IPreguntaSeguridadRepository, PreguntaSeguridadRepository>();
        services.AddScoped<IFuncionarioPreguntaSeguridadRepository, FuncionarioPreguntaSeguridadRepository>();

        return services;
    }
}

