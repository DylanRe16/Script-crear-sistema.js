using System.Text.Json;
using System.Security.Claims;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.WebApi.Middleware;

/// <summary>
/// Middleware de exhortación y bloqueo temporal de seguridad.
/// Si un funcionario autenticado no posee preguntas de seguridad configuradas,
/// restringe el acceso a los módulos operativos del sistema, obligándolo a configurarlas
/// a través de /seguridad/mis-preguntas antes de continuar.
/// </summary>
public class ExhortacionPreguntasSeguridadMiddleware
{
    private readonly RequestDelegate _next;

    // Rutas exentas que el funcionario necesita para cumplir con la configuración o consultar su sesión
    private static readonly string[] RutasPermitidas =
    {
        "/seguridad",
        "/preguntasseguridad",
        "/profile/me",
        "/auth",
        "/swagger",
        "/scalar",
        "/openapi"
    };

    public ExhortacionPreguntasSeguridadMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IFuncionarioPreguntaSeguridadRepository preguntasRepository)
    {
        var user = context.User;

        // Solo evalúa a usuarios autenticados
        if (user?.Identity?.IsAuthenticated == true)
        {
            var claimRequiere = user.FindFirst("requiere_preguntas_seg")?.Value;
            var requiereConfigurar = string.Equals(claimRequiere, "true", StringComparison.OrdinalIgnoreCase);
            if (!requiereConfigurar)
            {
                var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(subClaim, out var funcionarioId) && funcionarioId > 0)
                {
                    requiereConfigurar = !await preguntasRepository.HasPreguntasConfiguradasAsync(
                        funcionarioId,
                        context.RequestAborted);
                }
            }

            if (requiereConfigurar)
            {
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

                // Si la ruta no está en la lista de rutas permitidas para la configuración
                var esRutaPermitida = RutasPermitidas.Any(r => path.StartsWith(r, StringComparison.OrdinalIgnoreCase));

                if (!esRutaPermitida)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    var respuestaBloqueo = new
                    {
                        success = false,
                        bloqueado = true,
                        requiereConfigurarPreguntasSeguridad = true,
                        codigo = "PREGUNTAS_SEGURIDAD_REQUERIDAS",
                        mensaje = "Acceso restringido temporalmente. Por políticas de seguridad del organismo PlantillaBackend, debe configurar sus preguntas de seguridad antes de poder operar en el sistema.",
                        enlaceAccion = "/seguridad/mis-preguntas"
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(respuestaBloqueo));
                    return;
                }
            }
        }

        await _next(context);
    }
}

