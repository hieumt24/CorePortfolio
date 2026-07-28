import { useState } from 'react';
import './Auth.css';

interface RecoveryCodesPanelProps {
  codes: string[];
  onContinue: () => void;
  continueLabel?: string;
}

export const RecoveryCodesPanel = ({
  codes,
  onContinue,
  continueLabel = 'Tiếp tục',
}: RecoveryCodesPanelProps) => {
  const [confirmed, setConfirmed] = useState(false);
  const [copyStatus, setCopyStatus] = useState('');

  const copyCodes = async () => {
    try {
      await navigator.clipboard.writeText(codes.join('\n'));
      setCopyStatus('Đã sao chép.');
    } catch {
      setCopyStatus('Không thể sao chép tự động.');
    }
  };

  const downloadCodes = () => {
    const blob = new Blob(
      [`CorePortfolio recovery codes\n\n${codes.join('\n')}\n`],
      { type: 'text/plain;charset=utf-8' },
    );
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'coreportfolio-recovery-codes.txt';
    anchor.click();
    URL.revokeObjectURL(url);
  };

  return (
    <section className="recovery-panel" aria-labelledby="recovery-title">
      <div className="auth-header">
        <span className="auth-step">Bước cuối</span>
        <h2 id="recovery-title">Lưu mã khôi phục</h2>
        <p>Mỗi mã chỉ dùng được một lần. Mã sẽ không được hiển thị lại.</p>
      </div>
      <div className="recovery-code-grid" aria-label="Danh sách mã khôi phục">
        {codes.map(code => <code key={code}>{code}</code>)}
      </div>
      <div className="recovery-actions">
        <button type="button" className="btn-outline" onClick={() => void copyCodes()}>
          Sao chép
        </button>
        <button type="button" className="btn-outline" onClick={downloadCodes}>
          Tải xuống
        </button>
      </div>
      <span className="auth-inline-status" role="status">{copyStatus}</span>
      <label className="auth-confirmation">
        <input
          type="checkbox"
          checked={confirmed}
          onChange={event => setConfirmed(event.target.checked)}
        />
        Tôi đã lưu các mã ở nơi an toàn.
      </label>
      <button
        type="button"
        className="btn-primary auth-submit"
        disabled={!confirmed}
        onClick={onContinue}
      >
        {continueLabel}
      </button>
    </section>
  );
};
