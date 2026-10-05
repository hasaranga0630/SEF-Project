using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SmeBackend.Models;
using SmeBackend.Services;

namespace SmeBackend.Tests;

public class CurrentUserTokenValidatorTests
{
    [Theory]
    [InlineData("unchanged", false)]
    [InlineData("role", true)]
    [InlineData("branch", true)]
    [InlineData("inactive", true)]
    [InlineData("tenant", true)]
    public async Task SavedAccessChangesInvalidateExistingSession(string change, bool rejected)
    {
        using var db = TestHelpers.NewInMemoryDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test business" };
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Tenant = tenant,
            Role = UserRole.Staff, BranchId = Guid.NewGuid() };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, "Staff"),
            new Claim("tenantId", tenant.Id.ToString()),
            new Claim("branchId", user.BranchId.Value.ToString())
        };
        switch (change)
        {
            case "role": user.Role = UserRole.Manager; break;
            case "branch": user.BranchId = Guid.NewGuid(); break;
            case "inactive": user.IsActive = false; break;
            case "tenant": tenant.IsActive = false; break;
        }
        await db.SaveChangesAsync();
        using var services = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        var context = new TokenValidatedContext(http,
            new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), new JwtBearerOptions())
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
        };
        await CurrentUserTokenValidator.ValidateAsync(context);
        Assert.Equal(rejected, context.Result?.Failure != null);
    }
}
