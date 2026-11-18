using System.ComponentModel.DataAnnotations;

namespace PlantillaBackend.Application.DTOs.Profile;

public class ChangePasswordDto
{
    public int? FuncionarioId { get; set; }

    [Required(ErrorMessage = "La contraseña actual es requerida.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$", 
        ErrorMessage = "La nueva contraseña debe contener al menos una letra mayúscula, una letra minúscula y un número.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de la nueva contraseña es requerida.")]
    [Compare(nameof(NewPassword), ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

