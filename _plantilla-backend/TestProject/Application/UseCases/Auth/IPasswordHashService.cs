using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Application.UseCases.Auth;

public interface IPasswordHashService
{
    string HashPassword(Funcionario funcionario, string password);
    bool VerifyPassword(Funcionario funcionario, string hashedPassword, string providedPassword);
}

