using System.Text;
using Maratona.Api.Data.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Maratona.Api.Auth;

public class JwtTokens(IConfiguration config)
{
    public const string Issuer = "maratona";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public static SymmetricSecurityKey Key(IConfiguration config)
    {
        var secret = config["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET não configurado.");
        var bytes = Encoding.UTF8.GetBytes(secret);
        if (bytes.Length < 32)
        {
            throw new InvalidOperationException("JWT_SECRET precisa ter pelo menos 32 caracteres.");
        }
        return new SymmetricSecurityKey(bytes);
    }

    public string Create(User user) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = Issuer,
        Audience = Issuer,
        Expires = DateTime.UtcNow + Lifetime,
        Claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.UniqueName] = user.Username
        },
        SigningCredentials = new SigningCredentials(Key(config), SecurityAlgorithms.HmacSha256)
    });
}
