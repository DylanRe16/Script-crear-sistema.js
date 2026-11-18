using PlantillaBackend.Application.DTOs.Funcionarios;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Application.UseCases.Users;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IFuncionarioRepository _funcionarioRepository;
    private readonly IRolRepository _rolRepository;

    public UserManagementService(IFuncionarioRepository funcionarioRepository, IRolRepository rolRepository)
    {
        _funcionarioRepository = funcionarioRepository;
        _rolRepository = rolRepository;
    }

    public async Task<IReadOnlyList<FuncionarioAdminResponse>> GetUsersAsync(bool soloActivos, CancellationToken cancellationToken = default)
    {
        var users = await _funcionarioRepository.GetAllWithDetailsAsync(soloActivos, cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<FuncionarioAdminResponse?> GetUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var funcionario = await _funcionarioRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return funcionario is null ? null : Map(funcionario);
    }

    public async Task<IReadOnlyList<RolAdminResponse>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _rolRepository.GetAllAsync(cancellationToken);
        return roles
            .Where(rol => rol.Benabled)
            .OrderBy(rol => rol.Sdescripcion)
            .Select(rol => new RolAdminResponse(rol.IdRol, rol.Sdescripcion, rol.Benabled))
            .ToList();
    }

    public async Task<FuncionarioAdminResponse?> UpdateUserAsync(int id, ActualizarFuncionarioAdminRequest request, CancellationToken cancellationToken = default)
    {
        var funcionario = await _funcionarioRepository.GetByIdAsync(id, cancellationToken);
        if (funcionario is null)
        {
            return null;
        }

        var rol = await _rolRepository.GetByIdAsync(request.RolId, cancellationToken);
        if (rol is null || !rol.Benabled)
        {
            throw new InvalidOperationException($"El rol con ID {request.RolId} no existe o no se encuentra activo.");
        }

        funcionario.SprimerNombre = request.SprimerNombre.Trim();
        funcionario.SsegundoNombre = string.IsNullOrWhiteSpace(request.SsegundoNombre) ? null : request.SsegundoNombre.Trim();
        funcionario.SprimerApellido = request.SprimerApellido.Trim();
        funcionario.SsegundoApellido = string.IsNullOrWhiteSpace(request.SsegundoApellido) ? null : request.SsegundoApellido.Trim();
        funcionario.RolId = request.RolId;
        funcionario.Benabled = request.Benabled;

        await _funcionarioRepository.UpdateAsync(funcionario, cancellationToken);
        await _funcionarioRepository.SaveChangesAsync(cancellationToken);

        var actualizado = await _funcionarioRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return actualizado is null ? null : Map(actualizado);
    }

    private static FuncionarioAdminResponse Map(Funcionario funcionario)
    {
        var fechaCreacionUtc = DateTime.SpecifyKind(funcionario.DfechaCreacion, DateTimeKind.Utc);

        return new FuncionarioAdminResponse(
            funcionario.IdFuncionario,
            funcionario.Snacionalidad,
            funcionario.NcedulaIdent,
            funcionario.CedulaCompleta,
            funcionario.SprimerNombre,
            funcionario.SsegundoNombre,
            funcionario.SprimerApellido,
            funcionario.SsegundoApellido,
            funcionario.NombreCompleto,
            funcionario.RolId,
            funcionario.Rol?.Sdescripcion ?? string.Empty,
            funcionario.Benabled,
            new DateTimeOffset(fechaCreacionUtc));
    }
}
