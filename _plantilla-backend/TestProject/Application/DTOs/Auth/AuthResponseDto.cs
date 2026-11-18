using PlantillaBackend.Application.DTOs.Funcionarios;

namespace PlantillaBackend.Application.DTOs.Auth;

public class AuthResponseDto
{
    public bool Success { get; set; } = true;
    public string Mensaje { get; set; } = "Operación exitosa.";
    public string? Token { get; set; }
    public string? TokenType { get; set; }
    public int? ExpiresInSeconds { get; set; }
    public FuncionarioResponseDto Funcionario { get; set; } = null!;
    public bool RequiereConfigurarPreguntasSeguridad { get; set; }
    public int TotalPreguntasSeguridadConfiguradas { get; set; }
    public bool RequiereCambiarContrasena { get; set; }
}

