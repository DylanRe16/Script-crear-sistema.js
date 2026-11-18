namespace PlantillaBackend.Domain.Abstractions;

/// <summary>
/// Contrato para obtener la identidad, cédula y perfil del usuario autenticado en la sesión actual.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Cédula numérica del funcionario autenticado, o 1 si no hay sesión activa.
    /// </summary>
    int Cedula { get; }

    /// <summary>
    /// Identificador del funcionario autenticado, o null si el token no contiene un identificador válido.
    /// </summary>
    int? FuncionarioId { get; }

    /// <summary>
    /// Indica si la petición actual proviene de un usuario autenticado.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Nombre o descripción del rol asignado (ej. "Administrador"), o null si no está autenticado.
    /// </summary>
    string? Rol { get; }

    /// <summary>
    /// Indica si el usuario autenticado posee privilegios de Administrador.
    /// </summary>
    bool IsAdmin { get; }
}

