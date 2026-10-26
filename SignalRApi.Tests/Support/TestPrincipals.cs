using System.Security.Claims;

namespace SignalR.Api.Tests.Support
{
    public static class TestPrincipals
    {
        public static ClaimsPrincipal Admin { get; } = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        ], "Test"));

        public static ClaimsPrincipal NonAdmin { get; } = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "non-admin")
        ], "Test"));
    }
}
