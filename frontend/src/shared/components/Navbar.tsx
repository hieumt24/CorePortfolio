import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { settingsApi } from '../../features/admin/api/settingsApi';
import { notificationsApi } from '../../features/notifications/api/notificationsApi';
import type { NotificationItem } from '../../features/notifications/types';
import { useAuth } from '../../context/AuthContext';
import './Navbar.css';

const navigationItems = [
  { key: 'NAV_DASHBOARD', path: '/dashboard', label: 'Tổng quan' },
  { key: 'NAV_PORTFOLIOS', path: '/portfolios', label: 'Danh mục' },
  { key: 'NAV_TRANSACTIONS', path: '/transactions', label: 'Giao dịch' },
  { key: 'NAV_REPORTS', path: '/reports', label: 'Báo cáo' },
  { key: 'NAV_CASHFLOW', path: '/cashflow', label: 'Dòng tiền' },
  { key: 'NAV_WATCHLIST', path: '/watchlist', label: 'Theo dõi' },
  { key: 'NAV_BUDGETS', path: '/budgets', label: 'Ngân sách' },
  { key: 'NAV_SAVING_GOALS', path: '/saving-goals', label: 'Mục tiêu' },
  { key: 'NAV_ANALYTICS', path: '/analytics', label: 'Phân tích' },
  { key: 'NAV_REBALANCING', path: '/rebalancing', label: 'Tái cân bằng' },
  { key: 'NAV_DCA_PLANS', path: '/dca-plans', label: 'Lịch DCA' },
];

type OpenPanel = 'more' | 'notifications' | 'profile' | null;

const BellIcon = () => (
  <svg viewBox="0 0 24 24" aria-hidden="true">
    <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" />
  </svg>
);

const ChevronIcon = () => (
  <svg viewBox="0 0 24 24" aria-hidden="true">
    <path d="m8 10 4 4 4-4" />
  </svg>
);

const UserIcon = () => (
  <svg viewBox="0 0 24 24" aria-hidden="true">
    <circle cx="12" cy="8" r="4" />
    <path d="M4 21a8 8 0 0 1 16 0" />
  </svg>
);

const ShieldIcon = () => (
  <svg viewBox="0 0 24 24" aria-hidden="true">
    <path d="M12 3 5 6v5c0 4.8 2.9 8.2 7 10 4.1-1.8 7-5.2 7-10V6z" />
    <path d="m9.5 12 1.6 1.6 3.8-4" />
  </svg>
);

const LogoutIcon = () => (
  <svg viewBox="0 0 24 24" aria-hidden="true">
    <path d="M10 17l5-5-5-5M15 12H3M14 3h5a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-5" />
  </svg>
);

const getInitials = (name: string) =>
  name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map(part => part[0]?.toUpperCase())
    .join('') || 'CP';

