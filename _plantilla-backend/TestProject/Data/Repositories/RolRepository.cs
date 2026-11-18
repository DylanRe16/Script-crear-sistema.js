using PlantillaBackend.Data.Persistence;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Data.Repositories;

/// <summary>
/// Implementación de IRolRepository usando EF Core
/// </summary>
public class RolRepository : Repository<Rol, int>, IRolRepository
{
    public RolRepository(ApplicationDbContext context) : base(context)
    {
    }
}

