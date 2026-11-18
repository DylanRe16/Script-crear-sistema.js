using Microsoft.EntityFrameworkCore;
using PlantillaBackend.Data.Persistence;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Data.Repositories;

public class PreguntaSeguridadRepository : Repository<PreguntaSeguridad, int>, IPreguntaSeguridadRepository
{
    public PreguntaSeguridadRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PreguntaSeguridad>> GetAllAsync(bool soloActivas = true, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (soloActivas)
        {
            query = query.Where(p => p.Benabled);
        }

        return await query.OrderBy(p => p.IdPreguntaSeg).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsDescripcionAsync(string descripcion, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var descNorm = descripcion.Trim().ToLower();
        var query = _dbSet.AsNoTracking().Where(p => p.Sdescripcion.ToLower() == descNorm);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.IdPreguntaSeg != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

