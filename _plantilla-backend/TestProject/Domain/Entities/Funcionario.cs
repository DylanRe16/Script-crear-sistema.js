using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.Domain.Entities;

/// <summary>
/// Funcionario del organismo / usuario del sistema
/// Mapeo de tabla existente PlantillaBackend.funcionarios
/// </summary>
[Table("funcionarios", Schema = "PlantillaBackend")]
public class Funcionario : IAuditableEntity
{
    [Key]
    [Column("id_funcionario")]
    public int IdFuncionario { get; set; }

    [Required]
    [MaxLength(1)]
    [Column("snacionalidad")]
    public string Snacionalidad { get; set; } = "V";

    [Column("ncedula_ident")]
    public int NcedulaIdent { get; set; }

    [Required]
    [Column("sprimer_apellido")]
    public string SprimerApellido { get; set; } = string.Empty;

    [Column("ssegundo_apellido")]
    public string? SsegundoApellido { get; set; }

    [Required]
    [Column("sprimer_nombre")]
    public string SprimerNombre { get; set; } = string.Empty;

    [Column("ssegundo_nombre")]
    public string? SsegundoNombre { get; set; }

    [Required]
    [Column("spassword_hash")]
    public string SpasswordHash { get; set; } = string.Empty;

    [Column("rol_id")]
    public int RolId { get; set; }

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

    [ForeignKey(nameof(RolId))]
    public virtual Rol Rol { get; set; } = null!;

    public virtual ICollection<FuncionarioPreguntaSeguridad> PreguntasSeguridad { get; set; }
        = new List<FuncionarioPreguntaSeguridad>();

    [NotMapped]
    public string NombreCompleto => $"{SprimerNombre} {SprimerApellido}".Trim();

    [NotMapped]
    public string CedulaCompleta => $"{Snacionalidad}-{NcedulaIdent}";
}

