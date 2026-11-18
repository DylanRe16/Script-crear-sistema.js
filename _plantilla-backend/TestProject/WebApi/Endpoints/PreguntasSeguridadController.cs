using Microsoft.AspNetCore.Mvc;
using PlantillaBackend.Application.DTOs.PreguntasSeguridad;
using PlantillaBackend.Application.UseCases.PreguntasSeguridad;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.WebApi.Endpoints;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class PreguntasSeguridadController : ControllerBase
{
    private readonly IPreguntaSeguridadService _service;
    private readonly ICurrentUserService _currentUserService;

    public PreguntasSeguridadController(
        IPreguntaSeguridadService service,
        ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lista el catálogo de preguntas de seguridad maestras.
    /// </summary>
    /// <param name="soloActivas">Si es true (por defecto), retorna solo preguntas activas</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PreguntaSeguridadResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PreguntaSeguridadResponseDto>>> GetAll(
        [FromQuery] bool soloActivas = true,
        CancellationToken cancellationToken = default)
    {
        var lista = await _service.GetAllAsync(soloActivas, cancellationToken);
        return Ok(lista);
    }

    /// <summary>
    /// Obtiene una pregunta de seguridad maestra por su ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PreguntaSeguridadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreguntaSeguridadResponseDto>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _service.GetByIdAsync(id, cancellationToken);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    /// <summary>
    /// Crea una nueva pregunta de seguridad en el catálogo maestro. Exclusivo para Administradores.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PreguntaSeguridadResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PreguntaSeguridadResponseDto>> Create(
        [FromBody] CrearPreguntaSeguridadDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized(new { success = false, mensaje = "Debe iniciar sesión para realizar esta operación." });
        }

        if (!_currentUserService.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                mensaje = "Acceso denegado: solo los usuarios administradores pueden crear preguntas de seguridad maestras."
            });
        }

        try
        {
            var resultado = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = resultado.IdPreguntaSeg }, resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza una pregunta de seguridad del catálogo. Exclusivo para Administradores.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PreguntaSeguridadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreguntaSeguridadResponseDto>> Update(
        int id,
        [FromBody] ActualizarPreguntaSeguridadDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized(new { success = false, mensaje = "Debe iniciar sesión para realizar esta operación." });
        }

        if (!_currentUserService.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                mensaje = "Acceso denegado: solo los usuarios administradores pueden modificar preguntas maestras."
            });
        }

        try
        {
            var resultado = await _service.UpdateAsync(id, dto, cancellationToken);
            return resultado is null ? NotFound() : Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Habilita o deshabilita una pregunta de seguridad. Exclusivo para Administradores.
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(
        int id,
        [FromQuery] bool benabled,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized(new { success = false, mensaje = "Debe iniciar sesión." });
        }

        if (!_currentUserService.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                mensaje = "Acceso denegado: solo los administradores pueden cambiar el estado de preguntas maestras."
            });
        }

        var ok = await _service.ToggleStatusAsync(id, benabled, cancellationToken);
        if (!ok) return NotFound();

        return Ok(new
        {
            success = true,
            mensaje = $"Pregunta {(benabled ? "habilitada" : "deshabilitada")} exitosamente."
        });
    }

    /// <summary>
    /// Deshabilita (soft-delete) una pregunta de seguridad del catálogo maestro. Exclusivo para Administradores.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await ToggleStatus(id, benabled: false, cancellationToken);
    }
}

