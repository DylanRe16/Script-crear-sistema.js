using PlantillaBackend.Application.DTOs.Funcionarios;

namespace PlantillaBackend.Application.UseCases.Users;

public interface IUserManagementService
{
    Task<IReadOnlyList<FuncionarioAdminResponse>> GetUsersAsync(bool soloActivos, CancellationToken cancellationToken = default);
    Task<FuncionarioAdminResponse?> GetUserAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RolAdminResponse>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<FuncionarioAdminResponse?> UpdateUserAsync(int id, ActualizarFuncionarioAdminRequest request, CancellationToken cancellationToken = default);
}
