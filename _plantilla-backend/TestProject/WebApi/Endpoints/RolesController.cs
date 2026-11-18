using Microsoft.AspNetCore.Mvc;
using PlantillaBackend.Application.DTOs.Funcionarios;
using PlantillaBackend.Application.UseCases.Users;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.WebApi.Endpoints;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;

    public RolesController(IUserManagementService userManagementService, ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
    }

    /// <summary>Lista los roles activos que un administrador puede asignar.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RolAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RolAdminResponse>>> GetAll(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized(new { success = false, mensaje = "Se requiere una sesión autenticada." });
        }

        if (!_currentUserService.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { success = false, mensaje = "Se requiere el rol Administrador." });
        }

        return Ok(await _userManagementService.GetRolesAsync(cancellationToken));
    }
}
