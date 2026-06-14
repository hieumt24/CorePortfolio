import { Outlet } from 'react-router-dom';
import './AdminDashboard.css';

export function AdminDashboard() {
  return (
    <div className="admin-layout">
      <div className="admin-header">
        <h1>Admin Control Panel</h1>
      </div>

      <div className="admin-container">
        <Outlet />
      </div>
    </div>
  );
}
