import React, { useState, useEffect } from 'react';
import { watchlistApi } from '../api/watchlistApi';
import type { WatchlistDto } from '../types';
import { useNotification } from '../../../context/NotificationContext';
import { AddWatchlistModal } from './AddWatchlistModal';
import '../../cashflows/components/CashflowDashboard.css'; // Re-use some styles
import { formatVietnamDate } from '../../../shared/utils/dateTime';

export const WatchlistDashboard: React.FC = () => {
  const { showNotification } = useNotification();
  const [watchlist, setWatchlist] = useState<WatchlistDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);

  const fetchWatchlist = async () => {
    try {
      setLoading(true);
      const data = await watchlistApi.getWatchlist();
      setWatchlist(data);
    } catch (error) {
      console.error(error);
      showNotification('Failed to load watchlist', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchWatchlist();
  }, []);

  const handleRemove = async (id: string) => {
    if (!window.confirm('Remove from watchlist?')) return;
    try {
      await watchlistApi.removeFromWatchlist(id);
      showNotification('Removed from watchlist', 'success');
      fetchWatchlist();
    } catch (error) {
      console.error(error);
      showNotification('Failed to remove item', 'error');
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const formatDate = (dateStr: string) => {
    return formatVietnamDate(dateStr, '—', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  return (
    <div className="cashflow-dashboard">
      <div className="dashboard-header">
        <div className="header-title">
          <h1>👀 Watchlist</h1>
          <p className="subtitle">Theo dõi các tài sản tiềm năng</p>
        </div>
        <div className="header-actions">
          <button className="btn btn-primary" onClick={() => setIsAddModalOpen(true)}>
            + Add to Watchlist
          </button>
        </div>
      </div>

      <div className="dashboard-grid" style={{ gridTemplateColumns: '1fr' }}>
        <div className="history-section">
          {loading ? (
            <div className="loading-state">
               <div className="spinner"></div>
               <p>Đang tải dữ liệu...</p>
            </div>
          ) : (
            <div className="transactions-list">
              {watchlist.length === 0 && (
                <div className="empty-state">
                  <div className="empty-icon">👀</div>
                  <p>Danh sách theo dõi trống. Hãy thêm tài sản từ trang Admin/Market Assets.</p>
                </div>
              )}
              {watchlist.map((item) => (
                <div key={item.id} className="transaction-item">
                  <div className="transaction-icon" style={{ backgroundColor: `rgba(59, 130, 246, 0.2)`, color: '#3b82f6' }}>
                    📈
                  </div>
                  <div className="transaction-details">
                    <h4>{item.symbol} - {item.name}</h4>
                    <div className="meta-info">
                      <span className="portfolio-tag">{item.assetCategoryName}</span>
                      <span className="date-tag">Added: {formatDate(item.addedAt)}</span>
                    </div>
                  </div>
                  <div className="transaction-amount positive">
                    {formatCurrency(item.currentPrice)}
                  </div>
                  <div className="transaction-actions" style={{ display: 'flex', gap: '0.5rem', marginLeft: '1rem' }}>
                    <button 
                      className="btn-icon" 
                      onClick={() => handleRemove(item.id)}
                      style={{ background: 'transparent', border: '1px solid rgba(239, 68, 68, 0.3)', color: '#ef4444', borderRadius: '4px', padding: '0.2rem 0.5rem', cursor: 'pointer' }}
                      title="Xóa khỏi Watchlist"
                    >
                      🗑️
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {isAddModalOpen && (
        <AddWatchlistModal 
          onClose={() => setIsAddModalOpen(false)} 
          onSuccess={() => {
            setIsAddModalOpen(false);
            fetchWatchlist();
          }} 
        />
      )}
    </div>
  );
};
