using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.Domain.Entities;

/// <summary>
/// Respuestas de preguntas de seguridad asignadas a un funcionario.
/// Mapeo 1:1 de tabla existente PlantillaBackend.funcionario_pseguridad
/// </summary>
[Table("funcionario_pseguridad", Schema = "PlantillaBackend")]
public class FuncionarioPreguntaSeguridad : IAuditableEntity
{
    [Key]
    [Column("id_fpseguridad")]
    public int IdFpseguridad { get; set; }

    [Column("id_funcionario")]
    public int IdFuncionario { get; set; }

    [Column("id_preguntaseg")]
    public int IdPreguntaSeg { get; set; }

    [Column("dfecha_reg")]
    public DateTime? DfechaReg { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Column("srespuesta")]
    public string Srespuesta { get; set; } = string.Empty;

    [Column("nusuario_creacion")]
    public int NusuarioCreacion { get; set; } = 1;

    [Column("dfecha_creacion")]
    public DateTime DfechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("nusuario_actualizacion")]
    public int? NusuarioActualizacion { get; set; }

    [Column("dfecha_actualizacion")]
    public DateTime? DfechaActualizacion { get; set; }

    // Relaciones
    [ForeignKey(nameof(IdFuncionario))]
    public virtual Funcionario Funcionario { get; set; } = null!;

    [ForeignKey(nameof(IdPreguntaSeg))]
    public virtual PreguntaSeguridad PreguntaSeguridad { get; set; } = null!;
}

