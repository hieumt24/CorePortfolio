import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { PortfolioDashboard } from '../features/portfolios/components/PortfolioDashboard';
import { PortfolioDetails } from '../features/portfolios/components/PortfolioDetails';
import { AdminDashboard } from '../features/admin/components/AdminDashboard';
import { SystemSettings } from '../features/admin/components/SystemSettings';
import { CategoryManagement } from '../features/admin/components/CategoryManagement';
import { CashflowCategoryManagement } from '../features/admin/components/CashflowCategoryManagement';
import { MarketAssetManagement } from '../features/admin/components/MarketAssetManagement';
import { GlobalReportDashboard } from '../features/reports/components/GlobalReportDashboard';
import { TransactionsDashboard } from '../features/transactions/components/TransactionsDashboard';
import { CashflowDashboard } from '../features/cashflows/components/CashflowDashboard';
import { WatchlistDashboard } from '../features/watchlist/components/WatchlistDashboard';
import { AnalyticsDashboard } from '../features/analytics/components/AnalyticsDashboard';
import { BudgetsPage } from '../features/budgets/components/BudgetsPage';
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
          <Route path="/cashflow" element={<ProtectedRoute><CashflowDashboard /></ProtectedRoute>} />
          <Route path="/watchlist" element={<ProtectedRoute><WatchlistDashboard /></ProtectedRoute>} />
          <Route path="/analytics" element={<ProtectedRoute><AnalyticsDashboard /></ProtectedRoute>} />
          <Route path="/budgets" element={<ProtectedRoute><BudgetsPage /></ProtectedRoute>} />
          
          
          <Route path="/admin" element={<ProtectedRoute requireAdmin={true}><AdminDashboard /></ProtectedRoute>}>
            <Route index element={<Navigate to="settings" replace />} />
            <Route path="settings" element={<SystemSettings />} />
            <Route path="categories" element={<CategoryManagement />} />
            <Route path="cashflow-categories" element={<CashflowCategoryManagement />} />
            <Route path="market-assets" element={<MarketAssetManagement />} />
          </Route>
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
