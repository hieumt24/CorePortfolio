import React, { useEffect, useState } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { settingsApi } from '../../features/admin/api/settingsApi';
import { notificationsApi } from '../../features/notifications/api/notificationsApi';
import type { NotificationItem } from '../../features/notifications/types';
import { useAuth } from '../../context/AuthContext';
import './Navbar.css';

const navigationItems = [
  { key: 'NAV_DASHBOARD', path: '/dashboard', label: 'Dashboard' },
  { key: 'NAV_PORTFOLIOS', path: '/portfolios', label: 'My Portfolios' },
  { key: 'NAV_TRANSACTIONS', path: '/transactions', label: 'Transactions' },
  { key: 'NAV_REPORTS', path: '/reports', label: 'Global Report' },
  { key: 'NAV_CASHFLOW', path: '/cashflow', label: 'Cashflow' },
  { key: 'NAV_WATCHLIST', path: '/watchlist', label: 'Watchlist' },
  { key: 'NAV_BUDGETS', path: '/budgets', label: 'Budgets' },
  { key: 'NAV_SAVING_GOALS', path: '/saving-goals', label: 'Mục tiêu tiết kiệm' },
  { key: 'NAV_ANALYTICS', path: '/analytics', label: 'Analytics' },
  { key: 'NAV_REBALANCING', path: '/rebalancing', label: 'Tái cân bằng' },
  { key: 'NAV_DCA_PLANS', path: '/dca-plans', label: 'Lịch DCA' },
];

export const Navbar: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated, isAdmin, user, logout } = useAuth();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [showNotifications, setShowNotifications] = useState(false);
  const [navigationVisibility, setNavigationVisibility] = useState<Record<string, boolean>>({});

  useEffect(() => {
    if (!isAuthenticated) {
      setNavigationVisibility({});
      setNotifications([]);
      setUnreadCount(0);
      return;
    }

    notificationsApi.list({ unreadOnly: true, page: 1, pageSize: 5 })
      .then(result => setNotifications(result.items))
      .catch(() => setNotifications([]));
    notificationsApi.getUnreadCount()
      .then(result => setUnreadCount(result.count))
      .catch(() => setUnreadCount(0));
    settingsApi.getNavigationFeatures()
      .then(features => setNavigationVisibility(
        Object.fromEntries(features.map(feature => [feature.key, feature.isEnabled])),
      ))
      .catch(() => setNavigationVisibility({}));
  }, [isAuthenticated]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const closeMenu = () => {
    setIsMobileMenuOpen(false);
  };

  const handleNotificationClick = async (notification: NotificationItem) => {
    try {
      await notificationsApi.markRead(notification.id);
      setNotifications(current => current.filter(item => item.id !== notification.id));
      setUnreadCount(current => Math.max(0, current - 1));
      setShowNotifications(false);
      if (notification.link) navigate(notification.link);
    } catch {
      // Keep the notification visible so the user can retry.
    }
  };

  const handleMarkAllRead = async () => {
    try {
      await notificationsApi.markAllRead();
      setNotifications([]);
      setUnreadCount(0);
    } catch {
      // Keep the current state so the user can retry.
    }
  };

  return (
    <>
      <button
        className={`mobile-menu-toggle ${isMobileMenuOpen ? 'open' : ''}`}
        onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
        aria-label="Toggle menu"
      >
        <span />
        <span />
        <span />
      </button>

      <div
        className={`mobile-overlay ${isMobileMenuOpen ? 'active' : ''}`}
        onClick={closeMenu}
      />

      <nav className={`glass-navbar ${isMobileMenuOpen ? 'mobile-open' : ''}`}>
        <div className="navbar-container">
          <div className="navbar-logo" onClick={() => navigate('/')}>
            CorePortfolio
          </div>

          <div className="navbar-menu">
            {isAuthenticated && (
              <div className="navbar-links">
                {navigationItems
                  .filter(item => navigationVisibility[item.key] !== false)
                  .map(item => (
                    <NavLink
                      key={item.key}
                      to={item.path}
                      className={location.pathname.startsWith(item.path) ? 'nav-link active' : 'nav-link'}
                      onClick={closeMenu}
                    >
                      {item.label}
                    </NavLink>
                  ))}
                <button
                  className="nav-link notification-trigger"
                  onClick={() => setShowNotifications(value => !value)}
                  aria-label="Notifications"
                >
                  🔔{unreadCount > 0 && (
                    <span className="notification-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
                  )}
                </button>
              </div>
            )}

            <div className="navbar-admin">
              {isAuthenticated ? (
                <div className="admin-actions">
                  <span className="user-greeting">Hi, {user?.email}</span>
                  {isAdmin && (
                    <NavLink to="/admin" className="btn-outline admin-panel-btn" onClick={closeMenu}>
                      Admin Panel 🛡️
                    </NavLink>
                  )}
                  <button onClick={handleLogout} className="btn-outline logout-btn">Logout</button>
                </div>
              ) : (
                <div className="auth-actions">
                  <NavLink to="/login" className="nav-link" onClick={closeMenu}>Login</NavLink>
                  <NavLink to="/register" className="nav-link admin-link" onClick={closeMenu}>Register</NavLink>
                </div>
              )}
            </div>
          </div>

          {showNotifications && (
            <div className="notification-popover">
              <div className="notification-popover-header">
                <strong>Notifications</strong>
                <button onClick={handleMarkAllRead}>Mark all read</button>
              </div>
              {notifications.length === 0 ? (
                <span className="notification-empty">No new alerts</span>
              ) : notifications.map(item => (
                <button
                  key={item.id}
                  className="notification-item"
                  onClick={() => handleNotificationClick(item)}
                >
                  <strong>{item.title}</strong>
                  <small>{item.message}</small>
                </button>
              ))}
            </div>
          )}
        </div>
      </nav>
    </>
  );
};
