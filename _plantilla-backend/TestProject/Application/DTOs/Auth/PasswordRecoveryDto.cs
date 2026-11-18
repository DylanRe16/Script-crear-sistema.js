using System.ComponentModel.DataAnnotations;

namespace PlantillaBackend.Application.DTOs.Auth;

/// <summary>
/// Datos mínimos para identificar al funcionario en el flujo de recuperación de contraseña.
/// </summary>
public class SolicitarRecuperacionDto
{
    [Required(ErrorMessage = "La nacionalidad es requerida.")]
    [RegularExpression("^[VEJvej]$", ErrorMessage = "La nacionalidad debe ser 'V', 'E' o 'J'.")]
    public string Snacionalidad { get; set; } = "V";

    [Required(ErrorMessage = "La cédula de identidad es requerida.")]
    [Range(1, 999_999_999, ErrorMessage = "El número de cédula debe ser válido.")]
    public int NcedulaIdent { get; set; }
}

/// <summary>
/// Pregunta de seguridad que el funcionario debe responder para recuperar su contraseña.
/// No expone la respuesta ni su hash.
/// </summary>
public class PreguntaRecuperacionDto
{
    public int IdPreguntaSeg { get; set; }
    public string Sdescripcion { get; set; } = string.Empty;
}

/// <summary>
/// Preguntas configuradas por el funcionario para el proceso de recuperación.
/// </summary>
public class PreguntasRecuperacionResponseDto
{
    public bool TienePreguntasConfiguradas { get; set; }
    public int TotalPreguntas { get; set; }
    public List<PreguntaRecuperacionDto> Preguntas { get; set; } = new();
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta suministrada por el funcionario para una pregunta de seguridad.
/// </summary>
public class RespuestaRecuperacionItemDto
{
    [Required(ErrorMessage = "El identificador de la pregunta es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe suministrar un identificador de pregunta válido.")]
    public int IdPreguntaSeg { get; set; }

    [Required(ErrorMessage = "La respuesta a la pregunta de seguridad es requerida.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "La respuesta debe tener entre 2 y 200 caracteres.")]
    public string Respuesta { get; set; } = string.Empty;
}

/// <summary>
/// Solicitud para validar las respuestas de seguridad antes de mostrar el
/// formulario de la nueva contraseña.
/// </summary>
public class ValidarRespuestasRecuperacionDto : SolicitarRecuperacionDto
{
    [Required(ErrorMessage = "Debe suministrar las respuestas de seguridad.")]
    [MinLength(1, ErrorMessage = "Debe suministrar al menos una respuesta de seguridad.")]
    public List<RespuestaRecuperacionItemDto> Respuestas { get; set; } = new();
}

/// <summary>
/// Resultado de la validación de las respuestas de seguridad. El token permite
/// restablecer la contraseña sin volver a enviar las respuestas.
/// </summary>
public class ValidacionRespuestasResponseDto
{
    public bool Success { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string TokenRecuperacion { get; set; } = string.Empty;
    public int ExpiraEnSegundos { get; set; }
}

/// <summary>
/// Solicitud de restablecimiento de contraseña una vez validadas las respuestas
/// de seguridad.
/// </summary>
public class RestablecerPasswordRecuperacionDto
{
    [Required(ErrorMessage = "El token de recuperación es requerido.")]
    public string TokenRecuperacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",
        ErrorMessage = "La nueva contraseña debe contener al menos una letra mayúscula, una letra minúscula y un número.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de la nueva contraseña es requerida.")]
    [Compare(nameof(NewPassword), ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Resultado del restablecimiento de contraseña por recuperación.
/// </summary>
public class RecuperacionPasswordResultDto
{
    public bool Success { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
