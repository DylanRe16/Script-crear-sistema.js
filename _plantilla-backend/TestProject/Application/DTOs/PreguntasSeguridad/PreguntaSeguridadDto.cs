using System.ComponentModel.DataAnnotations;

namespace PlantillaBackend.Application.DTOs.PreguntasSeguridad;

public class PreguntaSeguridadResponseDto
{
    public int IdPreguntaSeg { get; set; }
    public string Sdescripcion { get; set; } = string.Empty;
    public bool Benabled { get; set; }
    public DateTime DfechaCreacion { get; set; }
}

public class CrearPreguntaSeguridadDto
{
    [Required(ErrorMessage = "La descripción de la pregunta es requerida.")]
    [StringLength(250, MinimumLength = 5, ErrorMessage = "La pregunta debe tener entre 5 y 250 caracteres.")]
    public string Sdescripcion { get; set; } = string.Empty;
}

public class ActualizarPreguntaSeguridadDto
{
    [Required(ErrorMessage = "La descripción de la pregunta es requerida.")]
    [StringLength(250, MinimumLength = 5, ErrorMessage = "La pregunta debe tener entre 5 y 250 caracteres.")]
    public string Sdescripcion { get; set; } = string.Empty;

    public bool Benabled { get; set; } = true;
}

