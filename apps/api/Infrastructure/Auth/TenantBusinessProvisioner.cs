namespace Documate.Api.Infrastructure.Auth;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ensures CorTenant / CorTenantBusiness product-extension rows exist for the authenticated Business.</summary>
public interface ITenantBusinessProvisioner
{
    Task EnsureAsync(IBusinessContext context, CancellationToken cancellationToken = default);
}

public sealed class TenantBusinessProvisioner(
    DocumateDbContext db,
    ICorEnumIdResolver enumIds) : ITenantBusinessProvisioner
{
    public async Task EnsureAsync(IBusinessContext context, CancellationToken cancellationToken = default)
    {
        if (!context.IsAuthenticated
            || string.IsNullOrWhiteSpace(context.TenantId)
            || string.IsNullOrWhiteSpace(context.BusinessId))
        {
            return;
        }

        var mode1Id = enumIds.Require("provider_mode", "mode_1");
        var tenantName = context.TenantName ?? "Tenant";
        var businessName = context.BusinessName ?? "Business";

        var tenant = await db.CorTenants
            .FirstOrDefaultAsync(t => t.IdenTenantId == context.TenantId, cancellationToken);

        if (tenant is null)
        {
            tenant = new CorTenant
            {
                IdenTenantId = context.TenantId,
                Name = tenantName,
                ProviderModeEnumId = mode1Id,
                IsActive = true,
                CreatedByUserId = context.UserId,
                UpdatedByUserId = context.UserId,
            };
            db.CorTenants.Add(tenant);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (!string.Equals(tenant.Name, tenantName, StringComparison.Ordinal))
        {
            tenant.Name = tenantName;
            tenant.UpdatedByUserId = context.UserId;
            await db.SaveChangesAsync(cancellationToken);
        }

        var business = await db.CorTenantBusinesses
            .FirstOrDefaultAsync(b => b.IdenBusinessId == context.BusinessId, cancellationToken);

        if (business is null)
        {
            db.CorTenantBusinesses.Add(new CorTenantBusiness
            {
                TenantId = tenant.Id,
                IdenBusinessId = context.BusinessId,
                Name = businessName,
                TenantName = tenant.Name,
                IsActive = true,
                CreatedByUserId = context.UserId,
                UpdatedByUserId = context.UserId,
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var dirty = false;
            if (!string.Equals(business.Name, businessName, StringComparison.Ordinal))
            {
                business.Name = businessName;
                dirty = true;
            }

            if (!string.Equals(business.TenantName, tenant.Name, StringComparison.Ordinal))
            {
                business.TenantName = tenant.Name;
                dirty = true;
            }

            if (dirty)
            {
                business.UpdatedByUserId = context.UserId;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
