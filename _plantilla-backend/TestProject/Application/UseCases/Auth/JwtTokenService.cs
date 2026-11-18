using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Application.UseCases.Auth;

public class JwtTokenService : ITokenService
{
    private readonly byte[] _keyBytes;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationHours;

    public JwtTokenService(IConfiguration configuration)
    {
        var secret = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("Falta configurar JwtSettings:SecretKey.");
        if (Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("JwtSettings:SecretKey debe tener al menos 32 bytes.");
        }

        _issuer = configuration["JwtSettings:Issuer"] ?? "PlantillaBackend.Api";
        _audience = configuration["JwtSettings:Audience"] ?? "PlantillaBackend.Client";
        _expirationHours = int.TryParse(configuration["JwtSettings:ExpirationHours"], out var h) ? h : 8;

        _keyBytes = Encoding.UTF8.GetBytes(secret);
    }

    public string GenerateToken(Funcionario funcionario, out int expiresInSeconds, bool requierePreguntasSeguridad = false)
    {
        var now = DateTimeOffset.UtcNow;
        var exp = now.AddHours(_expirationHours);
        expiresInSeconds = (int)(exp - now).TotalSeconds;

        var header = new Dictionary<string, object>
        {
            { "alg", "HS256" },
            { "typ", "JWT" }
        };

        var payload = new Dictionary<string, object>
        {
            { "sub", funcionario.IdFuncionario.ToString() },
            { "name", funcionario.NombreCompleto },
            { "cedula", funcionario.CedulaCompleta },
            { "cedula_ident", funcionario.NcedulaIdent },
            { "rol", funcionario.Rol?.Sdescripcion ?? string.Empty },
            { "rol_id", funcionario.RolId },
            { "requiere_preguntas_seg", requierePreguntasSeguridad },
            { "iss", _issuer },
            { "aud", _audience },
            { "iat", now.ToUnixTimeSeconds() },
            { "exp", exp.ToUnixTimeSeconds() }
        };

        var headerBase64 = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var payloadBase64 = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));

        var unsignedToken = $"{headerBase64}.{payloadBase64}";
        var signatureBase64 = ComputeSignature(unsignedToken);

        return $"{unsignedToken}.{signatureBase64}";
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var parts = token.Trim().Split('.');
        if (parts.Length != 3)
            return null;

        var unsignedToken = $"{parts[0]}.{parts[1]}";
        var expectedSignature = ComputeSignature(unsignedToken);

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(parts[2]),
            Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return null;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(payloadBytes);
            var root = doc.RootElement;

            if (root.TryGetProperty("exp", out var expElement))
            {
                var expSeconds = expElement.GetInt64();
                var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (nowSeconds >= expSeconds)
                {
                    return null;
                }
            }

            var claims = new List<Claim>();

            if (root.TryGetProperty("sub", out var sub))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.GetString() ?? ""));

            if (root.TryGetProperty("name", out var name))
                claims.Add(new Claim(ClaimTypes.Name, name.GetString() ?? ""));

            if (root.TryGetProperty("cedula", out var cedula))
                claims.Add(new Claim("cedula", cedula.GetString() ?? ""));

            if (root.TryGetProperty("cedula_ident", out var cedulaIdent))
            {
                var val = cedulaIdent.ValueKind == JsonValueKind.Number 
                    ? cedulaIdent.GetInt32().ToString() 
                    : cedulaIdent.GetString();
                if (!string.IsNullOrWhiteSpace(val)) claims.Add(new Claim("cedula_ident", val));
            }

            if (root.TryGetProperty("rol", out var rol))
                claims.Add(new Claim(ClaimTypes.Role, rol.GetString() ?? ""));

            if (root.TryGetProperty("rol_id", out var rolId))
            {
                var val = rolId.ValueKind == JsonValueKind.Number 
                    ? rolId.GetInt32().ToString() 
                    : rolId.GetString();
                if (!string.IsNullOrWhiteSpace(val)) claims.Add(new Claim("rol_id", val));
            }

            if (root.TryGetProperty("requiere_preguntas_seg", out var reqPreg))
            {
                var boolVal = reqPreg.ValueKind == JsonValueKind.True ||
                              (reqPreg.ValueKind == JsonValueKind.String && string.Equals(reqPreg.GetString(), "true", StringComparison.OrdinalIgnoreCase));
                claims.Add(new Claim("requiere_preguntas_seg", boolVal.ToString().ToLowerInvariant()));
            }

            var identity = new ClaimsIdentity(claims, "Bearer");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }

    private string ComputeSignature(string unsignedToken)
    {
        using var hmac = new HMACSHA256(_keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }
}

