import React, { useState } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import './Navbar.css';

export const Navbar: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated, isAdmin, user, logout } = useAuth();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const closeMenu = () => {
    setIsMobileMenuOpen(false);
  };

  return (
    <nav className="glass-navbar">
      <div className="navbar-container">
        <div className="navbar-logo" onClick={() => navigate('/')}>
          CorePortfolio
        </div>
        
        {/* Mobile Hamburger Toggle */}
        <button 
          className={`mobile-menu-toggle ${isMobileMenuOpen ? 'open' : ''}`} 
          onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
          aria-label="Toggle menu"
        >
          <span></span>
          <span></span>
          <span></span>
        </button>

        <div className={`navbar-menu ${isMobileMenuOpen ? 'active' : ''}`}>
          {isAuthenticated && (
            <div className="navbar-links">
              <NavLink 
                to="/portfolios" 
                className={location.pathname.startsWith('/portfolios') ? "nav-link active" : "nav-link"}
                onClick={closeMenu}
              >
                My Portfolios
              </NavLink>
              <NavLink 
                to="/transactions" 
                className={location.pathname.startsWith('/transactions') ? "nav-link active" : "nav-link"}
                onClick={closeMenu}
              >
                Transactions
              </NavLink>
              <NavLink 
                to="/reports" 
                className={location.pathname.startsWith('/reports') ? "nav-link active" : "nav-link"}
                onClick={closeMenu}
              >
                Global Report
              </NavLink>
              <NavLink 
                to="/cashflow" 
                className={location.pathname.startsWith('/cashflow') ? "nav-link active" : "nav-link"}
                onClick={closeMenu}
              >
                Cashflow
              </NavLink>
            </div>
          )}
          
          <div className="navbar-admin">
            {isAuthenticated ? (
              <div className="admin-actions">
                <span className="user-greeting">Hi, {user?.email}</span>
                {isAdmin && (
                  <div className="navbar-dropdown">
                    <span className="nav-link admin-link dropdown-toggle">Manage ▼</span>
                    <div className="dropdown-menu">
                      <NavLink to="/admin/settings" className={({ isActive }) => isActive ? "dropdown-item active" : "dropdown-item"} onClick={closeMenu}>System Settings</NavLink>
                      <NavLink to="/admin/categories" className={({ isActive }) => isActive ? "dropdown-item active" : "dropdown-item"} onClick={closeMenu}>Category Management</NavLink>
                      <NavLink to="/admin/cashflow-categories" className={({ isActive }) => isActive ? "dropdown-item active" : "dropdown-item"} onClick={closeMenu}>Cashflow Categories</NavLink>
                      <NavLink to="/admin/market-assets" className={({ isActive }) => isActive ? "dropdown-item active" : "dropdown-item"} onClick={closeMenu}>Market Asset Management</NavLink>
                    </div>
                  </div>
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
      </div>
    </nav>
  );
};
