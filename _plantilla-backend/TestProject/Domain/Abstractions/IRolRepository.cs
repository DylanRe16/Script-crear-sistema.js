using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Domain.Abstractions;

/// <summary>
/// Contrato de repositorio para el dominio de Roles
/// </summary>
public interface IRolRepository : IRepository<Rol, int>
{
}

