using CorePortfolio.API.Features.Analytics.GetAnalyticsOverview;
using CorePortfolio.Domain.Analytics;

namespace CorePortfolio.API.Features.Analytics.GetAnalyticsInsights;

public sealed record AnalyticsInsightEvidenceDto(
    string Key,
    string Label,
    decimal Value,
    string Unit,
    string Source);

public sealed record AnalyticsInsightActionDto(
    string Label,
    string Href);

public sealed record AnalyticsInsightDto(
    string Code,
    string Category,
    string Severity,
    string Confidence,
    int Priority,
    string Title,
    string Observation,
    string Interpretation,
    string WhyItMatters,
    IReadOnlyList<AnalyticsInsightEvidenceDto> Evidence,
    IReadOnlyList<string> Limitations,
    AnalyticsInsightActionDto? Action);

public sealed record AnalyticsInsightsSummaryDto(
    int TotalCount,
    int CriticalCount,
    int WarningCount,
    int InfoCount,
    int PositiveCount);

public sealed record AnalyticsInsightsDto(
    AnalyticsScopeDto Scope,
    DateTime GeneratedAt,
    string MethodologyVersion,
    string MethodologyDescription,
    string Disclaimer,
    AnalyticsInsightsSummaryDto Summary,
    IReadOnlyList<AnalyticsInsightDto> Items);

public static class AnalyticsInsightPresenter
{
    public static AnalyticsInsightsDto Create(
        AnalyticsScopeDto scope,
        IReadOnlyList<AnalyticsInsightFinding> findings,
        DateTime generatedAt)
    {
        var items = findings.Select(finding => ToDto(
            finding,
            scope.FinancialHealthIsGlobal,
            scope.Currency)).ToList();
        return new AnalyticsInsightsDto(
            scope,
            generatedAt,
            "rules-v1",
            "Các quy tắc xác định được đánh giá trên dữ liệu trong phạm vi đã chọn; không dùng mô hình dự báo hoặc dữ liệu bên ngoài.",
            "Thông tin chỉ nhằm hỗ trợ rà soát. Đây không phải khuyến nghị đầu tư, tư vấn thuế hoặc chỉ dẫn giao dịch.",
            new AnalyticsInsightsSummaryDto(
                items.Count,
                items.Count(item => item.Severity == AnalyticsInsightSeverities.Critical),
                items.Count(item => item.Severity == AnalyticsInsightSeverities.Warning),
                items.Count(item => item.Severity == AnalyticsInsightSeverities.Info),
                items.Count(item => item.Severity == AnalyticsInsightSeverities.Positive)),
            items);
    }

    private static AnalyticsInsightDto ToDto(
        AnalyticsInsightFinding finding,
        bool financialHealthIsGlobal,
        string currency)
    {
        var copy = CopyFor(finding, financialHealthIsGlobal, currency);
        var evidence = finding.Evidence
            .Select(item => new AnalyticsInsightEvidenceDto(
                item.Key,
                EvidenceLabel(item.Key),
                item.Value,
                item.Unit,
                EvidenceSource(finding.Category)))
            .ToList();
        return new AnalyticsInsightDto(
            finding.Code,
            finding.Category,
            finding.Severity,
            finding.Confidence,
            finding.Priority,
            copy.Title,
            copy.Observation,
            copy.Interpretation,
            copy.WhyItMatters,
            evidence,
            copy.Limitations,
            copy.Action);
    }