export const Navbar: React.FC = () => {
  const navigate = useNavigate();
  const navbarRef = useRef<HTMLElement>(null);
  const { isAuthenticated, isAdmin, user, logout } = useAuth();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [openPanel, setOpenPanel] = useState<OpenPanel>(null);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [notificationsLoading, setNotificationsLoading] = useState(false);
  const [notificationsError, setNotificationsError] = useState('');
  const [navigationVisibility, setNavigationVisibility] = useState<Record<string, boolean>>({});

  const refreshNotifications = useCallback(async () => {
    setNotificationsLoading(true);
    setNotificationsError('');
    try {
      const [page, count] = await Promise.all([
        notificationsApi.list({ unreadOnly: true, page: 1, pageSize: 5 }),
        notificationsApi.getUnreadCount(),
      ]);
      setNotifications(page.items);
      setUnreadCount(count.count);
    } catch {
      setNotificationsError('Không thể tải thông báo.');
    } finally {
      setNotificationsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!isAuthenticated) return;

    Promise.all([
      notificationsApi.list({ unreadOnly: true, page: 1, pageSize: 5 }),
      notificationsApi.getUnreadCount(),
    ])
      .then(([page, count]) => {
        setNotifications(page.items);
        setUnreadCount(count.count);
      })
      .catch(() => setNotificationsError('Không thể tải thông báo.'));
    settingsApi.getNavigationFeatures()
      .then(features => setNavigationVisibility(
        Object.fromEntries(features.map(feature => [feature.key, feature.isEnabled])),
      ))
      .catch(() => setNavigationVisibility({}));
  }, [isAuthenticated]);

  useEffect(() => {
    const handleOutsideClick = (event: MouseEvent) => {
      if (navbarRef.current && !navbarRef.current.contains(event.target as Node)) {
        setOpenPanel(null);
      }
    };
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpenPanel(null);
        setIsMobileMenuOpen(false);
      }
    };

    document.addEventListener('mousedown', handleOutsideClick);
    document.addEventListener('keydown', handleEscape);
    return () => {
      document.removeEventListener('mousedown', handleOutsideClick);
      document.removeEventListener('keydown', handleEscape);
    };
  }, []);

  const visibleNavigation = useMemo(
    () => navigationItems.filter(item => navigationVisibility[item.key] !== false),
    [navigationVisibility],
  );
  const primaryNavigation = visibleNavigation.slice(0, 5);
  const secondaryNavigation = visibleNavigation.slice(5);
  const displayName = user?.displayName || user?.username || 'Tài khoản';
  const initials = getInitials(displayName);

  const togglePanel = (panel: Exclude<OpenPanel, null>) => {
    setOpenPanel(current => current === panel ? null : panel);
  };

  const closeNavigation = () => {
    setOpenPanel(null);
    setIsMobileMenuOpen(false);
  };

  const handleLogout = () => {
    setOpenPanel(null);
    logout();
    navigate('/login');
  };

  const handleNotificationClick = async (notification: NotificationItem) => {
    try {
      await notificationsApi.markRead(notification.id);
      setNotifications(current => current.filter(item => item.id !== notification.id));
      setUnreadCount(current => Math.max(0, current - 1));
      setOpenPanel(null);
      if (notification.link) navigate(notification.link);
    } catch {
      setNotificationsError('Không thể đánh dấu thông báo. Hãy thử lại.');
    }
  };

  const handleMarkAllRead = async () => {
    try {
      await notificationsApi.markAllRead();
      setNotifications([]);
      setUnreadCount(0);
    } catch {
      setNotificationsError('Không thể đánh dấu tất cả đã đọc.');
    }
  };

  const renderNavLink = (item: typeof navigationItems[number], compact = false) => (
    <NavLink
      key={item.key}
      to={item.path}
      className={({ isActive }) =>
        `${compact ? 'navbar-dropdown-link' : 'navbar-link'}${isActive ? ' active' : ''}`
      }
      onClick={closeNavigation}
    >
      {item.label}
    </NavLink>
  );

  return (
    <>
      <header ref={navbarRef} className="app-navbar">
        <div className="navbar-shell">
          <NavLink to="/dashboard" className="navbar-brand" aria-label="CorePortfolio — Tổng quan" onClick={closeNavigation}>
            <span className="navbar-brand-mark" aria-hidden="true">CP</span>
            <span>CorePortfolio</span>
          </NavLink>

          {isAuthenticated && (
            <nav className="navbar-primary" aria-label="Điều hướng chính">
              {primaryNavigation.map(item => renderNavLink(item))}
              {secondaryNavigation.length > 0 && (
                <div className="navbar-popover-anchor">
                  <button
                    type="button"
                    className={`navbar-link navbar-more-trigger${openPanel === 'more' ? ' active' : ''}`}
                    onClick={() => togglePanel('more')}
                    aria-expanded={openPanel === 'more'}
                    aria-haspopup="menu"
                  >
                    Thêm
                    <ChevronIcon />
                  </button>
                  {openPanel === 'more' && (
                    <div className="navbar-dropdown navbar-more-menu" role="menu">
                      {secondaryNavigation.map(item => renderNavLink(item, true))}
                    </div>
                  )}
                </div>
              )}
            </nav>
          )}

          <div className="navbar-utilities">
            {isAuthenticated ? (
              <>
                <div className="navbar-popover-anchor">
                  <button
                    type="button"
                    className={`navbar-icon-button${openPanel === 'notifications' ? ' active' : ''}`}
                    onClick={() => togglePanel('notifications')}
                    aria-label={`Thông báo${unreadCount > 0 ? `, ${unreadCount} chưa đọc` : ''}`}
                    aria-expanded={openPanel === 'notifications'}
                    aria-haspopup="dialog"
                  >
                    <BellIcon />
                    {unreadCount > 0 && (
                      <span className="notification-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
                    )}
                  </button>

                  {openPanel === 'notifications' && (
                    <section className="navbar-dropdown notification-popover" aria-label="Thông báo mới">
                      <div className="popover-heading">
                        <div>
                          <strong>Thông báo</strong>
                          <span>{unreadCount > 0 ? `${unreadCount} chưa đọc` : 'Đã xem hết'}</span>
                        </div>
                        {unreadCount > 0 && (
                          <button type="button" onClick={handleMarkAllRead}>Đọc tất cả</button>
                        )}
                      </div>

                      {notificationsLoading ? (
                        <div className="popover-state" aria-busy="true">
                          <span className="navbar-spinner" aria-hidden="true" />
                          <span>Đang tải…</span>
                        </div>
                      ) : notificationsError ? (
                        <div className="popover-state popover-state--error" role="alert">
                          <span>{notificationsError}</span>
                          <button type="button" onClick={() => void refreshNotifications()}>Thử lại</button>
                        </div>
                      ) : notifications.length === 0 ? (
                        <div className="popover-state">
                          <BellIcon />
                          <span>Không có thông báo mới.</span>
                        </div>
                      ) : (
                        <div className="notification-list">
                          {notifications.map(item => (
                            <button
                              key={item.id}
                              type="button"
                              className="notification-item"
                              onClick={() => void handleNotificationClick(item)}
                            >
                              <span className={`notification-dot notification-dot--${item.severity.toLowerCase()}`} />
                              <span>
                                <strong>{item.title}</strong>
                                <small>{item.message}</small>
                              </span>
                            </button>
                          ))}
                        </div>
                      )}
                    </section>
                  )}
                </div>

                <div className="navbar-popover-anchor">
                  <button
                    type="button"
                    className={`profile-trigger${openPanel === 'profile' ? ' active' : ''}`}
                    onClick={() => togglePanel('profile')}
                    aria-expanded={openPanel === 'profile'}
                    aria-haspopup="menu"
                  >
                    <span className="navbar-avatar" aria-hidden="true">{initials}</span>
                    <span className="profile-trigger-copy">
                      <strong>{displayName}</strong>
                      <small>{isAdmin ? 'Quản trị viên' : 'Người dùng'}</small>
                    </span>
                    <ChevronIcon />
                  </button>

                  {openPanel === 'profile' && (
                    <div className="navbar-dropdown profile-menu" role="menu">
                      <div className="profile-menu-summary">
                        <span className="navbar-avatar navbar-avatar--large" aria-hidden="true">{initials}</span>
                        <div>
                          <strong>{displayName}</strong>
                          <span>{user?.email || `@${user?.username}`}</span>
                        </div>
                      </div>
                      <NavLink to="/profile" className="profile-menu-item" role="menuitem" onClick={closeNavigation}>
                        <UserIcon />
                        <span>
                          <strong>Hồ sơ cá nhân</strong>
                          <small>Thông tin và mật khẩu</small>
                        </span>
                      </NavLink>
                      {isAdmin && (
                        <NavLink to="/admin" className="profile-menu-item" role="menuitem" onClick={closeNavigation}>
                          <ShieldIcon />
                          <span>
                            <strong>Quản trị hệ thống</strong>
                            <small>Người dùng và cài đặt</small>
                          </span>
                        </NavLink>
                      )}
                      <button type="button" className="profile-menu-item profile-menu-item--danger" onClick={handleLogout} role="menuitem">
                        <LogoutIcon />
                        <span>
                          <strong>Đăng xuất</strong>
                          <small>Kết thúc phiên hiện tại</small>
                        </span>
                      </button>
                    </div>
                  )}
                </div>

                <button
                  type="button"
                  className={`mobile-menu-toggle${isMobileMenuOpen ? ' open' : ''}`}
                  onClick={() => {
                    setOpenPanel(null);
                    setIsMobileMenuOpen(current => !current);
                  }}
                  aria-label={isMobileMenuOpen ? 'Đóng menu' : 'Mở menu'}
                  aria-expanded={isMobileMenuOpen}
                >
                  <span />
                  <span />
                  <span />
                </button>
              </>
            ) : (
              <div className="navbar-auth-actions">
                <NavLink to="/login" className="navbar-signin" onClick={closeNavigation}>Đăng nhập</NavLink>
                <NavLink to="/register" className="navbar-register" onClick={closeNavigation}>Tạo tài khoản</NavLink>
              </div>
            )}
          </div>
        </div>

        {isAuthenticated && isMobileMenuOpen && (
          <nav className="navbar-mobile-menu" aria-label="Điều hướng di động">
            {visibleNavigation.map(item => renderNavLink(item, true))}
          </nav>
        )}
      </header>

      {isMobileMenuOpen && (
        <button
          type="button"
          className="navbar-mobile-scrim"
          onClick={() => setIsMobileMenuOpen(false)}
          aria-label="Đóng menu"
        />
      )}
    </>
  );
};
