using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DataFlow.WebApi.Data;
using Microsoft.IdentityModel.Tokens;

namespace DataFlow.WebApi.Services;

public sealed class JwtService(IConfiguration config)
{
    private readonly string _secret = config["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret not configured");
    private readonly string _issuer  = config["Jwt:Issuer"]  ?? "DataFlowApi";
    private readonly string _audience = config["Jwt:Audience"] ?? "DataFlowApp";
    private readonly int _accessMinutes  = int.Parse(config["Jwt:AccessTokenMinutes"]  ?? "60");
    private readonly int _refreshDays    = int.Parse(config["Jwt:RefreshTokenDays"]    ?? "30");

    public string GenerateAccessToken(AppUser user, IList<string> roles)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("display_name", user.DisplayName),
            new("theme", user.Theme),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer:   _issuer,
            audience: _audience,
            claims:   claims,
            expires:  DateTime.UtcNow.AddMinutes(_accessMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string token, DateTime expires) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        return (token, DateTime.UtcNow.AddDays(_refreshDays));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
                ValidateIssuer   = true,
                ValidIssuer      = _issuer,
                ValidateAudience = true,
                ValidAudience    = _audience,
                ValidateLifetime = false   // allow validating expired tokens for refresh
            }, out _);
        }
        catch { return null; }
    }
}