    private static InsightCopy CopyFor(
        AnalyticsInsightFinding finding,
        bool financialHealthIsGlobal,
        string currency)
    {
        var evidence = finding.Evidence.ToDictionary(item => item.Key, item => item.Value);
        return finding.Code switch
        {
            "DATA_UNAVAILABLE" => new(
                "Chưa đủ dữ liệu để kết luận",
                $"Phạm vi đang thiếu {Value(evidence, "missingSnapshotDays"):0} ngày snapshot.",
                "Các chỉ số hiệu suất có thể chưa đại diện cho toàn bộ kỳ đã chọn.",
                "Bổ sung dữ liệu trước sẽ giảm nguy cơ diễn giải sai lợi suất và rủi ro.",
                ["Chất lượng dữ liệu phản ánh snapshot đã ghi nhận, không xác nhận độ chính xác của mọi giao dịch nguồn."],
                new AnalyticsInsightActionDto("Kiểm tra dữ liệu hiệu suất", "/analytics/performance")),
            "DATA_QUALITY" => new(
                "Dữ liệu cần được kiểm tra",
                $"Có {Value(evidence, "missingSnapshotDays"):0} ngày thiếu snapshot, {Value(evidence, "staleAssetCount"):0} tài sản dùng giá cũ và {Value(evidence, "unclassifiedCashFlowCount"):0} dòng tiền chưa phân loại.",
                "Một phần kết quả vẫn có thể tham khảo, nhưng độ tin cậy của chỉ số phụ thuộc vào các khoảng trống này.",
                "Giá cũ và dòng tiền chưa phân loại có thể làm thay đổi NAV, TWR hoặc XIRR.",
                ["Engine không tự nội suy ngày thiếu và không thay thế giá thị trường."],
                new AnalyticsInsightActionDto("Mở kiểm tra chất lượng", "/analytics/performance")),
            "BUDGET_EXCEEDED" => new(
                "Có ngân sách đã vượt giới hạn",
                $"{Value(evidence, "budgetExceededCount"):0} ngân sách đang vượt mức thiết lập.",
                "Áp lực chi tiêu hiện tại có thể cạnh tranh với tiền dành cho mục tiêu và DCA.",
                "Kiểm tra các khoản chi lớn giúp xác định đây là biến động một lần hay xu hướng lặp lại.",
                financialHealthIsGlobal
                    ? ["Ngân sách là ngữ cảnh tổng thể của mọi danh mục, không chỉ danh mục đang chọn."]
                    : ["Ngưỡng ngân sách do người dùng thiết lập và không tự điều chỉnh theo thu nhập."],
                new AnalyticsInsightActionDto("Rà soát ngân sách", "/budgets")),
            "DRAWDOWN" => new(
                "Rà soát mức sụt giảm trong kỳ",
                $"Drawdown lớn nhất là {Value(evidence, "maximumDrawdownPercentage"):0.##}%.",
                "Chỉ số đo mức giảm từ một đỉnh trước đó trong chính phạm vi đang xem.",
                "Drawdown giúp đánh giá mức biến động đã trải qua, bổ sung cho con số lợi suất cuối kỳ.",
                ["Không phải dự báo mức lỗ tương lai và không đo thanh khoản khi cần bán tài sản."],
                new AnalyticsInsightActionDto("Xem đường drawdown", "/analytics/performance")),
            "ALLOCATION_DRIFT" => new(
                $"Phân bổ {finding.Subject ?? "tài sản"} lệch khỏi mục tiêu",
                $"Tỷ trọng hiện tại {Value(evidence, "currentPercentage"):0.##}% so với mục tiêu {Value(evidence, "targetPercentage"):0.##}%, lệch {Value(evidence, "driftPercentagePoints"):+0.##;-0.##;0} điểm phần trăm.",
                "Độ lệch vượt biên dung sai đã cấu hình nên cần được xem xét, chưa đồng nghĩa phải giao dịch.",
                "Dòng tiền mới có thể là cách điều chỉnh ít gây phí và thuế hơn so với bán tài sản.",
                ["Mục tiêu áp dụng ở cấp người dùng; giá cũ có thể thay đổi tỷ trọng hiện tại."],
                new AnalyticsInsightActionDto("Mở phân bổ", "/analytics?tab=allocation")),
            "CASHFLOW_PRESSURE" => new(
                "Dòng tiền gần đây đang chịu áp lực",
                $"{Value(evidence, "negativeMonthCount"):0} trong ba tháng gần nhất có dòng tiền ròng âm; tổng ròng là {Value(evidence, "recentNetFlow"):N0} {currency}.",
                "Nhiều tháng âm liên tiếp có thể làm giảm khoảng đệm cho mục tiêu và lịch đầu tư định kỳ.",
                "Phân biệt chi phí một lần với chi phí lặp lại giúp chọn phản ứng phù hợp hơn.",
                ["Chỉ dùng bản ghi thu/chi trong ứng dụng và không bao gồm tài khoản ngoài hệ thống."],
                new AnalyticsInsightActionDto("Xem dòng tiền", "/analytics?tab=cashflow")),
            "GOALS_AT_RISK" => new(
                "Mục tiêu sắp đến hạn cần chú ý",
                $"{Value(evidence, "goalAtRiskCount"):0} mục tiêu còn dưới 80% tiến độ và không quá 30 ngày.",
                "Khoảng thời gian còn lại ngắn hơn trong khi phần cần tích lũy vẫn đáng kể.",
                "Rà soát số tiền, hạn mục tiêu hoặc nhịp đóng góp giúp kiểm tra tính khả thi.",
                ["Tiến độ dựa trên số dư và dòng tiền đã liên kết trong CorePortfolio."],
                new AnalyticsInsightActionDto("Mở mục tiêu tiết kiệm", "/saving-goals")),
            "DCA_CASH" => new(
                "Một số kế hoạch DCA chưa đủ tiền mặt",
                $"{Value(evidence, "dcaInsufficientCashCount"):0} kế hoạch đang hoạt động có số dư thấp hơn số tiền dự kiến.",
                "Lịch vẫn tồn tại nhưng khả năng thực hiện phụ thuộc số dư tiền mặt liên kết.",
                "Kiểm tra sớm tránh nhầm một kế hoạch đã cấu hình với một giao dịch chắc chắn xảy ra.",
                ["CorePortfolio không tự nạp tiền hoặc tự đặt lệnh từ tín hiệu này."],
                new AnalyticsInsightActionDto("Mở lịch DCA", "/dca-plans")),
            "RETURN_GAP" => new(
                "TWR và XIRR đang kể hai câu chuyện khác nhau",
                $"TWR là {Value(evidence, "timeWeightedReturnPercentage"):0.##}% và XIRR là {Value(evidence, "moneyWeightedReturnPercentage"):0.##}%, chênh {Value(evidence, "returnGapPercentagePoints"):+0.##;-0.##;0} điểm phần trăm.",
                "Thời điểm và quy mô nạp/rút tiền có ảnh hưởng đáng kể đến lợi suất thực tế của bạn.",
                "Đọc cả hai chỉ số giúp tách hiệu quả danh mục khỏi trải nghiệm lợi suất theo dòng tiền cá nhân.",
                ["XIRR cần đủ dòng tiền có ngày thực tế; dữ liệu thiếu làm giảm độ tin cậy."],
                new AnalyticsInsightActionDto("Xem phương pháp tính", "/analytics/performance")),
            _ => new(
                "Chưa có tín hiệu cần xử lý ngay",
                "Không quy tắc ưu tiên nào được kích hoạt trong phạm vi hiện tại.",
                "Các chỉ số đang nằm trong ngưỡng rà soát của methodology rules-v1.",
                "Tiếp tục cập nhật snapshot giúp duy trì chất lượng theo dõi.",
                ["Không có cảnh báo không đồng nghĩa không có rủi ro đầu tư."],
                new AnalyticsInsightActionDto("Xem hiệu suất chi tiết", "/analytics/performance"))
        };
    }

