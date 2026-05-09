using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using AppEntity = Auth.Domain.Entities.Application;

namespace Auth.Persistence.Seed;

public static class ApplicationDbContextSeed
{
    private static readonly Guid DefaultAppId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
    private static readonly Guid AdminRoleId = Guid.Parse("B2C3D4E5-F6A7-8901-BCDE-F12345678901");
    private static readonly Guid UserManagerRoleId = Guid.Parse("C3D4E5F6-A7B8-9012-CDEF-123456789012");
    private static readonly Guid ViewerRoleId = Guid.Parse("D4E5F6A7-B8C9-0123-DEF1-234567890123");
    private static readonly Guid AdminUserId = Guid.Parse("E5F6A7B8-C9D0-1234-EF12-345678901234");
    private static readonly Guid ViewerUserId = Guid.Parse("F6A7B8C9-D0E1-2345-F123-456789012345");

    private static readonly Dictionary<string, Guid> PermissionIds = new()
    {
        ["users.read"] = Guid.Parse("A1B1C1D1-E1F1-1111-ABCD-111111111111"),
        ["users.create"] = Guid.Parse("A2B2C2D2-E2F2-2222-BCDE-222222222222"),
        ["users.update"] = Guid.Parse("A3B3C3D3-E3F3-3333-CDEF-333333333333"),
        ["users.delete"] = Guid.Parse("A4B4C4D4-E4F4-4444-DEF1-444444444444"),
        ["roles.read"] = Guid.Parse("B1C1D1E1-F1A1-5555-ABCD-555555555555"),
        ["roles.create"] = Guid.Parse("B2C2D2E2-F2A2-6666-BCDE-666666666666"),
        ["roles.update"] = Guid.Parse("B3C3D3E3-F3A3-7777-CDEF-777777777777"),
        ["roles.delete"] = Guid.Parse("B4C4D4E4-F4A4-8888-DEF1-888888888888"),
        ["permissions.read"] = Guid.Parse("C1D1E1F1-A1B1-9999-ABCD-999999999999"),
        ["permissions.assign"] = Guid.Parse("C2D2E2F2-A2B2-AAAA-BCDE-AAAAAAAAAAAA"),
        ["applications.read"] = Guid.Parse("D1E1F1A1-B1C1-BBBB-ABCD-BBBBBBBBBBBB"),
        ["applications.manage"] = Guid.Parse("D2E2F2A2-B2C2-CCCC-BCDE-CCCCCCCCCCCC"),
        ["tenants.read"] = Guid.Parse("E1F1A1B1-C1D1-DDDD-ABCD-DDDDDDDDDDDD"),
        ["tenants.manage"] = Guid.Parse("E2F2A2B2-C2D2-EEEE-BCDE-EEEEEEEEEEEE"),
        ["audit.read"] = Guid.Parse("F1A1B1C1-D1E1-FFFF-ABCD-FFFFFFFFFFFF"),
        ["system.settings"] = Guid.Parse("A1A2A3A4-B1B2-CCCC-AAAA-111122223333"),
        ["system.maintenance"] = Guid.Parse("B1B2B3B4-C1C2-DDDD-BBBB-222233334444"),
        ["system.logs"] = Guid.Parse("C1C2C3C4-D1D2-EEEE-CCCC-333344445555"),
    };

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Use a single transaction for all seed operations
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await SeedApplicationAsync(context);
            await SeedPermissionsAsync(context);
            await SeedRolesAsync(context);
            await SeedRolePermissionsAsync(context);
            await SeedUsersAsync(context);
            await SeedUserRolesAsync(context);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task SeedApplicationAsync(ApplicationDbContext context)
    {
        if (await context.Applications.AnyAsync())
            return;

        context.Applications.Add(new AppEntity(
            DefaultAppId,
            "Admin Portal",
            "admin-portal",
            "Default administration portal application",
            "default-api-key"));
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync())
            return;

