using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Domain.Abstractions;

/// <summary>
/// Contrato de persistencia para el catálogo de preguntas de seguridad
/// </summary>
public interface IPreguntaSeguridadRepository : IRepository<PreguntaSeguridad, int>
{
    Task<IEnumerable<PreguntaSeguridad>> GetAllAsync(bool soloActivas = true, CancellationToken cancellationToken = default);
    Task<bool> ExistsDescripcionAsync(string descripcion, int? excludeId = null, CancellationToken cancellationToken = default);
}

