using Microsoft.Extensions.Caching.Memory;
using PlantillaBackend.Application.DTOs.Auth;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Application.UseCases.Auth;

/// <summary>
/// Recuperación de contraseña mediante la validación de las preguntas de seguridad
/// configuradas previamente por el funcionario.
/// </summary>
public interface IPasswordRecoveryService
{
    Task<PreguntasRecuperacionResponseDto> ObtenerPreguntasAsync(SolicitarRecuperacionDto dto, CancellationToken cancellationToken = default);
    Task<ValidacionRespuestasResponseDto> ValidarRespuestasAsync(ValidarRespuestasRecuperacionDto dto, CancellationToken cancellationToken = default);
    Task<RecuperacionPasswordResultDto> RestablecerPasswordAsync(RestablecerPasswordRecuperacionDto dto, CancellationToken cancellationToken = default);
}

public class PasswordRecoveryService : IPasswordRecoveryService
{
    private const int MaxIntentosFallidos = 5;
    private const int PreguntasRequeridas = 2;
    private static readonly TimeSpan VentanaIntentos = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan VigenciaToken = TimeSpan.FromMinutes(10);

    private readonly IFuncionarioRepository _funcionarioRepo;
    private readonly IFuncionarioPreguntaSeguridadRepository _preguntasRepo;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IMemoryCache _cache;

    public PasswordRecoveryService(
        IFuncionarioRepository funcionarioRepo,
        IFuncionarioPreguntaSeguridadRepository preguntasRepo,
        IPasswordHashService passwordHashService,
        IMemoryCache cache)
    {
        _funcionarioRepo = funcionarioRepo;
        _preguntasRepo = preguntasRepo;
        _passwordHashService = passwordHashService;
        _cache = cache;
    }

    public async Task<PreguntasRecuperacionResponseDto> ObtenerPreguntasAsync(SolicitarRecuperacionDto dto, CancellationToken cancellationToken = default)
    {
        var funcionario = await ObtenerFuncionarioActivoAsync(dto, cancellationToken);
        var preguntasConfiguradas = (await _preguntasRepo.GetByFuncionarioIdAsync(funcionario.IdFuncionario, cancellationToken)).ToList();

        if (preguntasConfiguradas.Count == 0)
        {
            throw new InvalidOperationException(
                "La cuenta no posee preguntas de seguridad configuradas. Contacte al administrador para restablecer su contraseña.");
        }

        // Se solicitan solo 2 preguntas elegidas al azar en cada intento. Las
        // respuestas incorrectas fuerzan un nuevo conjunto aleatorio.
        var seleccionadas = SeleccionarAleatorio(preguntasConfiguradas, PreguntasRequeridas);

        return new PreguntasRecuperacionResponseDto
        {
            TienePreguntasConfiguradas = true,
            TotalPreguntas = seleccionadas.Count,
            Mensaje = $"Responda correctamente {seleccionadas.Count} preguntas de seguridad para continuar.",
            Preguntas = seleccionadas
                .Select(pregunta => new PreguntaRecuperacionDto
                {
                    IdPreguntaSeg = pregunta.IdPreguntaSeg,
                    Sdescripcion = pregunta.PreguntaSeguridad?.Sdescripcion ?? $"Pregunta de seguridad #{pregunta.IdPreguntaSeg}"
                })
                .ToList()
        };
    }

    public async Task<ValidacionRespuestasResponseDto> ValidarRespuestasAsync(ValidarRespuestasRecuperacionDto dto, CancellationToken cancellationToken = default)
    {
        var funcionario = await ObtenerFuncionarioActivoAsync(dto, cancellationToken);
        var claveIntentos = CrearClaveIntentos(funcionario.Snacionalidad, funcionario.NcedulaIdent);

        VerificarBloqueo(claveIntentos);

        var preguntasConfiguradas = (await _preguntasRepo.GetByFuncionarioIdAsync(funcionario.IdFuncionario, cancellationToken)).ToList();
        if (preguntasConfiguradas.Count == 0)
        {
            throw new InvalidOperationException(
                "La cuenta no posee preguntas de seguridad configuradas. Contacte al administrador para restablecer su contraseña.");
        }

        ValidarEstructuraRespuestas(dto.Respuestas, preguntasConfiguradas);

        var respuestasCorrectas = dto.Respuestas.All(respuesta =>
        {
            var pregunta = preguntasConfiguradas.First(p => p.IdPreguntaSeg == respuesta.IdPreguntaSeg);
            var respuestaLimpia = respuesta.Respuesta.Trim().ToLowerInvariant();
            return _passwordHashService.VerifyPassword(funcionario, pregunta.Srespuesta, respuestaLimpia);
        });

        if (!respuestasCorrectas)
        {
            RegistrarIntentoFallido(claveIntentos);
            throw new InvalidOperationException(
                "Una o más respuestas de seguridad son incorrectas. Se generaron nuevas preguntas para que intente nuevamente.");
        }

        // Respuestas correctas: se emite un token temporal que habilita el
        // cambio de contraseña sin reenviar las respuestas.
        _cache.Remove(claveIntentos);

        var token = Guid.NewGuid().ToString("N");
        _cache.Set(CrearClaveToken(token), new RecuperacionTicket
        {
            IdFuncionario = funcionario.IdFuncionario,
            Snacionalidad = funcionario.Snacionalidad,
            NcedulaIdent = funcionario.NcedulaIdent
        }, VigenciaToken);

        return new ValidacionRespuestasResponseDto
        {
            Success = true,
            Mensaje = "Respuestas validadas correctamente. Defina su nueva contraseña.",
            TokenRecuperacion = token,
            ExpiraEnSegundos = (int)VigenciaToken.TotalSeconds
        };
    }

