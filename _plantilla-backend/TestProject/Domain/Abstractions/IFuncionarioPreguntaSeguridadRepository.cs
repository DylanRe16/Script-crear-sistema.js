using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Domain.Abstractions;

/// <summary>
/// Contrato de persistencia para las preguntas de seguridad asignadas a funcionarios
/// </summary>
public interface IFuncionarioPreguntaSeguridadRepository : IRepository<FuncionarioPreguntaSeguridad, int>
{
    Task<IEnumerable<FuncionarioPreguntaSeguridad>> GetByFuncionarioIdAsync(int funcionarioId, CancellationToken cancellationToken = default);
    Task<int> CountByFuncionarioIdAsync(int funcionarioId, CancellationToken cancellationToken = default);
    Task<bool> HasPreguntasConfiguradasAsync(int funcionarioId, CancellationToken cancellationToken = default);
    Task<FuncionarioPreguntaSeguridad?> GetByFuncionarioYPreguntaAsync(int funcionarioId, int idPreguntaSeg, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<FuncionarioPreguntaSeguridad> entidades, CancellationToken cancellationToken = default);
    Task<int> DeleteByFuncionarioIdAsync(int funcionarioId, CancellationToken cancellationToken = default);
    Task ReplacePreguntasFuncionarioAsync(int funcionarioId, IEnumerable<FuncionarioPreguntaSeguridad> nuevasPreguntas, CancellationToken cancellationToken = default);
}

