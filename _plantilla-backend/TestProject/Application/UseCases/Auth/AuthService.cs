using PlantillaBackend.Application.DTOs.Auth;
using PlantillaBackend.Application.DTOs.Funcionarios;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;
using Microsoft.Extensions.Options;

namespace PlantillaBackend.Application.UseCases.Auth;

public class AuthService : IAuthService
{
    private readonly IFuncionarioRepository _repository;
    private readonly IRolRepository _rolRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITokenService _tokenService;
    private readonly IFuncionarioPreguntaSeguridadRepository _funcionarioPreguntasRepo;
    private readonly PasswordResetOptions _passwordResetOptions;

    public AuthService(
        IFuncionarioRepository repository,
        IRolRepository rolRepository,
        IPasswordHashService passwordHashService,
        ITokenService tokenService,
        IFuncionarioPreguntaSeguridadRepository funcionarioPreguntasRepo,
        IOptions<PasswordResetOptions> passwordResetOptions)
    {
        _repository = repository;
        _rolRepository = rolRepository;
        _passwordHashService = passwordHashService;
        _tokenService = tokenService;
        _funcionarioPreguntasRepo = funcionarioPreguntasRepo;
        _passwordResetOptions = passwordResetOptions.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterFuncionarioDto dto, CancellationToken cancellationToken = default)
    {
        var nac = dto.Snacionalidad.Trim().ToUpper();

        var existe = await _repository.ExistsCedulaAsync(nac, dto.NcedulaIdent, cancellationToken);
        if (existe)
        {
            throw new InvalidOperationException($"Ya existe un funcionario registrado con la cédula {nac}-{dto.NcedulaIdent}.");
        }

        var rol = await _rolRepository.GetByIdAsync(dto.RolId, cancellationToken);
        if (rol == null || !rol.Benabled)
        {
            throw new InvalidOperationException($"El rol con ID {dto.RolId} no existe o no se encuentra activo.");
        }

        var funcionario = new Funcionario
        {
            Snacionalidad = nac,
            NcedulaIdent = dto.NcedulaIdent,
            SprimerNombre = dto.SprimerNombre.Trim(),
            SsegundoNombre = string.IsNullOrWhiteSpace(dto.SsegundoNombre) ? null : dto.SsegundoNombre.Trim(),
            SprimerApellido = dto.SprimerApellido.Trim(),
            SsegundoApellido = string.IsNullOrWhiteSpace(dto.SsegundoApellido) ? null : dto.SsegundoApellido.Trim(),
            RolId = dto.RolId,
            Benabled = true
        };

        funcionario.SpasswordHash = _passwordHashService.HashPassword(funcionario, dto.Password);

        await _repository.AddAsync(funcionario, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var nuevo = await _repository.GetByIdWithDetailsAsync(funcionario.IdFuncionario, cancellationToken);

        return new AuthResponseDto
        {
            Success = true,
            Mensaje = "Cuenta creada. El usuario debe iniciar sesión y configurar sus preguntas de seguridad.",
            Funcionario = MapToDto(nuevo ?? funcionario),
            RequiereConfigurarPreguntasSeguridad = true,
            TotalPreguntasSeguridadConfiguradas = 0
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var nac = dto.Snacionalidad.Trim().ToUpper();

        var funcionario = await _repository.GetByCedulaAsync(nac, dto.NcedulaIdent, cancellationToken);
        if (funcionario == null || !funcionario.Benabled)
        {
            return null;
        }

        var valida = _passwordHashService.VerifyPassword(funcionario, funcionario.SpasswordHash, dto.Password);
        if (!valida)
        {
            return null;
        }

        // Verificación de exhortación de preguntas de seguridad
        var preguntasCount = await _funcionarioPreguntasRepo.CountByFuncionarioIdAsync(funcionario.IdFuncionario, cancellationToken);
        var requierePreguntas = preguntasCount == 0;

        var token = _tokenService.GenerateToken(funcionario, out var expiresInSeconds, requierePreguntas);

        return new AuthResponseDto
        {
            Success = true,
            Mensaje = requierePreguntas
                ? "Autenticación exitosa. Exhortación de seguridad: Debe configurar sus preguntas de seguridad para habilitar el acceso total."
                : "Autenticación exitosa.",
            Token = token,
            TokenType = "Bearer",
            ExpiresInSeconds = expiresInSeconds,
            Funcionario = MapToDto(funcionario),
            RequiereConfigurarPreguntasSeguridad = requierePreguntas,
            TotalPreguntasSeguridadConfiguradas = preguntasCount,
            RequiereCambiarContrasena = !string.IsNullOrEmpty(_passwordResetOptions.DefaultResetPassword)
                && dto.Password == _passwordResetOptions.DefaultResetPassword
        };
    }

    public async Task<bool> ResetPasswordAsync(int funcionarioId, CancellationToken cancellationToken = default)
    {
        var passwordTemporal = _passwordResetOptions.DefaultResetPassword;
        if (string.IsNullOrWhiteSpace(passwordTemporal))
        {
            throw new InvalidOperationException("No está configurada la contraseña temporal de restablecimiento.");
        }

        var funcionario = await _repository.GetByIdAsync(funcionarioId, cancellationToken);
        if (funcionario is null)
        {
            return false;
        }

        funcionario.SpasswordHash = _passwordHashService.HashPassword(funcionario, passwordTemporal);
        await _repository.UpdateAsync(funcionario, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static FuncionarioResponseDto MapToDto(Funcionario f) => new()
    {
        IdFuncionario = f.IdFuncionario,
        Snacionalidad = f.Snacionalidad,
        NcedulaIdent = f.NcedulaIdent,
        CedulaCompleta = f.CedulaCompleta,
        SprimerNombre = f.SprimerNombre,
        SsegundoNombre = f.SsegundoNombre,
        SprimerApellido = f.SprimerApellido,
        SsegundoApellido = f.SsegundoApellido,
        NombreCompleto = f.NombreCompleto,
        RolId = f.RolId,
        NombreRol = f.Rol?.Sdescripcion ?? string.Empty,
        Benabled = f.Benabled,
        DfechaCreacion = f.DfechaCreacion
    };
}
