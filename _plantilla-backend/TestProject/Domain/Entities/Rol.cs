using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.Domain.Entities;

/// <summary>
/// Rol de funcionario en el sistema
/// Mapeo de tabla existente PlantillaBackend.roles
/// </summary>
[Table("roles", Schema = "PlantillaBackend")]
public class Rol : IAuditableEntity
{
    [Key]
    [Column("id_rol")]
    public int IdRol { get; set; }

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

    public virtual ICollection<Funcionario> Funcionarios { get; set; } = new List<Funcionario>();
}

