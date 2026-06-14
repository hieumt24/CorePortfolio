import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { PortfolioDashboard } from '../features/portfolios/components/PortfolioDashboard';
import { PortfolioDetails } from '../features/portfolios/components/PortfolioDetails';
import { AdminDashboard } from '../features/admin/components/AdminDashboard';
import { GlobalReportDashboard } from '../features/reports/components/GlobalReportDashboard';
import { TransactionsDashboard } from '../features/transactions/components/TransactionsDashboard';
import { Navbar } from '../shared/components/Navbar';
import { LoginPage } from '../features/auth/components/LoginPage';
import { RegisterPage } from '../features/auth/components/RegisterPage';
import { ProtectedRoute } from '../shared/components/ProtectedRoute';

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <div className="main-content">
        <Routes>
          <Route path="/" element={<Navigate to="/portfolios" replace />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          
          <Route path="/portfolios" element={<ProtectedRoute><PortfolioDashboard /></ProtectedRoute>} />
          <Route path="/portfolios/:id" element={<ProtectedRoute><PortfolioDetails /></ProtectedRoute>} />
          <Route path="/transactions" element={<ProtectedRoute><TransactionsDashboard /></ProtectedRoute>} />
          <Route path="/reports" element={<ProtectedRoute><GlobalReportDashboard /></ProtectedRoute>} />
          
          <Route path="/admin" element={<ProtectedRoute requireAdmin={true}><AdminDashboard /></ProtectedRoute>} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
