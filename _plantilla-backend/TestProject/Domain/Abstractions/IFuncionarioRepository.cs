using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Domain.Abstractions;

/// <summary>
/// Contrato de repositorio para el dominio de Funcionarios
/// </summary>
public interface IFuncionarioRepository : IRepository<Funcionario, int>
{
    Task<IEnumerable<Funcionario>> GetAllWithDetailsAsync(bool soloActivos = true, CancellationToken cancellationToken = default);
    Task<Funcionario?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<Funcionario?> GetByCedulaAsync(string nacionalidad, int cedula, CancellationToken cancellationToken = default);
    Task<bool> ExistsCedulaAsync(string nacionalidad, int cedula, CancellationToken cancellationToken = default);
}

