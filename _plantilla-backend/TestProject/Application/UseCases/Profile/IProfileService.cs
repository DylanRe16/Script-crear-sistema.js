using PlantillaBackend.Application.DTOs.Funcionarios;
using PlantillaBackend.Application.DTOs.Profile;

namespace PlantillaBackend.Application.UseCases.Profile;

public interface IProfileService
{
    Task<bool> ChangePasswordAsync(int funcionarioId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
    Task<FuncionarioResponseDto?> GetProfileAsync(int funcionarioId, CancellationToken cancellationToken = default);
}

