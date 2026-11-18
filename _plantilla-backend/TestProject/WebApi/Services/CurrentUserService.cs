using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using PlantillaBackend.Domain.Abstractions;

namespace PlantillaBackend.WebApi.Services;

/// <summary>
/// Implementación de ICurrentUserService para resolver la identidad del usuario actual
/// a partir de los Claims del HttpContext (token JWT).
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Indica si la petición actual posee un ClaimsPrincipal autenticado.
    /// </summary>
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public int? FuncionarioId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var funcionarioId) && funcionarioId > 0
                ? funcionarioId
                : null;
        }
    }

    /// <summary>
    /// Retorna la descripción del rol del usuario autenticado, o null si no hay sesión activa.
    /// </summary>
    public string? Rol =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("rol")?.Value;

    /// <summary>
    /// Determina si el usuario autenticado posee el rol de Administrador.
    /// Valida contra los nombres usuales de rol y rol_id == 1 de forma insensible a mayúsculas/minúsculas.
    /// </summary>
    public bool IsAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            var rol = Rol;
            if (!string.IsNullOrWhiteSpace(rol) &&
                (rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                 rol.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Fallback por rol_id == 1 en los claims
            var claimRolId = user.FindFirst("rol_id")?.Value;
            if (int.TryParse(claimRolId, out var rolId) && rolId == 1)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Retorna la cédula numérica del usuario logueado, o 1 en su defecto.
    /// </summary>
    public int Cedula
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                return 1;
            }

            // 1. Intentar obtener el claim directo numérico 'cedula_ident'
            var claimCedulaIdent = user.FindFirst("cedula_ident")?.Value;
            if (!string.IsNullOrWhiteSpace(claimCedulaIdent) && int.TryParse(claimCedulaIdent, out var cedulaNum))
            {
                return cedulaNum > 0 ? cedulaNum : 1;
            }

            // 2. Intentar obtener el claim 'cedula' (ej. "V-18452136" o "18452136")
            var claimCedula = user.FindFirst("cedula")?.Value;
            if (!string.IsNullOrWhiteSpace(claimCedula))
            {
                if (int.TryParse(claimCedula, out var parsedDirecto) && parsedDirecto > 0)
                {
                    return parsedDirecto;
                }

                var soloDigitos = Regex.Replace(claimCedula, @"[^\d]", "");
                if (int.TryParse(soloDigitos, out var parsedDigitos) && parsedDigitos > 0)
                {
                    return parsedDigitos;
                }
            }

            return 1;
        }
    }
}

