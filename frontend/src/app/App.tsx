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
import { WatchlistPage } from '../features/watchlist/components/WatchlistPage';
import { AnalyticsDashboard } from '../features/analytics/components/AnalyticsDashboard';
import { PerformanceCenter } from '../features/performance/components/PerformanceCenter';
import { BudgetsPage } from '../features/budgets/components/BudgetsPage';
import { DcaPlansPage } from '../features/dcaPlans/components/DcaPlansPage';
import { RebalancingPlansPage } from '../features/rebalancing/components/RebalancingPlansPage';
import { SavingGoalsPage } from '../features/savingGoals/components/SavingGoalsPage';
import { Navbar } from '../shared/components/Navbar';
import { LoginPage } from '../features/auth/components/LoginPage';
import { RegisterPage } from '../features/auth/components/RegisterPage';
import { ProtectedRoute } from '../shared/components/ProtectedRoute';
import { OverviewDashboard } from '../features/dashboard/components/OverviewDashboard';
import { AdminOverview } from '../features/admin/components/AdminOverview';
import { UserManagement } from '../features/admin/components/UserManagement';
import { ProfilePage } from '../features/profile/components/ProfilePage';
import {
  AuditLogPage,
  OperationsPage,
  UserDetailPage,
  MarketDataControlPage,
  NotificationManagementPage,
  RolesPermissionsPage,
  DataIntegrityPage,
  BackupConfigurationPage,
} from '../features/admin/components/ControlPlanePages';

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <div className="main-content">
        <Routes>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          
          <Route path="/dashboard" element={<ProtectedRoute><OverviewDashboard /></ProtectedRoute>} />
          <Route path="/portfolios" element={<ProtectedRoute><PortfolioDashboard /></ProtectedRoute>} />
          <Route path="/portfolios/:id" element={<ProtectedRoute><PortfolioDetails /></ProtectedRoute>} />
          <Route path="/transactions" element={<ProtectedRoute><TransactionsDashboard /></ProtectedRoute>} />
          <Route path="/reports" element={<ProtectedRoute><GlobalReportDashboard /></ProtectedRoute>} />
          <Route path="/cashflow" element={<ProtectedRoute><CashflowDashboard /></ProtectedRoute>} />
          <Route path="/watchlist" element={<ProtectedRoute><WatchlistPage /></ProtectedRoute>} />
          <Route path="/analytics" element={<ProtectedRoute><AnalyticsDashboard /></ProtectedRoute>} />
          <Route path="/analytics/performance" element={<ProtectedRoute><PerformanceCenter /></ProtectedRoute>} />
          <Route path="/budgets" element={<ProtectedRoute><BudgetsPage /></ProtectedRoute>} />
          <Route path="/saving-goals" element={<ProtectedRoute><SavingGoalsPage /></ProtectedRoute>} />
          <Route path="/rebalancing" element={<ProtectedRoute><RebalancingPlansPage /></ProtectedRoute>} />
          <Route path="/dca-plans" element={<ProtectedRoute><DcaPlansPage /></ProtectedRoute>} />
          <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
          
          
          <Route path="/admin" element={<ProtectedRoute requireAdmin={true}><AdminDashboard /></ProtectedRoute>}>
            <Route index element={<Navigate to="overview" replace />} />
            <Route path="overview" element={<AdminOverview />} />
            <Route path="users" element={<UserManagement />} />
            <Route path="settings" element={<SystemSettings />} />
            <Route path="categories" element={<CategoryManagement />} />
            <Route path="cashflow-categories" element={<CashflowCategoryManagement />} />
            <Route path="market-assets" element={<MarketAssetManagement />} />
            <Route path="audit" element={<AuditLogPage />} />
            <Route path="operations" element={<OperationsPage />} />
            <Route path="users/:id" element={<UserDetailPage />} />
            <Route path="market-data" element={<MarketDataControlPage />} />
            <Route path="notifications" element={<NotificationManagementPage />} />
            <Route path="roles" element={<RolesPermissionsPage />} />
            <Route path="integrity" element={<DataIntegrityPage />} />
            <Route path="system" element={<BackupConfigurationPage />} />
          </Route>
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
