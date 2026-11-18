using System.ComponentModel.DataAnnotations;

namespace PlantillaBackend.Application.DTOs.PreguntasSeguridad;

/// <summary>
/// Elemento individual para asignar una respuesta a una pregunta de seguridad
/// </summary>
public class AsignarPreguntaItemDto
{
    [Required(ErrorMessage = "El identificador de la pregunta es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe suministrar un identificador de pregunta válido.")]
    public int IdPreguntaSeg { get; set; }

    [Required(ErrorMessage = "La respuesta a la pregunta de seguridad es requerida.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "La respuesta debe tener entre 2 y 200 caracteres.")]
    public string Respuesta { get; set; } = string.Empty;
}

/// <summary>
/// Lote de preguntas de seguridad a asociar por el funcionario (máximo 6 según regla de negocio)
/// </summary>
public class AsignarPreguntasSeguridadDto
{
    [Required(ErrorMessage = "Debe suministrar la lista de preguntas de seguridad.")]
    public List<AsignarPreguntaItemDto> Preguntas { get; set; } = new();
}

/// <summary>
/// Información pública/segura de las preguntas asociadas al funcionario.
/// POR SEGURIDAD: Nunca expone el texto ni el hash de la respuesta.
/// </summary>
public class FuncionarioPreguntaSeguridadResponseDto
{
    public int IdFpseguridad { get; set; }
    public int IdPreguntaSeg { get; set; }
    public string SdescripcionPregunta { get; set; } = string.Empty;
    public DateTime? DfechaReg { get; set; }
}

/// <summary>
/// Estado de cumplimiento de preguntas de seguridad del funcionario logueado
/// </summary>
public class EstadoPreguntasSeguridadDto
{
    public bool TienePreguntasConfiguradas { get; set; }
    public int TotalConfiguradas { get; set; }
    public int MaximoPermitido { get; set; } = 6;
    public bool RequiereConfiguracion { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>
/// DTO para validar una respuesta de seguridad en flujos de verificación o recuperación
/// </summary>
public class ValidarRespuestaSeguridadDto
{
    [Required(ErrorMessage = "El identificador de la pregunta es requerido.")]
    public int IdPreguntaSeg { get; set; }

    [Required(ErrorMessage = "La respuesta es requerida.")]
    public string Respuesta { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de validación de respuesta
/// </summary>
public class ResultadoValidacionRespuestaDto
{
    public bool Valida { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

