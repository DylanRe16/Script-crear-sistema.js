using PlantillaBackend.Application.DTOs.Funcionarios;
using PlantillaBackend.Application.DTOs.Profile;
using PlantillaBackend.Application.UseCases.Auth;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Application.UseCases.Profile;

public class ProfileService : IProfileService
{
    private readonly IFuncionarioRepository _repository;
    private readonly IPasswordHashService _passwordHashService;

    public ProfileService(IFuncionarioRepository repository, IPasswordHashService passwordHashService)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
    }

    public async Task<bool> ChangePasswordAsync(int funcionarioId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var funcionario = await _repository.GetByIdAsync(funcionarioId, cancellationToken);
        if (funcionario == null || !funcionario.Benabled)
        {
            throw new KeyNotFoundException($"Funcionario con ID {funcionarioId} no encontrado o inactivo.");
        }

        var passwordValida = _passwordHashService.VerifyPassword(funcionario, funcionario.SpasswordHash, dto.CurrentPassword);
        if (!passwordValida)
        {
            throw new InvalidOperationException("La contraseña actual suministrada es incorrecta.");
        }

        if (dto.CurrentPassword == dto.NewPassword)
        {
            throw new InvalidOperationException("La nueva contraseña no puede ser idéntica a la contraseña actual.");
        }

        funcionario.SpasswordHash = _passwordHashService.HashPassword(funcionario, dto.NewPassword);

        await _repository.UpdateAsync(funcionario, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FuncionarioResponseDto?> GetProfileAsync(int funcionarioId, CancellationToken cancellationToken = default)
    {
        var funcionario = await _repository.GetByIdWithDetailsAsync(funcionarioId, cancellationToken);
        return funcionario == null ? null : MapToDto(funcionario);
    }

    private static FuncionarioResponseDto MapToDto(Funcionario f) => new()
    {
        IdFuncionario = f.IdFuncionario,
        Snacionalidad = f.Snacionalidad,
        NcedulaIdent = f.NcedulaIdent,
        CedulaCompleta = f.CedulaCompleta,
        SprimerNombre = f.SprimerNombre,
        SsegundoNombre = f.SsegundoNombre,
        SprimerApellido = f.SprimerApellido,
        SsegundoApellido = f.SsegundoApellido,
        NombreCompleto = f.NombreCompleto,
        RolId = f.RolId,
        NombreRol = f.Rol?.Sdescripcion ?? string.Empty,
        Benabled = f.Benabled,
        DfechaCreacion = f.DfechaCreacion
    };
}
