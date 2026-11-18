using PlantillaBackend.Application.DTOs.PreguntasSeguridad;
using PlantillaBackend.Application.UseCases.Auth;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Application.UseCases.PreguntasSeguridad;

public interface IFuncionarioPreguntaSeguridadService
{
    Task<IEnumerable<FuncionarioPreguntaSeguridadResponseDto>> GetMisPreguntasAsync(int funcionarioId, CancellationToken cancellationToken = default);
    Task<EstadoPreguntasSeguridadDto> AsignarMisPreguntasAsync(int funcionarioId, AsignarPreguntasSeguridadDto dto, CancellationToken cancellationToken = default);
    Task<EstadoPreguntasSeguridadDto> GetEstadoSeguridadAsync(int funcionarioId, CancellationToken cancellationToken = default);
    Task<ResultadoValidacionRespuestaDto> ValidarRespuestaAsync(int funcionarioId, ValidarRespuestaSeguridadDto dto, CancellationToken cancellationToken = default);
    Task<bool> RestablecerPreguntasUsuarioAsync(int funcionarioId, CancellationToken cancellationToken = default);
}

public class FuncionarioPreguntaSeguridadService : IFuncionarioPreguntaSeguridadService
{
    public const int MaximoPreguntasPermitidas = 6;

    private readonly IFuncionarioPreguntaSeguridadRepository _repository;
    private readonly IPreguntaSeguridadRepository _preguntaRepo;
    private readonly IFuncionarioRepository _funcionarioRepo;
    private readonly IPasswordHashService _passwordHashService;

    public FuncionarioPreguntaSeguridadService(
        IFuncionarioPreguntaSeguridadRepository repository,
        IPreguntaSeguridadRepository preguntaRepo,
        IFuncionarioRepository funcionarioRepo,
        IPasswordHashService passwordHashService)
    {
        _repository = repository;
        _preguntaRepo = preguntaRepo;
        _funcionarioRepo = funcionarioRepo;
        _passwordHashService = passwordHashService;
    }

    public async Task<IEnumerable<FuncionarioPreguntaSeguridadResponseDto>> GetMisPreguntasAsync(int funcionarioId, CancellationToken cancellationToken = default)
    {
        var lista = await _repository.GetByFuncionarioIdAsync(funcionarioId, cancellationToken);
        return lista.Select(MapToResponseDto);
    }

    public async Task<EstadoPreguntasSeguridadDto> AsignarMisPreguntasAsync(int funcionarioId, AsignarPreguntasSeguridadDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Validar funcionario
        var funcionario = await _funcionarioRepo.GetByIdAsync(funcionarioId, cancellationToken);
        if (funcionario == null || !funcionario.Benabled)
        {
            throw new KeyNotFoundException($"Funcionario con ID {funcionarioId} no encontrado o inactivo.");
        }

        // 2. Validar lista
        if (dto.Preguntas == null || dto.Preguntas.Count == 0)
        {
            throw new InvalidOperationException("Debe suministrar al menos una pregunta de seguridad.");
        }

        // 3. Regla de negocio crítica: Máximo 6 preguntas
        if (dto.Preguntas.Count > MaximoPreguntasPermitidas)
        {
            throw new InvalidOperationException($"Regla de negocio: No se permite configurar más de {MaximoPreguntasPermitidas} preguntas de seguridad (suministradas: {dto.Preguntas.Count}).");
        }

        // 4. Regla de unicidad: Respetar uq_funcionario_pregunta (sin preguntas duplicadas)
        var idsPreguntas = dto.Preguntas.Select(p => p.IdPreguntaSeg).ToList();
        if (idsPreguntas.Distinct().Count() != idsPreguntas.Count)
        {
            throw new InvalidOperationException("No se permiten preguntas de seguridad repetidas en la asignación.");
        }

        // 5. Validar que cada pregunta exista y esté activa en el catálogo
        var nuevasEntidades = new List<FuncionarioPreguntaSeguridad>();
        var fechaHoy = DateTime.UtcNow.Date;

        foreach (var item in dto.Preguntas)
        {
            var preguntaMaestra = await _preguntaRepo.GetByIdAsync(item.IdPreguntaSeg, cancellationToken);
            if (preguntaMaestra == null || !preguntaMaestra.Benabled)
            {
                throw new KeyNotFoundException($"La pregunta con ID {item.IdPreguntaSeg} no existe o se encuentra inactiva.");
            }

            var respTrim = item.Respuesta.Trim();
            if (string.IsNullOrWhiteSpace(respTrim))
            {
                throw new InvalidOperationException($"La respuesta para la pregunta '{preguntaMaestra.Sdescripcion}' no puede estar vacía.");
            }

            // Normalizar y Hashear respuesta para máxima confidencialidad (compatible con texto plano, SHA-256 o PBKDF2)
            var respuestaHash = _passwordHashService.HashPassword(funcionario, respTrim.ToLowerInvariant());

            nuevasEntidades.Add(new FuncionarioPreguntaSeguridad
            {
                IdFuncionario = funcionarioId,
                IdPreguntaSeg = item.IdPreguntaSeg,
                DfechaReg = fechaHoy,
                Srespuesta = respuestaHash
            });
        }

        // 6. Reemplazo atómico en BD
        await _repository.ReplacePreguntasFuncionarioAsync(funcionarioId, nuevasEntidades, cancellationToken);

        return new EstadoPreguntasSeguridadDto
        {
            TienePreguntasConfiguradas = true,
            TotalConfiguradas = nuevasEntidades.Count,
            MaximoPermitido = MaximoPreguntasPermitidas,
            RequiereConfiguracion = false,
            Mensaje = $"Se han configurado exitosamente {nuevasEntidades.Count} preguntas de seguridad."
        };
    }

