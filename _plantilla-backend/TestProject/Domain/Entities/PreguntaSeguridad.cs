using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.Domain.Entities;

/// <summary>
/// Catálogo maestro de preguntas de seguridad del sistema PlantillaBackend.
/// Mapeo 1:1 de tabla existente PlantillaBackend.preguntas_seg
/// </summary>
[Table("preguntas_seg", Schema = "PlantillaBackend")]
public class PreguntaSeguridad : IAuditableEntity
{
    [Key]
    [Column("id_preguntaseg")]
    public int IdPreguntaSeg { get; set; }

    [Required]
    [Column("sdescripcion")]
    public string Sdescripcion { get; set; } = string.Empty;

    [Column("benabled")]
    public bool Benabled { get; set; } = true;

    [Column("nusuario_creacion")]
    public int NusuarioCreacion { get; set; } = 1;

    [Column("dfecha_creacion")]
    public DateTime DfechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("nusuario_actualizacion")]
    public int? NusuarioActualizacion { get; set; }

    [Column("dfecha_actualizacion")]
    public DateTime? DfechaActualizacion { get; set; }

    // Relaciones
    public virtual ICollection<FuncionarioPreguntaSeguridad> FuncionarioPreguntas { get; set; } = new List<FuncionarioPreguntaSeguridad>();
}

