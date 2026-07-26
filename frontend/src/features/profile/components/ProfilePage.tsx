import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '../../../context/AuthContext';
import { profileApi } from '../api/profileApi';
import type { UserProfile } from '../types';
import { formatVietnamDateTime } from '../../../shared/utils/dateTime';
import './ProfilePage.css';

type FieldErrors = Partial<Record<'displayName' | 'username' | 'email', string>>;
type PasswordErrors = Partial<Record<'currentPassword' | 'newPassword' | 'confirmPassword', string>>;

const formatDate = (value: string | null) =>
  formatVietnamDateTime(value, 'Chưa có dữ liệu');

const getInitials = (name: string) =>
  name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map(part => part[0]?.toUpperCase())
    .join('') || 'CP';

export const ProfilePage = () => {
  const { refreshUser } = useAuth();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [profileSaving, setProfileSaving] = useState(false);
  const [passwordSaving, setPasswordSaving] = useState(false);
  const [profileStatus, setProfileStatus] = useState('');
  const [passwordStatus, setPasswordStatus] = useState('');
  const [profileStatusTone, setProfileStatusTone] = useState<'success' | 'error' | ''>('');
  const [passwordStatusTone, setPasswordStatusTone] = useState<'success' | 'error' | ''>('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [passwordErrors, setPasswordErrors] = useState<PasswordErrors>({});
  const [form, setForm] = useState({ displayName: '', username: '', email: '' });
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const loadProfile = async () => {
    setLoading(true);
    setLoadError('');
    try {
      const result = await profileApi.get();
      setProfile(result);
      setForm({
        displayName: result.displayName,
        username: result.username,
        email: result.email ?? '',
      });
    } catch (error) {
      setLoadError(error instanceof Error ? error.message : 'Không thể tải hồ sơ.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let isCurrent = true;
    profileApi.get()
      .then(result => {
        if (!isCurrent) return;
        setProfile(result);
        setForm({
          displayName: result.displayName,
          username: result.username,
          email: result.email ?? '',
        });
      })
      .catch(error => {
        if (isCurrent) {
          setLoadError(error instanceof Error ? error.message : 'Không thể tải hồ sơ.');
        }
      })
      .finally(() => {
        if (isCurrent) setLoading(false);
      });

    return () => {
      isCurrent = false;
    };
  }, []);

  const initials = useMemo(
    () => getInitials(profile?.displayName || profile?.username || 'Core Portfolio'),
    [profile],
  );

  const validateProfile = () => {
    const errors: FieldErrors = {};
    if (form.displayName.trim().length < 2) {
      errors.displayName = 'Tên hiển thị cần ít nhất 2 ký tự.';
    }
    if (form.username.trim().length < 3) {
      errors.username = 'Username cần ít nhất 3 ký tự.';
    }
    if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
      errors.email = 'Email chưa đúng định dạng.';
    }
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleProfileSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setProfileStatus('');
    setProfileStatusTone('');
    if (!validateProfile()) return;
    setProfileSaving(true);
    try {
      const updated = await profileApi.update({
        displayName: form.displayName.trim(),
        username: form.username.trim(),
        email: form.email.trim() || null,
      });
      setProfile(updated);
      setProfileStatus('Thông tin cá nhân đã được cập nhật.');
      setProfileStatusTone('success');
      await refreshUser();
    } catch (error) {
      setProfileStatus(error instanceof Error ? error.message : 'Không thể cập nhật hồ sơ.');
      setProfileStatusTone('error');
    } finally {
      setProfileSaving(false);
    }
  };

  const validatePasswords = () => {
    const errors: PasswordErrors = {};
    if (!passwordForm.currentPassword) {
      errors.currentPassword = 'Nhập mật khẩu hiện tại.';
    }
    if (passwordForm.newPassword.length < 8) {
      errors.newPassword = 'Mật khẩu mới cần ít nhất 8 ký tự.';
    }
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      errors.confirmPassword = 'Mật khẩu xác nhận chưa khớp.';
    }
    setPasswordErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handlePasswordSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setPasswordStatus('');
    setPasswordStatusTone('');
    if (!validatePasswords()) return;
    setPasswordSaving(true);
    try {
      await profileApi.changePassword(passwordForm);
      setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
      setPasswordErrors({});
      setPasswordStatus('Mật khẩu đã được đổi. Bạn có thể tiếp tục sử dụng phiên hiện tại.');
      setPasswordStatusTone('success');
    } catch (error) {
      setPasswordStatus(error instanceof Error ? error.message : 'Không thể đổi mật khẩu.');
      setPasswordStatusTone('error');
    } finally {
      setPasswordSaving(false);
    }
  };

  if (loading) {
    return (
      <main className="profile-page" aria-busy="true">
        <div className="profile-loading">
          <span className="profile-spinner" aria-hidden="true" />
          <span>Đang tải hồ sơ…</span>
        </div>
      </main>
    );
  }

  if (loadError || !profile) {
    return (
      <main className="profile-page">
        <section className="profile-load-error" role="alert">
          <h1>Không thể mở hồ sơ</h1>
          <p>{loadError || 'Hồ sơ không tồn tại.'}</p>
          <button className="btn btn-primary" type="button" onClick={() => void loadProfile()}>
            Thử lại
          </button>
        </section>
      </main>
    );
  }

  return (
    <main className="profile-page">
      <header className="profile-heading">
        <div>
          <p className="profile-kicker">Tài khoản cá nhân</p>
          <h1>Hồ sơ và bảo mật</h1>
        </div>
        <p>Quản lý cách tên của bạn xuất hiện trong CorePortfolio và giữ tài khoản an toàn.</p>
      </header>

      <div className="profile-layout">
        <aside className="profile-identity">
          <div className="profile-avatar profile-avatar--large" aria-hidden="true">{initials}</div>
          <div>
            <h2>{profile.displayName}</h2>
            <p>@{profile.username}</p>
          </div>
          <dl className="profile-meta">
            <div>
              <dt>Quyền truy cập</dt>
              <dd>{profile.role === 'Admin' ? 'Quản trị viên' : 'Người dùng'}</dd>
            </div>
            <div>
              <dt>Tham gia</dt>
              <dd>{formatDate(profile.createdAt)}</dd>
            </div>
            <div>
              <dt>Đăng nhập gần nhất</dt>
              <dd>{formatDate(profile.lastLoginAt)}</dd>
            </div>
          </dl>
        </aside>

        <div className="profile-workspace">
          <section className="profile-section">
            <div className="profile-section-heading">
              <h2>Thông tin cá nhân</h2>
              <p>Cập nhật tên hiển thị, username và email liên hệ.</p>
            </div>
            <form onSubmit={handleProfileSubmit} noValidate>
              <div className="profile-field">
                <label htmlFor="profile-display-name">Tên hiển thị</label>
                <input
                  id="profile-display-name"
                  value={form.displayName}
                  onChange={event => setForm(current => ({ ...current, displayName: event.target.value }))}
                  onBlur={validateProfile}
                  aria-invalid={Boolean(fieldErrors.displayName)}
                  aria-describedby="profile-display-name-help"
                  maxLength={80}
                  autoComplete="name"
                />
                <span id="profile-display-name-help" className={fieldErrors.displayName ? 'field-message field-message--error' : 'field-message'}>
                  {fieldErrors.displayName || 'Tên này được hiển thị cạnh avatar của bạn.'}
                </span>
              </div>

              <div className="profile-field">
                <label htmlFor="profile-username">Username</label>
                <input
                  id="profile-username"
                  value={form.username}
                  onChange={event => setForm(current => ({ ...current, username: event.target.value }))}
                  onBlur={validateProfile}
                  aria-invalid={Boolean(fieldErrors.username)}
                  aria-describedby="profile-username-help"
                  maxLength={50}
                  autoComplete="username"
                />
                <span id="profile-username-help" className={fieldErrors.username ? 'field-message field-message--error' : 'field-message'}>
                  {fieldErrors.username || 'Dùng để đăng nhập vào tài khoản.'}
                </span>
              </div>

              <div className="profile-field">
                <label htmlFor="profile-email">Địa chỉ email</label>
                <input
                  id="profile-email"
                  type="email"
                  value={form.email}
                  onChange={event => setForm(current => ({ ...current, email: event.target.value }))}
                  onBlur={validateProfile}
                  aria-invalid={Boolean(fieldErrors.email)}
                  aria-describedby="profile-email-help"
                  maxLength={160}
                  autoComplete="email"
                  placeholder="name@example.com"
                />
                <span id="profile-email-help" className={fieldErrors.email ? 'field-message field-message--error' : 'field-message'}>
                  {fieldErrors.email || 'Email có thể để trống và không hiển thị công khai.'}
                </span>
              </div>

              <div className="profile-form-footer">
                <span className={`profile-status${profileStatusTone ? ` profile-status--${profileStatusTone}` : ''}`} role="status">
                  {profileStatus}
                </span>
                <button className="btn btn-primary" type="submit" disabled={profileSaving}>
                  {profileSaving ? 'Đang lưu…' : 'Lưu thay đổi'}
                </button>
              </div>
            </form>
          </section>

          <section className="profile-section profile-section--security">
            <div className="profile-section-heading">
              <h2>Đổi mật khẩu</h2>
              <p>Mật khẩu mới cần có ít nhất 8 ký tự và khác mật khẩu hiện tại.</p>
            </div>
            <form onSubmit={handlePasswordSubmit} noValidate>
              <div className="profile-field">
                <label htmlFor="current-password">Mật khẩu hiện tại</label>
                <input
                  id="current-password"
                  type="password"
                  value={passwordForm.currentPassword}
                  onChange={event => setPasswordForm(current => ({ ...current, currentPassword: event.target.value }))}
                  aria-invalid={Boolean(passwordErrors.currentPassword)}
                  aria-describedby="current-password-help"
                  autoComplete="current-password"
                />
                <span id="current-password-help" className={passwordErrors.currentPassword ? 'field-message field-message--error' : 'field-message'}>
                  {passwordErrors.currentPassword || 'Xác minh trước khi thay đổi thông tin bảo mật.'}
                </span>
              </div>

              <div className="profile-password-grid">
                <div className="profile-field">
                  <label htmlFor="new-password">Mật khẩu mới</label>
                  <input
                    id="new-password"
                    type="password"
                    value={passwordForm.newPassword}
                    onChange={event => setPasswordForm(current => ({ ...current, newPassword: event.target.value }))}
                    aria-invalid={Boolean(passwordErrors.newPassword)}
                    aria-describedby="new-password-help"
                    autoComplete="new-password"
                    minLength={8}
                    maxLength={72}
                  />
                  <span id="new-password-help" className={passwordErrors.newPassword ? 'field-message field-message--error' : 'field-message'}>
                    {passwordErrors.newPassword || 'Từ 8 đến 72 ký tự.'}
                  </span>
                </div>

                <div className="profile-field">
                  <label htmlFor="confirm-password">Xác nhận mật khẩu</label>
                  <input
                    id="confirm-password"
                    type="password"
                    value={passwordForm.confirmPassword}
                    onChange={event => setPasswordForm(current => ({ ...current, confirmPassword: event.target.value }))}
                    aria-invalid={Boolean(passwordErrors.confirmPassword)}
                    aria-describedby="confirm-password-help"
                    autoComplete="new-password"
                    minLength={8}
                    maxLength={72}
                  />
                  <span id="confirm-password-help" className={passwordErrors.confirmPassword ? 'field-message field-message--error' : 'field-message'}>
                    {passwordErrors.confirmPassword || 'Nhập lại chính xác mật khẩu mới.'}
                  </span>
                </div>
              </div>

              <div className="profile-form-footer">
                <span className={`profile-status${passwordStatusTone ? ` profile-status--${passwordStatusTone}` : ''}`} role="status">
                  {passwordStatus}
                </span>
                <button className="btn btn-outline" type="submit" disabled={passwordSaving}>
                  {passwordSaving ? 'Đang cập nhật…' : 'Đổi mật khẩu'}
                </button>
              </div>
            </form>
          </section>
        </div>
      </div>
    </main>
  );
};
