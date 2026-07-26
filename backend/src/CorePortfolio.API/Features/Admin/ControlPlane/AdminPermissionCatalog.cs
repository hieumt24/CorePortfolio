namespace CorePortfolio.API.Features.Admin.ControlPlane;

public static class AdminPermissionCatalog
{
    public static readonly string[] All =
    [
        "Audit.Read", "Operations.Read", "Operations.Execute", "Users.Read", "Users.Manage",
        "Sessions.Revoke", "MarketData.Read", "MarketData.Manage", "Notifications.Manage",
        "Integrity.Read", "Integrity.Repair", "Backups.Read", "Backups.Create",
        "Backups.Restore", "Settings.Manage", "Roles.Manage"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> ByRole =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["SuperAdmin"] = All,
            ["Admin"] = All,
            ["Operations"] =
            [
                "Audit.Read", "Operations.Read", "Operations.Execute", "MarketData.Read",
                "MarketData.Manage", "Notifications.Manage", "Integrity.Read",
                "Backups.Read", "Backups.Create"
            ],
            ["Support"] = ["Audit.Read", "Users.Read", "Sessions.Revoke", "Notifications.Manage"],
            ["MarketDataManager"] =
                ["Audit.Read", "Operations.Read", "MarketData.Read", "MarketData.Manage"],
            ["Auditor"] =
                ["Audit.Read", "Operations.Read", "Users.Read", "MarketData.Read",
                    "Integrity.Read", "Backups.Read"],
            ["User"] = []
        };

    public static IReadOnlyList<string> GetForRole(string? role) =>
        role is not null && ByRole.TryGetValue(role, out var permissions) ? permissions : [];

    public static bool Has(string? role, string permission) =>
        GetForRole(role).Contains(permission, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Roles => ByRole.Keys.OrderBy(item => item).ToArray();
}
