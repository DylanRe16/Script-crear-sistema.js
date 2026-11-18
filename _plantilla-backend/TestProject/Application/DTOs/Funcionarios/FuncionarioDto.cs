namespace PlantillaBackend.Application.DTOs.Funcionarios;

public class FuncionarioResponseDto
{
    public int IdFuncionario { get; set; }
    public string Snacionalidad { get; set; } = string.Empty;
    public int NcedulaIdent { get; set; }
    public string CedulaCompleta { get; set; } = string.Empty;
    public string SprimerNombre { get; set; } = string.Empty;
    public string? SsegundoNombre { get; set; }
    public string SprimerApellido { get; set; } = string.Empty;
    public string? SsegundoApellido { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public int RolId { get; set; }
    public string NombreRol { get; set; } = string.Empty;
    public bool Benabled { get; set; }
    public DateTime DfechaCreacion { get; set; }
}

