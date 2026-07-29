namespace CorePortfolio.Domain.Analytics;

public static class TargetAllocationPlanStatuses
{
    public const string NotConfigured = "NotConfigured";
    public const string Complete = "Complete";
    public const string Invalid = "Invalid";
}

public sealed record TargetAllocationWeight(Guid CategoryId, decimal Percentage);

public sealed record TargetAllocationPlanAssessment(
    string Status,
    decimal TotalPercentage,
    bool IsActionable,
    string? Reason);

public static class TargetAllocationPolicy
{
    private const decimal PercentageTolerance = 0.01m;

    public static TargetAllocationPlanAssessment Evaluate(
        IEnumerable<TargetAllocationWeight> allocations)
    {
        var rows = allocations.ToList();
        var total = rows.Sum(row => row.Percentage);
        if (rows.GroupBy(row => row.CategoryId).Any(group => group.Count() > 1))
        {
            return Invalid("Mỗi nhóm tài sản chỉ được xuất hiện một lần.");
        }

        if (rows.Any(row => row.Percentage is < 0m or > 100m))
        {
            return Invalid("Tỷ trọng mục tiêu phải nằm trong khoảng từ 0% đến 100%.");
        }

        if (rows.All(row => row.Percentage == 0m))
        {
            return new TargetAllocationPlanAssessment(
                TargetAllocationPlanStatuses.NotConfigured,
                0m,
                false,
                "Chưa thiết lập kế hoạch phân bổ mục tiêu.");
        }

        if (Math.Abs(total - 100m) > PercentageTolerance)
        {
            return Invalid("Tổng tỷ trọng mục tiêu phải bằng 100% hoặc bằng 0% để xóa kế hoạch.");
        }

        return new TargetAllocationPlanAssessment(
            TargetAllocationPlanStatuses.Complete,
            100m,
            true,
            null);

        TargetAllocationPlanAssessment Invalid(string reason) =>
            new(TargetAllocationPlanStatuses.Invalid, total, false, reason);
    }

    public static bool IsOutsideTolerance(
        decimal currentPercentage,
        decimal targetPercentage,
        decimal tolerancePercentagePoints) =>
        Math.Abs(currentPercentage - targetPercentage) > tolerancePercentagePoints;
}
