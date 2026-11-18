using System.ComponentModel.DataAnnotations;

namespace PlantillaBackend.Application.DTOs.Auth;

public class RegisterFuncionarioDto
{
    [Required(ErrorMessage = "La nacionalidad es requerida.")]
    [RegularExpression("^[VEJvej]$", ErrorMessage = "La nacionalidad debe ser 'V', 'E' o 'J'.")]
    public string Snacionalidad { get; set; } = "V";

    [Required(ErrorMessage = "La cédula de identidad es requerida.")]
    [Range(100_000, 999_999_999, ErrorMessage = "La cédula de identidad debe tener un valor numérico válido.")]
    public int NcedulaIdent { get; set; }

    [Required(ErrorMessage = "El primer nombre es requerido.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El primer nombre debe tener entre 2 y 100 caracteres.")]
    public string SprimerNombre { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo nombre no puede exceder 100 caracteres.")]
    public string? SsegundoNombre { get; set; }

    [Required(ErrorMessage = "El primer apellido es requerido.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El primer apellido debe tener entre 2 y 100 caracteres.")]
    public string SprimerApellido { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo apellido no puede exceder 100 caracteres.")]
    public string? SsegundoApellido { get; set; }

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$", 
        ErrorMessage = "La contraseña debe contener al menos una letra mayúscula, una letra minúscula y un número.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de la contraseña es requerida.")]
    [Compare(nameof(Password), ErrorMessage = "La confirmación de la contraseña no coincide.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "El identificador de rol es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe asignar un rol válido.")]
    public int RolId { get; set; }

}

public class LoginDto
{
    public string Snacionalidad { get; set; } = "V";

    [Required(ErrorMessage = "La cédula de identidad es requerida.")]
    [Range(1, 999_999_999, ErrorMessage = "El número de cédula debe ser válido.")]
    public int NcedulaIdent { get; set; }

    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = string.Empty;
}

