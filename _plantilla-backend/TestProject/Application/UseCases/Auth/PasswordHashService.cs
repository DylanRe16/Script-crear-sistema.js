using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Application.UseCases.Auth;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<Funcionario> _hasher = new();

    public string HashPassword(Funcionario funcionario, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        return _hasher.HashPassword(funcionario, password);
    }

    public bool VerifyPassword(Funcionario funcionario, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
            return false;

        try
        {
            var result = _hasher.VerifyHashedPassword(funcionario, hashedPassword, providedPassword);
            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return true;
            }
        }
        catch
        {
            // Continuar con verificación de compatibilidad
        }

        // Fallback SHA-256
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(providedPassword));
        var hexLower = Convert.ToHexString(bytes).ToLowerInvariant();
        var hexUpper = Convert.ToHexString(bytes);

        if (hashedPassword.Equals(hexLower, StringComparison.OrdinalIgnoreCase) ||
            hashedPassword.Equals(hexUpper, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Fallback texto plano (si existieran datos de prueba iniciales)
        return hashedPassword == providedPassword;
    }
}