        var permissions = new List<Permission>
        {
            // Users
            new(PermissionIds["users.read"], "users.read", "Read Users", "View user details", PermissionGroups.Users, true),
            new(PermissionIds["users.create"], "users.create", "Create Users", "Create new users", PermissionGroups.Users, true),
            new(PermissionIds["users.update"], "users.update", "Update Users", "Update existing users", PermissionGroups.Users, true),
            new(PermissionIds["users.delete"], "users.delete", "Delete Users", "Delete users", PermissionGroups.Users, true),

            // Roles
            new(PermissionIds["roles.read"], "roles.read", "Read Roles", "View role details", PermissionGroups.Roles, true),
            new(PermissionIds["roles.create"], "roles.create", "Create Roles", "Create new roles", PermissionGroups.Roles, true),
            new(PermissionIds["roles.update"], "roles.update", "Update Roles", "Update existing roles", PermissionGroups.Roles, true),
            new(PermissionIds["roles.delete"], "roles.delete", "Delete Roles", "Delete roles", PermissionGroups.Roles, true),

            // Permissions
            new(PermissionIds["permissions.read"], "permissions.read", "Read Permissions", "View permissions", PermissionGroups.Permissions, true),
            new(PermissionIds["permissions.assign"], "permissions.assign", "Assign Permissions", "Assign permissions to roles", PermissionGroups.Permissions, true),

            // Applications
            new(PermissionIds["applications.read"], "applications.read", "Read Applications", "View application details", PermissionGroups.Applications, true),
            new(PermissionIds["applications.manage"], "applications.manage", "Manage Applications", "Create and update applications", PermissionGroups.Applications, true),

            // Tenants
            new(PermissionIds["tenants.read"], "tenants.read", "Read Tenants", "View tenant details", PermissionGroups.Tenants, true),
            new(PermissionIds["tenants.manage"], "tenants.manage", "Manage Tenants", "Create and update tenants", PermissionGroups.Tenants, true),

            // Audit
            new(PermissionIds["audit.read"], "audit.read", "Read Audit Logs", "View audit logs", PermissionGroups.AuditLogs, true),

            // System
            new(PermissionIds["system.settings"], "system.settings", "System Settings", "Manage system-wide settings", PermissionGroups.System, true),
            new(PermissionIds["system.maintenance"], "system.maintenance", "System Maintenance", "Perform system maintenance tasks", PermissionGroups.System, true),
            new(PermissionIds["system.logs"], "system.logs", "System Logs", "View system logs", PermissionGroups.System, true),
        };

        context.Permissions.AddRange(permissions);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return;

        var roles = new List<Role>
        {
            new(AdminRoleId, "Admin", "System administrator with full access", DefaultAppId, true),
            new(UserManagerRoleId, "UserManager", "Can manage users and roles (no deletion or system settings)", DefaultAppId, true),
            new(ViewerRoleId, "Viewer", "Read-only access to all resources", DefaultAppId, true),
        };

        context.Roles.AddRange(roles);
    }

    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context)
    {
        if (await context.RolePermissions.AnyAsync())
            return;

        var rolePermissions = new List<RolePermission>();

        // Admin: all permissions
        foreach (var permissionId in PermissionIds.Values)
        {
            rolePermissions.Add(new RolePermission(AdminRoleId, permissionId));
        }

        // UserManager: read/write on users, roles, permissions (no delete)
        rolePermissions.AddRange(new[]
        {
            new RolePermission(UserManagerRoleId, PermissionIds["users.read"]),
            new RolePermission(UserManagerRoleId, PermissionIds["users.create"]),
            new RolePermission(UserManagerRoleId, PermissionIds["users.update"]),
            new RolePermission(UserManagerRoleId, PermissionIds["roles.read"]),
            new RolePermission(UserManagerRoleId, PermissionIds["roles.create"]),
            new RolePermission(UserManagerRoleId, PermissionIds["roles.update"]),
            new RolePermission(UserManagerRoleId, PermissionIds["permissions.read"]),
            new RolePermission(UserManagerRoleId, PermissionIds["permissions.assign"]),
            new RolePermission(UserManagerRoleId, PermissionIds["applications.read"]),
            new RolePermission(UserManagerRoleId, PermissionIds["tenants.read"]),
        });

        // Viewer: read-only
        rolePermissions.AddRange(new[]
        {
            new RolePermission(ViewerRoleId, PermissionIds["users.read"]),
            new RolePermission(ViewerRoleId, PermissionIds["roles.read"]),
            new RolePermission(ViewerRoleId, PermissionIds["permissions.read"]),
            new RolePermission(ViewerRoleId, PermissionIds["applications.read"]),
            new RolePermission(ViewerRoleId, PermissionIds["tenants.read"]),
            new RolePermission(ViewerRoleId, PermissionIds["audit.read"]),
        });

        context.RolePermissions.AddRange(rolePermissions);
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        var users = new List<User>
        {
            new(
                AdminUserId,
                "admin",
                "admin@authplatform.com",
                BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
                "System",
                "Administrator"),
            new(
                ViewerUserId,
                "viewer",
                "viewer@authplatform.com",
                BCrypt.Net.BCrypt.HashPassword("Viewer@123", workFactor: 12),
                "Read",
                "Only"),
        };

        context.Users.AddRange(users);
    }

    private static async Task SeedUserRolesAsync(ApplicationDbContext context)
    {
        if (await context.UserRoles.AnyAsync())
            return;

        var userRoles = new List<UserRole>
        {
            new(AdminUserId, AdminRoleId),
            new(ViewerUserId, ViewerRoleId),
        };

        context.UserRoles.AddRange(userRoles);
    }
}
