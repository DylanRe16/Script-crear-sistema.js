using Microsoft.EntityFrameworkCore;
using PlantillaBackend.Data.Persistence;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Data.Repositories;

public class FuncionarioPreguntaSeguridadRepository : Repository<FuncionarioPreguntaSeguridad, int>, IFuncionarioPreguntaSeguridadRepository
{
    public FuncionarioPreguntaSeguridadRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<FuncionarioPreguntaSeguridad>> GetByFuncionarioIdAsync(int funcionarioId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(fp => fp.PreguntaSeguridad)
            .Where(fp => fp.IdFuncionario == funcionarioId)
            .OrderBy(fp => fp.IdFpseguridad)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByFuncionarioIdAsync(int funcionarioId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .CountAsync(fp => fp.IdFuncionario == funcionarioId, cancellationToken);
    }

    public async Task<bool> HasPreguntasConfiguradasAsync(int funcionarioId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .AnyAsync(fp => fp.IdFuncionario == funcionarioId, cancellationToken);
    }

    public async Task<FuncionarioPreguntaSeguridad?> GetByFuncionarioYPreguntaAsync(int funcionarioId, int idPreguntaSeg, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(fp => fp.PreguntaSeguridad)
            .FirstOrDefaultAsync(fp => fp.IdFuncionario == funcionarioId && fp.IdPreguntaSeg == idPreguntaSeg, cancellationToken);
    }

    public Task DeleteRangeAsync(IEnumerable<FuncionarioPreguntaSeguridad> entidades, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entidades);
        return Task.CompletedTask;
    }

    public async Task<int> DeleteByFuncionarioIdAsync(int funcionarioId, CancellationToken cancellationToken = default)
    {
        var existentes = await _dbSet
            .Where(fp => fp.IdFuncionario == funcionarioId)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(existentes);
        await _context.SaveChangesAsync(cancellationToken);
        return existentes.Count;
    }

    public async Task ReplacePreguntasFuncionarioAsync(int funcionarioId, IEnumerable<FuncionarioPreguntaSeguridad> nuevasPreguntas, CancellationToken cancellationToken = default)
    {
        // En una transacción atómica para consistencia
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existentes = await _dbSet
                .Where(fp => fp.IdFuncionario == funcionarioId)
                .ToListAsync(cancellationToken);

            if (existentes.Count > 0)
            {
                _dbSet.RemoveRange(existentes);
                await _context.SaveChangesAsync(cancellationToken);
            }

            await _dbSet.AddRangeAsync(nuevasPreguntas, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

