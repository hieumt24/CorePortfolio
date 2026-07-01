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
    <>
      {/* Mobile Toggle Button (Visible only on mobile) */}
      <button 
        className={`mobile-menu-toggle ${isMobileMenuOpen ? 'open' : ''}`} 
        onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
        aria-label="Toggle menu"
      >
        <span></span>
        <span></span>
        <span></span>
      </button>

      {/* Mobile Overlay */}
      <div 
        className={`mobile-overlay ${isMobileMenuOpen ? 'active' : ''}`} 
        onClick={closeMenu}
      ></div>

      <nav className={`glass-navbar ${isMobileMenuOpen ? 'mobile-open' : ''}`}>
        <div className="navbar-container">
          <div className="navbar-logo" onClick={() => navigate('/')}>
            CorePortfolio
          </div>
          
          <div className="navbar-menu">
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
                <NavLink 
                  to="/watchlist" 
                  className={location.pathname.startsWith('/watchlist') ? "nav-link active" : "nav-link"}
                  onClick={closeMenu}
                >
                  Watchlist
                </NavLink>
                <NavLink 
                  to="/budgets" 
                  className={location.pathname.startsWith('/budgets') ? "nav-link active" : "nav-link"}
                  onClick={closeMenu}
                >
                  Budgets
                </NavLink>
                <NavLink 
                  to="/analytics" 
                  className={location.pathname.startsWith('/analytics') ? "nav-link active" : "nav-link"}
                  onClick={closeMenu}
                >
                  Analytics
                </NavLink>
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
        </div>
      </nav>
    </>
  );
};