    private static decimal Value(
        IReadOnlyDictionary<string, decimal> evidence,
        string key) =>
        evidence.GetValueOrDefault(key);

    private static string EvidenceLabel(string key) =>
        key switch
        {
            "missingSnapshotDays" => "Ngày thiếu snapshot",
            "staleAssetCount" => "Tài sản có giá cũ",
            "unclassifiedCashFlowCount" => "Dòng tiền chưa phân loại",
            "budgetExceededCount" => "Ngân sách vượt mức",
            "maximumDrawdownPercentage" => "Drawdown lớn nhất",
            "currentPercentage" => "Tỷ trọng hiện tại",
            "targetPercentage" => "Tỷ trọng mục tiêu",
            "driftPercentagePoints" => "Độ lệch",
            "negativeMonthCount" => "Tháng dòng tiền âm",
            "recentNetFlow" => "Dòng tiền ròng gần đây",
            "goalAtRiskCount" => "Mục tiêu cần chú ý",
            "dcaInsufficientCashCount" => "Kế hoạch thiếu tiền",
            "timeWeightedReturnPercentage" => "TWR",
            "moneyWeightedReturnPercentage" => "XIRR",
            "returnGapPercentagePoints" => "Chênh lệch TWR–XIRR",
            _ => key
        };

    private static string EvidenceSource(string category) =>
        category switch
        {
            AnalyticsInsightCategories.DataQuality => "Performance snapshots",
            AnalyticsInsightCategories.Risk => "Performance summary",
            AnalyticsInsightCategories.Allocation => "Current allocation & target plan",
            AnalyticsInsightCategories.Cashflow => "Cashflow & financial health",
            AnalyticsInsightCategories.Goals => "Saving goals & DCA plans",
            AnalyticsInsightCategories.Performance => "TWR & XIRR metrics",
            _ => "Analytics overview"
        };

    private sealed record InsightCopy(
        string Title,
        string Observation,
        string Interpretation,
        string WhyItMatters,
        IReadOnlyList<string> Limitations,
        AnalyticsInsightActionDto? Action);
}
