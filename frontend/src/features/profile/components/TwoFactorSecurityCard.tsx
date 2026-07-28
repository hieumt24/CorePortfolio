import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { QRCodeSVG } from 'qrcode.react';
import { useAuth } from '../../../context/AuthContext';
import { authApi } from '../../auth/api/authApi';
import { RecoveryCodesPanel } from '../../auth/components/RecoveryCodesPanel';
import { profileApi } from '../api/profileApi';
import type { TwoFactorSetup, TwoFactorStatus } from '../types';

export const TwoFactorSecurityCard = () => {
  const [status, setStatus] = useState<TwoFactorStatus | null>(null);
  const [setup, setSetup] = useState<TwoFactorSetup | null>(null);
  const [currentPassword, setCurrentPassword] = useState('');
  const [code, setCode] = useState('');
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [recoveryCompletionMessage, setRecoveryCompletionMessage] = useState('');
  const [busy, setBusy] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const { login, logout } = useAuth();
  const navigate = useNavigate();

  const loadStatus = useCallback(async () => {
    setError('');
    try {
      setStatus(await profileApi.getTwoFactorStatus());
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tải trạng thái 2FA.');
    }
  }, []);

  useEffect(() => {
    let active = true;
    void profileApi.getTwoFactorStatus()
      .then(result => {
        if (active) setStatus(result);
      })
      .catch(reason => {
        if (active) {
          setError(reason instanceof Error ? reason.message : 'Không thể tải trạng thái 2FA.');
        }
      });
    return () => {
      active = false;
    };
  }, []);

  const beginSetup = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy('setup');
    setError('');
    try {
      setSetup(await profileApi.beginTwoFactorSetup(currentPassword));
      setCode('');
      setMessage('');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể bắt đầu thiết lập 2FA.');
    } finally {
      setBusy('');
    }
  };

  const completeSetup = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!setup) return;
    setBusy('verify');
    setError('');
    try {
      const response = await authApi.verifyTwoFactor(
        setup.challengeToken,
        { code },
      );
      if (!response.token || !response.recoveryCodes?.length) {
        throw new Error('Không nhận được mã khôi phục.');
      }
      login(response.token);
      setRecoveryCodes(response.recoveryCodes);
      setRecoveryCompletionMessage('2FA đã được kích hoạt.');
      setSetup(null);
      setCurrentPassword('');
      setCode('');
      await loadStatus();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Mã xác minh không hợp lệ.');
    } finally {
      setBusy('');
    }
  };

  const regenerateCodes = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy('regenerate');
    setError('');
    try {
      const result = await profileApi.regenerateRecoveryCodes(currentPassword, code);
      setRecoveryCodes(result.recoveryCodes);
      setRecoveryCompletionMessage('Mã khôi phục đã được thay thế.');
      setCurrentPassword('');
      setCode('');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tạo lại mã khôi phục.');
    } finally {
      setBusy('');
    }
  };

  const disableTwoFactor = async () => {
    if (!window.confirm('Tắt xác minh hai bước và thu hồi tất cả phiên đăng nhập?')) return;
    setBusy('disable');
    setError('');
    try {
      await profileApi.disableTwoFactor(currentPassword, code);
      await logout();
      navigate('/login', { replace: true });
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tắt 2FA.');
      setBusy('');
    }
  };

  const finishRecoveryCodes = async () => {
    setRecoveryCodes([]);
    setMessage(`${recoveryCompletionMessage} Hãy giữ mã khôi phục ở nơi an toàn.`);
  };

  if (recoveryCodes.length > 0) {
    return (
      <section className="profile-section profile-section--security">
        <RecoveryCodesPanel
          codes={recoveryCodes}
          continueLabel="Hoàn tất"
          onContinue={() => void finishRecoveryCodes()}
        />
      </section>
    );
  }

  return (
    <section className="profile-section profile-section--security">
      <div className="profile-section-heading security-heading-row">
        <div>
          <h2>Xác minh hai bước</h2>
          <p>Bảo vệ tài khoản bằng ứng dụng authenticator và mã khôi phục.</p>
        </div>
        {status && (
          <span className={`profile-security-badge ${status.isEnabled ? 'enabled' : 'disabled'}`}>
            {status.isEnabled ? 'Đã bật' : status.isRequired ? 'Bắt buộc' : 'Chưa bật'}
          </span>
        )}
      </div>

      {!status && !error && <p className="profile-security-note">Đang tải trạng thái bảo mật…</p>}
      {status && !status.isAvailable && (
        <div className="profile-security-unavailable" role="status">
          <strong>2FA chưa sẵn sàng trên máy chủ.</strong>
          <span>Vui lòng liên hệ quản trị hệ thống để hoàn tất cấu hình bảo mật.</span>
        </div>
      )}
      {status?.isEnabled && (
        <div className="profile-security-summary">
          <div>
            <span>Mã khôi phục còn lại</span>
            <strong>{status.recoveryCodesRemaining}</strong>
          </div>
          <p>
            {status.isRequired
              ? '2FA là bắt buộc với vai trò hiện tại và không thể tự tắt.'
              : 'Bạn có thể tắt 2FA sau khi xác minh lại mật khẩu và mã TOTP.'}
          </p>
        </div>
      )}

      {status?.isAvailable && !status.isEnabled && !setup && (
        <form onSubmit={beginSetup}>
          <div className="profile-field">
            <label htmlFor="two-factor-password">Mật khẩu hiện tại</label>
            <input
              id="two-factor-password"
              type="password"
              value={currentPassword}
              onChange={event => setCurrentPassword(event.target.value)}
              autoComplete="current-password"
              required
            />
            <span className="field-message">
              Xác minh danh tính trước khi tạo authenticator secret.
            </span>
          </div>
          <div className="profile-form-footer">
            <span className="profile-status" />
            <button className="btn btn-primary" disabled={busy !== ''}>
              {busy === 'setup' ? 'Đang chuẩn bị…' : 'Thiết lập 2FA'}
            </button>
          </div>
        </form>
      )}

      {setup && (
        <form onSubmit={completeSetup}>
          <div className="profile-2fa-setup">
            <div className="profile-2fa-qr">
              <QRCodeSVG
                value={setup.provisioningUri}
                size={180}
                level="M"
                marginSize={2}
                title="QR thiết lập CorePortfolio TOTP"
              />
            </div>
            <div>
              <h3>Quét QR bằng ứng dụng authenticator</h3>
              <p>Nếu không quét được, nhập khóa thủ công:</p>
              <code>{setup.manualKey}</code>
            </div>
          </div>
          <div className="profile-field">
            <label htmlFor="two-factor-setup-code">Mã xác minh 6 chữ số</label>
            <input
              id="two-factor-setup-code"
              className="profile-otp-input"
              value={code}
              onChange={event => setCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
              inputMode="numeric"
              autoComplete="one-time-code"
              pattern="[0-9]{6}"
              maxLength={6}
              required
            />
          </div>
          <div className="profile-form-footer">
            <button className="btn btn-outline" type="button" onClick={() => setSetup(null)}>
              Hủy
            </button>
            <button className="btn btn-primary" disabled={busy !== '' || code.length !== 6}>
              {busy === 'verify' ? 'Đang kích hoạt…' : 'Kích hoạt'}
            </button>
          </div>
        </form>
      )}

      {status?.isEnabled && (
        <form onSubmit={regenerateCodes}>
          <div className="profile-password-grid">
            <div className="profile-field">
              <label htmlFor="two-factor-current-password">Mật khẩu hiện tại</label>
              <input
                id="two-factor-current-password"
                type="password"
                value={currentPassword}
                onChange={event => setCurrentPassword(event.target.value)}
                autoComplete="current-password"
                required
              />
            </div>
            <div className="profile-field">
              <label htmlFor="two-factor-current-code">Mã authenticator</label>
              <input
                id="two-factor-current-code"
                className="profile-otp-input"
                value={code}
                onChange={event => setCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
                inputMode="numeric"
                autoComplete="one-time-code"
                pattern="[0-9]{6}"
                maxLength={6}
                required
              />
            </div>
          </div>
          <div className="profile-security-actions">
            <button className="btn btn-outline" disabled={busy !== '' || code.length !== 6}>
              {busy === 'regenerate' ? 'Đang tạo…' : 'Tạo lại mã khôi phục'}
            </button>
            {!status.isRequired && (
              <button
                className="btn profile-danger-button"
                type="button"
                disabled={busy !== '' || !currentPassword || code.length !== 6}
                onClick={() => void disableTwoFactor()}
              >
                {busy === 'disable' ? 'Đang tắt…' : 'Tắt 2FA'}
              </button>
            )}
          </div>
        </form>
      )}

      {message && <p className="profile-status profile-status--success" role="status">{message}</p>}
      {error && <p className="profile-status profile-status--error" role="alert">{error}</p>}
    </section>
  );
};
