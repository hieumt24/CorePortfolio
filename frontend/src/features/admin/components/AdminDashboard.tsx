import { useState } from 'react';
import { Outlet, NavLink } from 'react-router-dom';
import './AdminDashboard.css';

export function AdminDashboard() {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isMobileSidebarOpen, setIsMobileSidebarOpen] = useState(false);

  const toggleSidebar = () => {
    setIsSidebarCollapsed(!isSidebarCollapsed);
  };

  const toggleMobileSidebar = () => {
    setIsMobileSidebarOpen(!isMobileSidebarOpen);
  };

  const navItems = [
    { path: 'settings', icon: '⚙️', label: 'System Settings' },
    { path: 'categories', icon: '📁', label: 'Categories' },
    { path: 'cashflow-categories', icon: '💸', label: 'Cashflow Categories' },
    { path: 'market-assets', icon: '📈', label: 'Market Assets' },
  ];

  return (
    <div className="admin-layout">
      {/* Mobile Backdrop */}
      {isMobileSidebarOpen && (
        <div className="sidebar-backdrop" onClick={toggleMobileSidebar}></div>
      )}

      {/* Sidebar */}
      <aside className={`admin-sidebar ${isSidebarCollapsed ? 'collapsed' : ''} ${isMobileSidebarOpen ? 'mobile-open' : ''}`}>
        <div className="sidebar-header">
          <div className="sidebar-brand">
            <span className="brand-icon">🛡️</span>
            {!isSidebarCollapsed && <span className="brand-text">Admin Panel</span>}
          </div>
          <button className="sidebar-toggle-btn desktop-only" onClick={toggleSidebar} title="Toggle Sidebar">
            {isSidebarCollapsed ? '»' : '«'}
          </button>
        </div>

        <nav className="sidebar-nav">
          {navItems.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
              onClick={() => setIsMobileSidebarOpen(false)} // Close on mobile click
            >
              <span className="nav-icon">{item.icon}</span>
              {!isSidebarCollapsed && <span className="nav-label">{item.label}</span>}
            </NavLink>
          ))}
        </nav>
      </aside>

      {/* Main Content Area */}
      <main className="admin-main">
        <header className="mobile-header mobile-only">
          <button className="mobile-menu-btn" onClick={toggleMobileSidebar}>
            ☰
          </button>
          <h2>Admin Control Panel</h2>
        </header>
        
        <div className="admin-container">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
