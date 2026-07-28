namespace CorePortfolio.API.Features.Admin.ControlPlane;

public static class AdminPermissionCatalog
{
    public const string AdminAccess = "Admin.Access";
    public const string AuditRead = "Audit.Read";
    public const string OperationsRead = "Operations.Read";
    public const string OperationsExecute = "Operations.Execute";
    public const string UsersRead = "Users.Read";
    public const string UsersManage = "Users.Manage";
    public const string SessionsRevoke = "Sessions.Revoke";
    public const string MarketDataRead = "MarketData.Read";
    public const string MarketDataManage = "MarketData.Manage";
    public const string NotificationsManage = "Notifications.Manage";
    public const string IntegrityRead = "Integrity.Read";
    public const string IntegrityRepair = "Integrity.Repair";
    public const string BackupsRead = "Backups.Read";
    public const string BackupsCreate = "Backups.Create";
    public const string BackupsRestore = "Backups.Restore";
    public const string MigrationsExecute = "Migrations.Execute";
    public const string SettingsManage = "Settings.Manage";
    public const string RolesManage = "Roles.Manage";
    public const string TwoFactorReset = "TwoFactor.Reset";

    public static readonly string[] All =
    [
        AdminAccess, AuditRead, OperationsRead, OperationsExecute, UsersRead, UsersManage,
        SessionsRevoke, MarketDataRead, MarketDataManage, NotificationsManage,
        IntegrityRead, IntegrityRepair, BackupsRead, BackupsCreate, BackupsRestore,
        MigrationsExecute, SettingsManage, RolesManage, TwoFactorReset
    ];

    private static readonly IReadOnlyDictionary<string, string[]> ByRole =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["SuperAdmin"] = All,
            ["Admin"] = All
                .Where(permission => permission != TwoFactorReset)
                .ToArray(),
            ["Operations"] =
            [
                AdminAccess, AuditRead, OperationsRead, OperationsExecute, MarketDataRead,
                MarketDataManage, NotificationsManage, IntegrityRead, BackupsRead, BackupsCreate
            ],
            ["Support"] = [AdminAccess, AuditRead, UsersRead, SessionsRevoke, NotificationsManage],
            ["MarketDataManager"] =
                [AdminAccess, AuditRead, OperationsRead, MarketDataRead, MarketDataManage],
            ["Auditor"] =
                [AdminAccess, AuditRead, OperationsRead, UsersRead, MarketDataRead,
                    IntegrityRead, BackupsRead],
            ["User"] = []
        };

    public static IReadOnlyList<string> GetForRole(string? role) =>
        role is not null && ByRole.TryGetValue(role, out var permissions) ? permissions : [];

    public static bool Has(string? role, string permission) =>
        GetForRole(role).Contains(permission, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Roles => ByRole.Keys.OrderBy(item => item).ToArray();
}
