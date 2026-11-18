using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PlantillaBackend.Application.DTOs.PreguntasSeguridad;
using PlantillaBackend.Application.UseCases.PreguntasSeguridad;

namespace PlantillaBackend.WebApi.Endpoints;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class SeguridadController : ControllerBase
{
    private readonly IFuncionarioPreguntaSeguridadService _service;

    public SeguridadController(IFuncionarioPreguntaSeguridadService service)
    {
        _service = service;
    }

    /// <summary>
    /// Consulta el estado de configuración de preguntas de seguridad del funcionario autenticado.
    /// Informa si tiene preguntas registradas o si requiere configurarlas obligatoriamente.
    /// </summary>
    [HttpGet("estado")]
    [ProducesResponseType(typeof(EstadoPreguntasSeguridadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EstadoPreguntasSeguridadDto>> GetEstado(CancellationToken cancellationToken)
    {
        var funcionarioId = ObtenerFuncionarioIdActual();
        if (funcionarioId <= 0)
        {
            return Unauthorized(new { success = false, mensaje = "Sesión no autenticada o token inválido." });
        }

        var estado = await _service.GetEstadoSeguridadAsync(funcionarioId, cancellationToken);
        return Ok(estado);
    }

    /// <summary>
    /// Obtiene las preguntas de seguridad que tiene configuradas el funcionario actual (sin exponer las respuestas).
    /// </summary>
    [HttpGet("mis-preguntas")]
    [ProducesResponseType(typeof(IEnumerable<FuncionarioPreguntaSeguridadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<FuncionarioPreguntaSeguridadResponseDto>>> GetMisPreguntas(CancellationToken cancellationToken)
    {
        var funcionarioId = ObtenerFuncionarioIdActual();
        if (funcionarioId <= 0)
        {
            return Unauthorized(new { success = false, mensaje = "Sesión no autenticada o token inválido." });
        }

        var lista = await _service.GetMisPreguntasAsync(funcionarioId, cancellationToken);
        return Ok(lista);
    }

    /// <summary>
    /// Registra o actualiza el conjunto de preguntas de seguridad y sus respuestas para el funcionario autenticado.
    /// Regla de negocio: Máximo 6 preguntas y sin preguntas duplicadas.
    /// </summary>
    [HttpPost("mis-preguntas")]
    [ProducesResponseType(typeof(EstadoPreguntasSeguridadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoPreguntasSeguridadDto>> AsignarMisPreguntas(
        [FromBody] AsignarPreguntasSeguridadDto dto,
        CancellationToken cancellationToken)
    {
        var funcionarioId = ObtenerFuncionarioIdActual();
        if (funcionarioId <= 0)
        {
            return Unauthorized(new { success = false, mensaje = "Sesión no autenticada o token inválido." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var resultado = await _service.AsignarMisPreguntasAsync(funcionarioId, dto, cancellationToken);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Valida una respuesta a una pregunta de seguridad específica del funcionario.
    /// </summary>
    [HttpPost("validar-respuesta")]
    [ProducesResponseType(typeof(ResultadoValidacionRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResultadoValidacionRespuestaDto>> ValidarRespuesta(
        [FromBody] ValidarRespuestaSeguridadDto dto,
        CancellationToken cancellationToken)
    {
        var funcionarioId = ObtenerFuncionarioIdActual();
        if (funcionarioId <= 0)
        {
            return Unauthorized(new { success = false, mensaje = "Sesión no autenticada o token inválido." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resultado = await _service.ValidarRespuestaAsync(funcionarioId, dto, cancellationToken);
        return Ok(resultado);
    }

    private int ObtenerFuncionarioIdActual()
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(subClaim) && int.TryParse(subClaim, out var id))
        {
            return id;
        }
        return 0;
    }
}

