import React from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import './Navbar.css';

export const Navbar: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated, isAdmin, user, logout } = useAuth();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="glass-navbar">
      <div className="navbar-container">
        <div className="navbar-logo">
          CorePortfolio
        </div>
        {isAuthenticated && (
          <div className="navbar-links">
            <NavLink 
              to="/portfolios" 
              className={location.pathname.startsWith('/portfolios') ? "nav-link active" : "nav-link"}
            >
              My Portfolios
            </NavLink>
            <NavLink 
              to="/transactions" 
              className={location.pathname.startsWith('/transactions') ? "nav-link active" : "nav-link"}
            >
              Transactions
            </NavLink>
            <NavLink 
              to="/reports" 
              className={location.pathname.startsWith('/reports') ? "nav-link active" : "nav-link"}
            >
              Global Report
            </NavLink>
          </div>
        )}
        
        <div className="navbar-admin">
          {isAuthenticated ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
              <span className="user-greeting">Hi, {user?.email}</span>
              {isAdmin && (
                <NavLink 
                  to="/admin" 
                  className={({ isActive }) => isActive ? "nav-link admin-link active" : "nav-link admin-link"}
                >
                  Admin
                </NavLink>
              )}
              <button onClick={handleLogout} className="btn-outline logout-btn">Logout</button>
            </div>
          ) : (
            <div style={{ display: 'flex', gap: '1rem' }}>
              <NavLink to="/login" className="nav-link">Login</NavLink>
              <NavLink to="/register" className="nav-link admin-link">Register</NavLink>
            </div>
          )}
        </div>
      </div>
    </nav>
  );
};
