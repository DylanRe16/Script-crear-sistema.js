namespace PlantillaBackend.Domain.Abstractions;

/// <summary>
/// Contrato base de auditoría para entidades del dominio PlantillaBackend
/// </summary>
public interface IAuditableEntity
{
    int NusuarioCreacion { get; set; }
    DateTime DfechaCreacion { get; set; }
    int? NusuarioActualizacion { get; set; }
    DateTime? DfechaActualizacion { get; set; }
}

