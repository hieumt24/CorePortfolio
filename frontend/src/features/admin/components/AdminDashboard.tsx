import { useEffect, useMemo, useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { adminApi } from '../api/adminApi';
import './AdminDashboard.css';

const navItems = [
  { path: 'overview', icon: '01', label: 'Tổng quan' },
  { path: 'users', icon: '02', label: 'Người dùng' },
  { path: 'settings', icon: '03', label: 'Cài đặt hệ thống' },
  { path: 'categories', icon: '04', label: 'Danh mục tài sản' },
  { path: 'cashflow-categories', icon: '05', label: 'Danh mục dòng tiền' },
  { path: 'market-assets', icon: '06', label: 'Market Assets' },
  { path: 'market-data', icon: '07', label: 'Market Data' },
  { path: 'notifications', icon: '08', label: 'Thông báo' },
  { path: 'audit', icon: '09', label: 'Audit Log' },
  { path: 'operations', icon: '10', label: 'Operations' },
  { path: 'roles', icon: '11', label: 'Role & Permission' },
  { path: 'integrity', icon: '12', label: 'Data Integrity' },
  { path: 'system', icon: '13', label: 'Backup & System' },
];

export function AdminDashboard() {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isMobileSidebarOpen, setIsMobileSidebarOpen] = useState(false);
  const [permissions, setPermissions] = useState<string[]>([]);
  useEffect(() => {
    void adminApi.getCapabilities().then(result => setPermissions(result.permissions));
  }, []);
  const visibleItems = useMemo(() => {
    const requirements: Record<string, string> = {
      users: 'Users.Read', settings: 'Settings.Manage', 'market-assets': 'MarketData.Manage',
      'market-data': 'MarketData.Read', notifications: 'Notifications.Manage',
      audit: 'Audit.Read', operations: 'Operations.Read', roles: 'Roles.Manage',
      integrity: 'Integrity.Read', system: 'Backups.Read',
    };
    return navItems.filter(item => !requirements[item.path] || permissions.includes(requirements[item.path]));
  }, [permissions]);

  return (
    <div className="admin-layout">
      {isMobileSidebarOpen && <button className="sidebar-backdrop" onClick={() => setIsMobileSidebarOpen(false)} aria-label="Đóng menu" />}
      <aside className={`admin-sidebar ${isSidebarCollapsed ? 'collapsed' : ''} ${isMobileSidebarOpen ? 'mobile-open' : ''}`}>
        <div className="sidebar-header">
          <div className="sidebar-brand"><span className="brand-icon">CP</span>{!isSidebarCollapsed && <span className="brand-text">Admin Console</span>}</div>
          <button className="sidebar-toggle-btn desktop-only" onClick={() => setIsSidebarCollapsed(value => !value)} aria-label="Thu gọn menu">{isSidebarCollapsed ? '›' : '‹'}</button>
        </div>
        <nav className="sidebar-nav" aria-label="Điều hướng quản trị">
          {visibleItems.map(item => <NavLink key={item.path} to={item.path} title={isSidebarCollapsed ? item.label : undefined} className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`} onClick={() => setIsMobileSidebarOpen(false)}><span className="nav-icon">{item.icon}</span>{!isSidebarCollapsed && <span className="nav-label">{item.label}</span>}</NavLink>)}
        </nav>
      </aside>
      <main className="admin-main">
        <header className="mobile-header mobile-only"><button className="mobile-menu-btn" onClick={() => setIsMobileSidebarOpen(value => !value)} aria-label="Mở menu">☰</button><h2>Admin Console</h2></header>
        <div className="admin-container"><Outlet /></div>
      </main>
    </div>
  );
}
