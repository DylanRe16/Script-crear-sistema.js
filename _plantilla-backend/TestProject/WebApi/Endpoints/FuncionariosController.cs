using Microsoft.AspNetCore.Mvc;
using PlantillaBackend.Application.UseCases.Auth;
using PlantillaBackend.Application.DTOs.Funcionarios;
using PlantillaBackend.Application.UseCases.PreguntasSeguridad;
using PlantillaBackend.Application.UseCases.Users;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.WebApi.Endpoints;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class FuncionariosController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly IAuthService _authService;
    private readonly IFuncionarioPreguntaSeguridadService _securityService;
    private readonly ICurrentUserService _currentUserService;

    public FuncionariosController(
        IUserManagementService userManagementService,
        IAuthService authService,
        IFuncionarioPreguntaSeguridadService securityService,
        ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _authService = authService;
        _securityService = securityService;
        _currentUserService = currentUserService;
    }

    /// <summary>Lista las cuentas de usuario para la administración.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FuncionarioAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<FuncionarioAdminResponse>>> GetAll(
        [FromQuery] bool soloActivos = false,
        CancellationToken cancellationToken = default)
    {
        if (ValidarAdministrador() is { } resultado) return resultado;
        return Ok(await _userManagementService.GetUsersAsync(soloActivos, cancellationToken));
    }

    /// <summary>Obtiene una cuenta por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FuncionarioAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FuncionarioAdminResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        if (ValidarAdministrador() is { } resultado) return resultado;
        var funcionario = await _userManagementService.GetUserAsync(id, cancellationToken);
        return funcionario is null ? NotFound() : Ok(funcionario);
    }

    /// <summary>Actualiza datos, rol y estado de una cuenta.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(FuncionarioAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FuncionarioAdminResponse>> Update(
        int id,
        [FromBody] ActualizarFuncionarioAdminRequest request,
        CancellationToken cancellationToken)
    {
        if (ValidarAdministrador() is { } resultado) return resultado;
        if (EsLaCuentaActual(id)) return AccionSobreCuentaPropiaNoPermitida();

        try
        {
            var funcionario = await _userManagementService.UpdateUserAsync(id, request, cancellationToken);
            return funcionario is null ? NotFound() : Ok(funcionario);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
    }

    /// <summary>Restablece la contraseña de una cuenta a la contraseña temporal configurada en el backend.</summary>
    [HttpPost("{id:int}/restablecer-contrasena")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken cancellationToken)
    {
        if (ValidarAdministrador() is { } resultado) return resultado;
        if (EsLaCuentaActual(id)) return AccionSobreCuentaPropiaNoPermitida();

        try
        {
            var restablecida = await _authService.ResetPasswordAsync(id, cancellationToken);
            return restablecida
                ? Ok(new { success = true, mensaje = "La contraseña fue restablecida." })
                : NotFound(new { success = false, mensaje = "La cuenta no existe." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, mensaje = ex.Message });
        }
    }

    /// <summary>Elimina las preguntas de seguridad configuradas para una cuenta.</summary>
    [HttpPost("{id:int}/restablecer-preguntas-seguridad")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetSecurityQuestions(int id, CancellationToken cancellationToken)
    {
        if (ValidarAdministrador() is { } resultado) return resultado;
        if (EsLaCuentaActual(id)) return AccionSobreCuentaPropiaNoPermitida();

        var restablecidas = await _securityService.RestablecerPreguntasUsuarioAsync(id, cancellationToken);
        return restablecidas
            ? Ok(new { success = true, mensaje = "Las preguntas de seguridad fueron restablecidas." })
            : NotFound(new { success = false, mensaje = "La cuenta no existe." });
    }

    private bool EsLaCuentaActual(int id) => _currentUserService.FuncionarioId == id;

    private ObjectResult AccionSobreCuentaPropiaNoPermitida() =>
        StatusCode(StatusCodes.Status403Forbidden, new
        {
            success = false,
            mensaje = "No se permite modificar la propia cuenta desde la administración."
        });

    private ActionResult? ValidarAdministrador()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized(new { success = false, mensaje = "Se requiere una sesión autenticada." });
        }

        if (_currentUserService.FuncionarioId is null)
        {
            return Unauthorized(new { success = false, mensaje = "La sesión no contiene un identificador válido." });
        }

        return _currentUserService.IsAdmin
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new { success = false, mensaje = "Se requiere el rol Administrador." });
    }
}
