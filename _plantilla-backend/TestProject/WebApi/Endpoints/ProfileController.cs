using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PlantillaBackend.Application.DTOs.Funcionarios;
using PlantillaBackend.Application.DTOs.Profile;
using PlantillaBackend.Application.UseCases.Profile;

namespace PlantillaBackend.WebApi.Endpoints;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto, 
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        int funcionarioId = 0;
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(subClaim) && int.TryParse(subClaim, out var idFromToken))
        {
            funcionarioId = idFromToken;
        }
        else if (dto.FuncionarioId.HasValue && dto.FuncionarioId.Value > 0)
        {
            funcionarioId = dto.FuncionarioId.Value;
        }

        if (funcionarioId <= 0)
        {
            return Unauthorized(new 
            { 
                success = false, 
                mensaje = "Debe proporcionar un token Bearer o especificar el ID de funcionario." 
            });
        }

        try
        {
            await _profileService.ChangePasswordAsync(funcionarioId, dto, cancellationToken);
            return Ok(new { success = true, mensaje = "Contraseña actualizada exitosamente." });
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

    [HttpGet("me")]
    public async Task<ActionResult<FuncionarioResponseDto>> GetCurrentProfile(CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out var funcionarioId))
        {
            return Unauthorized(new { success = false, mensaje = "Sesión no autenticada o token inválido." });
        }

        var profile = await _profileService.GetProfileAsync(funcionarioId, cancellationToken);
        if (profile == null)
        {
            return NotFound(new { success = false, mensaje = "Funcionario no encontrado." });
        }

        return Ok(profile);
    }
}

