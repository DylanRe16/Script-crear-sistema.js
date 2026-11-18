using System.Security.Claims;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Application.UseCases.Auth;

public interface ITokenService
{
    string GenerateToken(Funcionario funcionario, out int expiresInSeconds, bool requierePreguntasSeguridad = false);
    ClaimsPrincipal? ValidateToken(string token);
}

