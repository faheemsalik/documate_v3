namespace Documate.Api.Infrastructure.Persistence.Seeding;

using Documate.Api.Domain;
using Microsoft.EntityFrameworkCore;

public static class CorEnumSeeder
{
    public static async Task SeedSystemAsync(DocumateDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var (typeKey, typeName, values) in CorEnumSeedCatalog.Types)
        {
            var type = await db.CorEnumTypes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.EnumTypeKey == typeKey, cancellationToken);

            if (type is null)
            {
                type = new CorEnumType
                {
                    EnumTypeKey = typeKey,
                    Name = typeName,
                    Scope = "system",
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.CorEnumTypes.Add(type);
                await db.SaveChangesAsync(cancellationToken);
            }
            else if (type.IsDeleted)
            {
                type.IsDeleted = false;
                type.DeletedAt = null;
                type.IsActive = true;
                type.UpdatedAt = now;
                await db.SaveChangesAsync(cancellationToken);
            }

            foreach (var (enumKey, name) in values)
            {
                var row = await db.CorEnums
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(
                        e => e.TypeId == type.Id && e.EnumKey == enumKey && e.BusinessId == null,
                        cancellationToken);

                if (row is null)
                {
                    db.CorEnums.Add(new CorEnum
                    {
                        TypeId = type.Id,
                        EnumKey = enumKey,
                        Name = name,
                        BusinessId = null,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }
                else if (row.IsDeleted)
                {
                    row.IsDeleted = false;
                    row.DeletedAt = null;
                    row.Name = name;
                    row.UpdatedAt = now;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