    public async Task<RecuperacionPasswordResultDto> RestablecerPasswordAsync(RestablecerPasswordRecuperacionDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.TokenRecuperacion) ||
            !_cache.TryGetValue(CrearClaveToken(dto.TokenRecuperacion), out RecuperacionTicket? ticket) ||
            ticket == null)
        {
            throw new InvalidOperationException(
                "La validación de las preguntas de seguridad expiró o no es válida. Vuelva a responder las preguntas.");
        }

        var funcionario = await _funcionarioRepo.GetByIdAsync(ticket.IdFuncionario, cancellationToken);
        if (funcionario == null || !funcionario.Benabled)
        {
            throw new InvalidOperationException(
                "No fue posible restablecer la contraseña. Verifique sus datos o contacte al administrador.");
        }

        if (_passwordHashService.VerifyPassword(funcionario, funcionario.SpasswordHash, dto.NewPassword))
        {
            throw new InvalidOperationException("La nueva contraseña no puede ser idéntica a la contraseña actual.");
        }

        funcionario.SpasswordHash = _passwordHashService.HashPassword(funcionario, dto.NewPassword);
        await _funcionarioRepo.UpdateAsync(funcionario, cancellationToken);
        await _funcionarioRepo.SaveChangesAsync(cancellationToken);

        _cache.Remove(CrearClaveToken(dto.TokenRecuperacion));
        _cache.Remove(CrearClaveIntentos(ticket.Snacionalidad, ticket.NcedulaIdent));

        return new RecuperacionPasswordResultDto
        {
            Success = true,
            Mensaje = "Contraseña restablecida exitosamente. Ya puede iniciar sesión con su nueva contraseña."
        };
    }

    private async Task<Funcionario> ObtenerFuncionarioActivoAsync(SolicitarRecuperacionDto dto, CancellationToken cancellationToken)
    {
        var nacionalidad = dto.Snacionalidad.Trim().ToUpperInvariant();
        var funcionario = await _funcionarioRepo.GetByCedulaAsync(nacionalidad, dto.NcedulaIdent, cancellationToken);

        if (funcionario == null || !funcionario.Benabled)
        {
            // Mensaje genérico para no revelar si la cédula existe o está inactiva.
            throw new InvalidOperationException(
                "No fue posible iniciar la recuperación de contraseña. Verifique los datos suministrados o contacte al administrador.");
        }

        return funcionario;
    }

    private void VerificarBloqueo(string claveIntentos)
    {
        if (ObtenerIntentosFallidos(claveIntentos) >= MaxIntentosFallidos)
        {
            throw new InvalidOperationException(
                $"Se superó el máximo de {MaxIntentosFallidos} intentos fallidos. Espere {VentanaIntentos.TotalMinutes:0} minutos e intente nuevamente.");
        }
    }

    private static void ValidarEstructuraRespuestas(List<RespuestaRecuperacionItemDto> respuestas, List<FuncionarioPreguntaSeguridad> preguntasConfiguradas)
    {
        if (respuestas == null || respuestas.Count == 0)
        {
            throw new InvalidOperationException("Debe suministrar las respuestas de las preguntas de seguridad.");
        }

        if (respuestas.GroupBy(respuesta => respuesta.IdPreguntaSeg).Any(grupo => grupo.Count() > 1))
        {
            throw new InvalidOperationException("No se permiten respuestas duplicadas para la misma pregunta de seguridad.");
        }

        var requeridas = Math.Min(PreguntasRequeridas, preguntasConfiguradas.Count);
        if (respuestas.Count != requeridas)
        {
            throw new InvalidOperationException($"Debe responder exactamente {requeridas} preguntas de seguridad.");
        }

        var idsConfigurados = preguntasConfiguradas.Select(pregunta => pregunta.IdPreguntaSeg).ToHashSet();
        if (respuestas.Any(respuesta => !idsConfigurados.Contains(respuesta.IdPreguntaSeg)))
        {
            throw new InvalidOperationException("Una de las preguntas suministradas no pertenece a su configuración de seguridad.");
        }
    }

    /// <summary>
    /// Mezcla la lista y devuelve la cantidad solicitada de elementos.
    /// </summary>
    private static List<T> SeleccionarAleatorio<T>(List<T> items, int cantidad)
    {
        var copia = items.ToList();

        for (var i = copia.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (copia[i], copia[j]) = (copia[j], copia[i]);
        }

        return copia.Take(Math.Min(cantidad, copia.Count)).ToList();
    }

    private static string CrearClaveIntentos(string nacionalidad, int cedula) => $"recuperacion-password:{nacionalidad.Trim().ToUpperInvariant()}:{cedula}";

    private static string CrearClaveToken(string token) => $"recuperacion-token:{token}";

    private int ObtenerIntentosFallidos(string clave) => _cache.TryGetValue<int>(clave, out var intentos) ? intentos : 0;

    private void RegistrarIntentoFallido(string clave)
    {
        var intentos = ObtenerIntentosFallidos(clave) + 1;
        _cache.Set(clave, intentos, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = VentanaIntentos
        });
    }

    private sealed class RecuperacionTicket
    {
        public int IdFuncionario { get; set; }
        public string Snacionalidad { get; set; } = string.Empty;
        public int NcedulaIdent { get; set; }
    }
}
