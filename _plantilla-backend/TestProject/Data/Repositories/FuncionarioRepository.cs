using Microsoft.EntityFrameworkCore;
using PlantillaBackend.Data.Persistence;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Data.Repositories;

/// <summary>
/// Implementación de IFuncionarioRepository usando EF Core
/// </summary>
public class FuncionarioRepository : Repository<Funcionario, int>, IFuncionarioRepository
{
    public FuncionarioRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Funcionario>> GetAllWithDetailsAsync(bool soloActivos = true, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(f => f.Rol);

        IQueryable<Funcionario> filtrado = soloActivos ? query.Where(f => f.Benabled) : query;
        return await filtrado.OrderBy(f => f.SprimerApellido).ToListAsync(cancellationToken);
    }

    public async Task<Funcionario?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(f => f.Rol)
            .FirstOrDefaultAsync(f => f.IdFuncionario == id, cancellationToken);
    }

    public async Task<Funcionario?> GetByCedulaAsync(string nacionalidad, int cedula, CancellationToken cancellationToken = default)
    {
        var nac = nacionalidad.Trim().ToUpper();
        return await _dbSet.AsNoTracking()
            .Include(f => f.Rol)
            .FirstOrDefaultAsync(f => f.Snacionalidad == nac && f.NcedulaIdent == cedula, cancellationToken);
    }

    public async Task<bool> ExistsCedulaAsync(string nacionalidad, int cedula, CancellationToken cancellationToken = default)
    {
        var nac = nacionalidad.Trim().ToUpper();
        return await _dbSet.AnyAsync(f => f.Snacionalidad == nac && f.NcedulaIdent == cedula, cancellationToken);
    }
}
