using PlantillaBackend.Application.DTOs.Auth;

namespace PlantillaBackend.Application.UseCases.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterFuncionarioDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(int funcionarioId, CancellationToken cancellationToken = default);
}

