namespace PlantillaBackend.Application.UseCases.Auth;

public sealed class PasswordResetOptions
{
    public const string SectionName = "SecuritySettings";

    public string? DefaultResetPassword { get; set; }
}