    public async Task<EstadoPreguntasSeguridadDto> GetEstadoSeguridadAsync(int funcionarioId, CancellationToken cancellationToken = default)
    {
        var count = await _repository.CountByFuncionarioIdAsync(funcionarioId, cancellationToken);
        var tieneConfiguradas = count > 0;

        return new EstadoPreguntasSeguridadDto
        {
            TienePreguntasConfiguradas = tieneConfiguradas,
            TotalConfiguradas = count,
            MaximoPermitido = MaximoPreguntasPermitidas,
            RequiereConfiguracion = !tieneConfiguradas,
            Mensaje = tieneConfiguradas
                ? $"El funcionario posee {count} de un máximo de {MaximoPreguntasPermitidas} preguntas de seguridad configuradas."
                : "Atención: Debe configurar sus preguntas de seguridad para completar la habilitación de acceso."
        };
    }

    public async Task<bool> RestablecerPreguntasUsuarioAsync(int funcionarioId, CancellationToken cancellationToken = default)
    {
        var funcionario = await _funcionarioRepo.GetByIdAsync(funcionarioId, cancellationToken);
        if (funcionario is null)
        {
            return false;
        }

        await _repository.DeleteByFuncionarioIdAsync(funcionarioId, cancellationToken);
        return true;
    }

    public async Task<ResultadoValidacionRespuestaDto> ValidarRespuestaAsync(int funcionarioId, ValidarRespuestaSeguridadDto dto, CancellationToken cancellationToken = default)
    {
        var registro = await _repository.GetByFuncionarioYPreguntaAsync(funcionarioId, dto.IdPreguntaSeg, cancellationToken);
        if (registro == null)
        {
            return new ResultadoValidacionRespuestaDto
            {
                Valida = false,
                Mensaje = "El funcionario no tiene configurada la pregunta de seguridad indicada."
            };
        }

        var funcionario = await _funcionarioRepo.GetByIdAsync(funcionarioId, cancellationToken);
        if (funcionario == null)
        {
            return new ResultadoValidacionRespuestaDto
            {
                Valida = false,
                Mensaje = "Funcionario no encontrado."
            };
        }

        var respNormalizada = dto.Respuesta.Trim().ToLowerInvariant();
        var esValida = _passwordHashService.VerifyPassword(funcionario, registro.Srespuesta, respNormalizada);

        return new ResultadoValidacionRespuestaDto
        {
            Valida = esValida,
            Mensaje = esValida ? "Respuesta correcta." : "La respuesta suministrada es incorrecta."
        };
    }

    private static FuncionarioPreguntaSeguridadResponseDto MapToResponseDto(FuncionarioPreguntaSeguridad fp) => new()
    {
        IdFpseguridad = fp.IdFpseguridad,
        IdPreguntaSeg = fp.IdPreguntaSeg,
        SdescripcionPregunta = fp.PreguntaSeguridad?.Sdescripcion ?? string.Empty,
        DfechaReg = fp.DfechaReg
    };
}

