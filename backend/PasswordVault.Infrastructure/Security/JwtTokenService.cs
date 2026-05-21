using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PasswordVault.Application.Interfaces;

namespace PasswordVault.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly SecuritySettings _settings;

    public JwtTokenService(IOptions<SecuritySettings> opts) => _settings = opts.Value;

    public string GenerateAccessToken(int userId, string email, string fullName)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name,  fullName),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim("userId", userId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:   _settings.JwtIssuer,
            audience: _settings.JwtAudience,
            claims:   claims,
            expires:  DateTime.UtcNow.AddMinutes(_settings.JwtExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public int? ValidateAccessToken(string token)
    {
        try
        {
            var key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.JwtSecret));
            var handler   = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = _settings.JwtIssuer,
                ValidAudience            = _settings.JwtAudience,
                IssuerSigningKey         = key,
                ClockSkew                = TimeSpan.Zero
            }, out _);

            var sub = principal.FindFirst("userId")?.Value;
            return sub is not null ? int.Parse(sub) : null;
        }
        catch { return null; }
    }
}
