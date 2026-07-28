import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { QRCodeSVG } from 'qrcode.react';
import { useAuth } from '../../../context/AuthContext';
import { authApi } from '../api/authApi';
import type {
  LoginFlowStage,
  TwoFactorSetupResponse,
} from '../types/twoFactor';
import { RecoveryCodesPanel } from './RecoveryCodesPanel';
import './Auth.css';

export const LoginPage = () => {
  const [stage, setStage] = useState<LoginFlowStage>('credentials');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [challengeToken, setChallengeToken] = useState('');
  const [challengeExpiresAt, setChallengeExpiresAt] = useState('');
  const [setup, setSetup] = useState<TwoFactorSetupResponse | null>(null);
  const [code, setCode] = useState('');
  const [recoveryCode, setRecoveryCode] = useState('');
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [pendingToken, setPendingToken] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

  const completeLogin = (token: string) => {
    login(token);
    navigate('/portfolios', { replace: true });
  };

  const resetFlow = () => {
    setStage('credentials');
    setPassword('');
    setChallengeToken('');
    setChallengeExpiresAt('');
    setSetup(null);
    setCode('');
    setRecoveryCode('');
    setUseRecoveryCode(false);
    setRecoveryCodes([]);
    setPendingToken('');
    setError('');
  };

  const handleCredentials = async (event: React.FormEvent) => {
    event.preventDefault();
    setLoading(true);
    setError('');
    try {
      const response = await authApi.login(username.trim(), password);
      if (response.status === 'Authenticated' && response.token) {
        completeLogin(response.token);
        return;
      }
      if (!response.challengeToken) throw new Error('Challenge 2FA không hợp lệ.');
      setPassword('');
      setChallengeToken(response.challengeToken);
      setChallengeExpiresAt(response.challengeExpiresAt ?? '');
      if (response.status === 'TwoFactorSetupRequired') {
        const setupResponse = await authApi.beginTwoFactorSetup(response.challengeToken);
        setSetup(setupResponse);
        setStage('setup');
      } else {
        setStage('verify');
      }
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể đăng nhập.');
    } finally {
      setLoading(false);
    }
  };

  const handleVerification = async (event: React.FormEvent) => {
    event.preventDefault();
    setLoading(true);
    setError('');
    try {
      const response = await authApi.verifyTwoFactor(
        challengeToken,
        useRecoveryCode
          ? { recoveryCode: recoveryCode.trim() }
          : { code: code.replace(/\D/g, '') },
      );
      if (!response.token) throw new Error('Không nhận được phiên đăng nhập.');
      setSetup(null);
      setChallengeToken('');
      setCode('');
      setRecoveryCode('');
      if (response.recoveryCodes?.length) {
        setPendingToken(response.token);
        setRecoveryCodes(response.recoveryCodes);
        setStage('recovery');
      } else {
        completeLogin(response.token);
      }
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : 'Mã xác minh không hợp lệ hoặc đã hết hạn.',
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card glass-panel">
        {stage === 'credentials' && (
          <>
            <div className="auth-header">
              <h2>Đăng nhập CorePortfolio</h2>
              <p>Truy cập danh mục và dữ liệu tài chính của bạn.</p>
            </div>
            <form className="auth-form" onSubmit={handleCredentials}>
              <div className="form-group">
                <label htmlFor="username">Username</label>
                <input
                  id="username"
                  value={username}
                  onChange={event => setUsername(event.target.value)}
                  autoComplete="username"
                  required
                  autoFocus
                />
              </div>
              <div className="form-group">
                <label htmlFor="password">Mật khẩu</label>
                <input
                  id="password"
                  type="password"
                  value={password}
                  onChange={event => setPassword(event.target.value)}
                  autoComplete="current-password"
                  required
                />
              </div>
              {error && <p className="auth-error" role="alert">{error}</p>}
              <button type="submit" className="btn-primary auth-submit" disabled={loading}>
                {loading ? 'Đang xác minh…' : 'Đăng nhập'}
              </button>
            </form>
            <div className="auth-footer">
              <p>Chưa có tài khoản? <Link to="/register">Đăng ký</Link></p>
            </div>
          </>
        )}

        {stage === 'setup' && setup && (
          <>
            <div className="auth-header">
              <span className="auth-step">Thiết lập bắt buộc</span>
              <h2>Bảo vệ tài khoản quản trị</h2>
              <p>Quét QR bằng ứng dụng authenticator, sau đó nhập mã 6 chữ số.</p>
            </div>
            <div className="auth-qr">
              <QRCodeSVG
                value={setup.provisioningUri}
                size={196}
                level="M"
                marginSize={2}
                title="QR thiết lập CorePortfolio TOTP"
              />
            </div>
            <div className="auth-manual-key">
              <span>Không quét được QR?</span>
              <code>{setup.manualKey}</code>
              <span>Challenge hết hạn lúc {new Date(setup.expiresAt).toLocaleTimeString('vi-VN')}.</span>
            </div>
            <form className="auth-form" onSubmit={handleVerification}>
              {challengeExpiresAt && (
                <p className="auth-expiry">
                  Challenge hết hạn lúc {new Date(challengeExpiresAt).toLocaleTimeString('vi-VN')}.
                </p>
              )}
              <div className="form-group">
                <label htmlFor="setup-code">Mã xác minh</label>
                <input
                  id="setup-code"
                  className="auth-code-input"
                  value={code}
                  onChange={event => setCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  pattern="[0-9]{6}"
                  maxLength={6}
                  required
                  autoFocus
                />
              </div>
              {error && <p className="auth-error" role="alert">{error}</p>}
              <button type="submit" className="btn-primary auth-submit" disabled={loading || code.length !== 6}>
                {loading ? 'Đang kích hoạt…' : 'Kích hoạt và đăng nhập'}
              </button>
              <button type="button" className="auth-text-button" onClick={resetFlow}>
                Quay lại đăng nhập
              </button>
            </form>
          </>
        )}

        {stage === 'verify' && (
          <>
            <div className="auth-header">
              <span className="auth-step">Xác minh hai bước</span>
              <h2>Xác nhận đăng nhập</h2>
              <p>
                {useRecoveryCode
                  ? 'Nhập một mã khôi phục chưa sử dụng.'
                  : 'Nhập mã hiện tại từ ứng dụng authenticator.'}
              </p>
            </div>
            <form className="auth-form" onSubmit={handleVerification}>
              {challengeExpiresAt && (
                <p className="auth-expiry">
                  Challenge hết hạn lúc {new Date(challengeExpiresAt).toLocaleTimeString('vi-VN')}.
                </p>
              )}
              <div className="form-group">
                <label htmlFor="two-factor-code">
                  {useRecoveryCode ? 'Mã khôi phục' : 'Mã 6 chữ số'}
                </label>
                <input
                  id="two-factor-code"
                  className={useRecoveryCode ? '' : 'auth-code-input'}
                  value={useRecoveryCode ? recoveryCode : code}
                  onChange={event => (
                    useRecoveryCode
                      ? setRecoveryCode(event.target.value.toUpperCase().slice(0, 19))
                      : setCode(event.target.value.replace(/\D/g, '').slice(0, 6))
                  )}
                  inputMode={useRecoveryCode ? 'text' : 'numeric'}
                  autoComplete="one-time-code"
                  required
                  autoFocus
                />
              </div>
              {error && <p className="auth-error" role="alert">{error}</p>}
              <button
                type="submit"
                className="btn-primary auth-submit"
                disabled={loading || (useRecoveryCode ? !recoveryCode.trim() : code.length !== 6)}
              >
                {loading ? 'Đang xác minh…' : 'Xác minh'}
              </button>
              <button
                type="button"
                className="auth-text-button"
                onClick={() => {
                  setUseRecoveryCode(current => !current);
                  setError('');
                }}
              >
                {useRecoveryCode ? 'Dùng ứng dụng authenticator' : 'Dùng mã khôi phục'}
              </button>
              <button type="button" className="auth-text-button" onClick={resetFlow}>
                Đăng nhập bằng tài khoản khác
              </button>
            </form>
          </>
        )}

        {stage === 'recovery' && (
          <RecoveryCodesPanel
            codes={recoveryCodes}
            continueLabel="Vào CorePortfolio"
            onContinue={() => completeLogin(pendingToken)}
          />
        )}
      </div>
    </div>
  );
};
