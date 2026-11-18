using PlantillaBackend.Application.DTOs.PreguntasSeguridad;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Application.UseCases.PreguntasSeguridad;

public interface IPreguntaSeguridadService
{
    Task<IEnumerable<PreguntaSeguridadResponseDto>> GetAllAsync(bool soloActivas = true, CancellationToken cancellationToken = default);
    Task<PreguntaSeguridadResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PreguntaSeguridadResponseDto> CreateAsync(CrearPreguntaSeguridadDto dto, CancellationToken cancellationToken = default);
    Task<PreguntaSeguridadResponseDto?> UpdateAsync(int id, ActualizarPreguntaSeguridadDto dto, CancellationToken cancellationToken = default);
    Task<bool> ToggleStatusAsync(int id, bool benabled, CancellationToken cancellationToken = default);
}

public class PreguntaSeguridadService : IPreguntaSeguridadService
{
    private readonly IPreguntaSeguridadRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public PreguntaSeguridadService(
        IPreguntaSeguridadRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<PreguntaSeguridadResponseDto>> GetAllAsync(bool soloActivas = true, CancellationToken cancellationToken = default)
    {
        var lista = await _repository.GetAllAsync(soloActivas, cancellationToken);
        return lista.Select(MapToDto);
    }

    public async Task<PreguntaSeguridadResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entidad = await _repository.GetByIdAsync(id, cancellationToken);
        return entidad is null ? null : MapToDto(entidad);
    }

    public async Task<PreguntaSeguridadResponseDto> CreateAsync(CrearPreguntaSeguridadDto dto, CancellationToken cancellationToken = default)
    {
        ValidarPermisoAdmin();

        var desc = dto.Sdescripcion.Trim();
        if (await _repository.ExistsDescripcionAsync(desc, null, cancellationToken))
        {
            throw new InvalidOperationException($"Ya existe una pregunta de seguridad registrada con la descripción '{desc}'.");
        }

        var entidad = new PreguntaSeguridad
        {
            Sdescripcion = desc,
            Benabled = true
        };

        await _repository.AddAsync(entidad, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return MapToDto(entidad);
    }

    public async Task<PreguntaSeguridadResponseDto?> UpdateAsync(int id, ActualizarPreguntaSeguridadDto dto, CancellationToken cancellationToken = default)
    {
        ValidarPermisoAdmin();

        var entidad = await _repository.GetByIdAsync(id, cancellationToken);
        if (entidad is null) return null;

        var desc = dto.Sdescripcion.Trim();
        if (await _repository.ExistsDescripcionAsync(desc, id, cancellationToken))
        {
            throw new InvalidOperationException($"Ya existe otra pregunta de seguridad con la descripción '{desc}'.");
        }

        entidad.Sdescripcion = desc;
        entidad.Benabled = dto.Benabled;

        await _repository.UpdateAsync(entidad, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return MapToDto(entidad);
    }

    public async Task<bool> ToggleStatusAsync(int id, bool benabled, CancellationToken cancellationToken = default)
    {
        ValidarPermisoAdmin();

        var entidad = await _repository.GetByIdAsync(id, cancellationToken);
        if (entidad is null) return false;

        entidad.Benabled = benabled;
        await _repository.UpdateAsync(entidad, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void ValidarPermisoAdmin()
    {
        if (!_currentUserService.IsAdmin)
        {
            throw new UnauthorizedAccessException("Operación denegada: solo los administradores pueden gestionar el catálogo de preguntas de seguridad.");
        }
    }

    private static PreguntaSeguridadResponseDto MapToDto(PreguntaSeguridad p) => new()
    {
        IdPreguntaSeg = p.IdPreguntaSeg,
        Sdescripcion = p.Sdescripcion,
        Benabled = p.Benabled,
        DfechaCreacion = p.DfechaCreacion
    };
}

