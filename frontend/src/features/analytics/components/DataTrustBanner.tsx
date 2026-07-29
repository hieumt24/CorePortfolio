import type { PerformanceDataQualityDto } from '../types';

interface DataTrustBannerProps {
  quality: PerformanceDataQualityDto;
}

const statusCopy: Record<string, { label: string; detail: string }> = {
  Complete: {
    label: 'Đủ tin cậy để tham khảo',
    detail: 'Snapshot trong phạm vi đang đầy đủ và không có lỗi phân loại nổi bật.',
  },
  StalePrices: {
    label: 'Có giá tài sản cần làm mới',
    detail: 'Kết quả vẫn hữu ích, nhưng nên cập nhật giá trước quyết định có độ nhạy cao.',
  },
  Partial: {
    label: 'Chỉ nên dùng như tín hiệu định hướng',
    detail: 'Một phần snapshot hoặc dòng tiền chưa hoàn chỉnh có thể làm lệch chỉ số.',
  },
  Unavailable: {
    label: 'Chưa đủ dữ liệu để kết luận',
    detail: 'Hãy tạo snapshot để bắt đầu theo dõi hiệu suất theo thời gian.',
  },
};

export const DataTrustBanner = ({ quality }: DataTrustBannerProps) => {
  const copy = statusCopy[quality.qualityStatus] ?? statusCopy.Partial;
  const asOf = quality.asOf
    ? new Intl.DateTimeFormat('vi-VN', {
        dateStyle: 'short',
        timeStyle: 'short',
        timeZone: 'Asia/Ho_Chi_Minh',
      }).format(new Date(quality.asOf))
    : 'Chưa có snapshot';

  return (
    <section
      className={`analytics-trust-banner is-${quality.qualityStatus.toLowerCase()}`}
      aria-label="Độ tin cậy dữ liệu"
    >
      <div className="analytics-trust-status" aria-hidden="true">
        <span />
      </div>
      <div>
        <span className="analytics-eyebrow">Độ tin cậy dữ liệu</span>
        <strong>{copy.label}</strong>
        <p>{copy.detail}</p>
      </div>
      <dl>
        <div>
          <dt>Thiếu snapshot</dt>
          <dd>{quality.missingSnapshotDays} ngày</dd>
        </div>
        <div>
          <dt>Giá cũ</dt>
          <dd>{quality.staleAssetCount} tài sản</dd>
        </div>
        <div>
          <dt>Cập nhật</dt>
          <dd>{asOf}</dd>
        </div>
      </dl>
    </section>
  );
};
