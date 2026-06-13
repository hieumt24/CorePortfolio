import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { PortfolioDashboard } from '../features/portfolios/components/PortfolioDashboard';
import { PortfolioDetails } from '../features/portfolios/components/PortfolioDetails';
import { AdminDashboard } from '../features/admin/components/AdminDashboard';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/portfolios" replace />} />
        <Route path="/portfolios" element={<PortfolioDashboard />} />
        <Route path="/portfolios/:id" element={<PortfolioDetails />} />
        <Route path="/admin" element={<AdminDashboard />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
