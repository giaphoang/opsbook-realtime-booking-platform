using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SignalR.Api.Tests.Support
{
    /// <summary>
    /// Creates JWTs that match the API's configured bearer validation
    /// (issuer/audience "realtime-ops", symmetric Registrar signing key).
    /// </summary>
    public static class JwtTokenFactory
    {
        public const string DefaultSigningKey = "super-secret-key-at-least-32-characters!";

        public static string CreateToken(
            string uniqueName,
            string audience = "realtime-ops",
            string issuer = "realtime-ops",
            string? signingKey = null,
            DateTime? expires = null,
            DateTime? notBefore = null)
        {
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? DefaultSigningKey)),
                SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var name = uniqueName.Length > 8 ? uniqueName[..8] : uniqueName;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, uniqueName),
                new(ClaimTypes.Name, $"test-user-{name}"),
                new(ClaimTypes.Role, "Admin"),
                new(ClaimTypes.Email, $"test-user-{name}@test")
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: notBefore ?? (expires.HasValue && expires.Value < now
                    ? expires.Value.AddMinutes(-1)
                    : now.AddMinutes(-1)),
                expires: expires ?? now.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
