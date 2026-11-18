using Microsoft.AspNetCore.Mvc;
using PlantillaBackend.Application.DTOs.Auth;
using PlantillaBackend.Application.UseCases.Auth;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.WebApi.Endpoints;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordRecoveryService _passwordRecoveryService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        IAuthService authService,
        IPasswordRecoveryService passwordRecoveryService,
        ICurrentUserService currentUserService)
    {
        _authService = authService;
        _passwordRecoveryService = passwordRecoveryService;
        _currentUserService = currentUserService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterFuncionarioDto dto, 
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized(new { success = false, mensaje = "Se requiere una sesión autenticada." });
        }

        if (!_currentUserService.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { success = false, mensaje = "Se requiere el rol Administrador." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.RegisterAsync(dto, cancellationToken);
            return Created($"{Request.PathBase}/Funcionarios/{result.Funcionario.IdFuncionario}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, mensaje = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto, 
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(dto, cancellationToken);
        if (result == null)
        {
            return Unauthorized(new 
            { 
                success = false, 
                mensaje = "Cédula o contraseña inválida." 
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene las preguntas de seguridad configuradas por el funcionario para iniciar
    /// el proceso de recuperación de contraseña.
    /// </summary>
    [HttpPost("recuperacion/preguntas")]
    [ProducesResponseType(typeof(PreguntasRecuperacionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PreguntasRecuperacionResponseDto>> ObtenerPreguntasRecuperacion(
        [FromBody] SolicitarRecuperacionDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _passwordRecoveryService.ObtenerPreguntasAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Valida las respuestas de las 2 preguntas de seguridad seleccionadas al azar.
    /// Si son correctas, devuelve un token temporal para cambiar la contraseña.
    /// </summary>
    [HttpPost("recuperacion/validar")]
    [ProducesResponseType(typeof(ValidacionRespuestasResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ValidacionRespuestasResponseDto>> ValidarRespuestasRecuperacion(
        [FromBody] ValidarRespuestasRecuperacionDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _passwordRecoveryService.ValidarRespuestasAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Restablece la contraseña del funcionario una vez validadas sus respuestas
    /// de seguridad.
    /// </summary>
    [HttpPost("recuperacion/restablecer")]
    [ProducesResponseType(typeof(RecuperacionPasswordResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecuperacionPasswordResultDto>> RestablecerPasswordRecuperacion(
        [FromBody] RestablecerPasswordRecuperacionDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _passwordRecoveryService.RestablecerPasswordAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
    }
}

