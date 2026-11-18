using System.ComponentModel.DataAnnotations;

namespace PlantillaBackend.Application.DTOs.Funcionarios;

/// <summary>Funcionario devuelto al área administrativa de usuarios.</summary>
public sealed record FuncionarioAdminResponse(
    int IdFuncionario,
    string Snacionalidad,
    int NcedulaIdent,
    string CedulaCompleta,
    string SprimerNombre,
    string? SsegundoNombre,
    string SprimerApellido,
    string? SsegundoApellido,
    string NombreCompleto,
    int RolId,
    string NombreRol,
    bool Benabled,
    DateTimeOffset DfechaCreacion);

/// <summary>Rol disponible para asignar a una cuenta.</summary>
public sealed record RolAdminResponse(int IdRol, string Sdescripcion, bool Benabled);

/// <summary>Datos editables de una cuenta en el área administrativa.</summary>
public sealed record ActualizarFuncionarioAdminRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string SprimerNombre { get; init; }

    [StringLength(100)]
    public string? SsegundoNombre { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public required string SprimerApellido { get; init; }

    [StringLength(100)]
    public string? SsegundoApellido { get; init; }

    [Range(1, int.MaxValue)]
    public required int RolId { get; init; }

    public required bool Benabled { get; init; }
}
