using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SmeBackend.Data;

namespace SmeBackend.Services;

/// Reject sessions whose access claims no longer match the saved account.
public static class CurrentUserTokenValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (!Guid.TryParse(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            context.Fail("Invalid account session.");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.Role, user.TenantId, user.BranchId, user.IsActive, TenantActive = user.Tenant.IsActive })
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
        if (user == null || !user.IsActive || !user.TenantActive ||
            principal?.FindFirst(ClaimTypes.Role)?.Value != user.Role.ToString() ||
            principal?.FindFirst("tenantId")?.Value != user.TenantId.ToString() ||
            (principal?.FindFirst("branchId")?.Value ?? "") != (user.BranchId?.ToString() ?? ""))
        {
            context.Fail("Account access has changed. Sign in again.");
        }
    }
}
