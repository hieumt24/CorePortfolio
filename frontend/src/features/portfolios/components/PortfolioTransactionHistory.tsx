import React, { useEffect, useState } from 'react';
import { getAllTransactions, deleteTransaction } from '../../transactions/api/transactionApi';
import type { GlobalTransactionDto, PaginatedResult } from '../../transactions/types';
import { TransactionType } from '../../transactions/types';
import { useNotification } from '../../../context/NotificationContext';
import { formatVietnamDateTime } from '../../../shared/utils/dateTime';
import { TransactionPnlCell } from '../../transactions/components/TransactionPnlCell';
import './PortfolioTransactionHistory.css';

interface Props {
  portfolioId: string;
}

export const PortfolioTransactionHistory: React.FC<Props> = ({ portfolioId }) => {
  const [data, setData] = useState<PaginatedResult<GlobalTransactionDto> | null>(null);
  const [loading, setLoading] = useState(true);
  
  // Filters
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [typeFilter, setTypeFilter] = useState<number | ''>('');

  const { showNotification } = useNotification();

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      const params: any = { portfolioId, page, pageSize };
      if (typeFilter !== '') params.type = typeFilter;

      const result = await getAllTransactions(params);
      setData(result);
    } catch (error) {
      showNotification('Failed to fetch transactions', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTransactions();
  }, [portfolioId, page, pageSize, typeFilter]);

  const handleDelete = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this transaction?')) {
      try {
        await deleteTransaction(id);
        showNotification('Transaction deleted successfully', 'success');
        fetchTransactions();
      } catch (error) {
        showNotification('Failed to delete transaction', 'error');
      }
    }
  };

  const formatCurrency = (value: number, currency: string) => {
    try {
      const isVND = currency === 'VND';
      return new Intl.NumberFormat(isVND ? 'vi-VN' : 'en-US', {
        style: 'currency',
        currency: currency || 'USD',
        minimumFractionDigits: isVND ? 0 : 2,
      }).format(value);
    } catch {
      return value.toString();
    }
  };

  const getTypeName = (type: number) => {
    switch (type) {
      case TransactionType.Buy: return 'Buy';
      case TransactionType.Sell: return 'Sell';
      case TransactionType.Deposit: return 'Deposit';
      case TransactionType.Withdrawal: return 'Withdrawal';
      case TransactionType.Dividend: return 'Dividend';
      case TransactionType.Earn: return 'Earn';
      default: return 'Unknown';
    }
  };

  return (
    <div className="portfolio-transactions-section">
      <div className="filters-toolbar glass-panel" style={{ marginBottom: '1rem', padding: '1rem' }}>
        <div className="toolbar-group">
          <label htmlFor="ptTypeFilter" className="sr-only">Type</label>
          <select 
            id="ptTypeFilter"
            className="filter-select"
            value={typeFilter} 
            onChange={(e) => {
              setTypeFilter(e.target.value === '' ? '' : Number(e.target.value));
              setPage(1);
            }}
          >
            <option value="">All Types</option>
            <option value={TransactionType.Buy}>Buy</option>
            <option value={TransactionType.Sell}>Sell</option>
            <option value={TransactionType.Deposit}>Deposit</option>
            <option value={TransactionType.Withdrawal}>Withdrawal</option>
            <option value={TransactionType.Dividend}>Dividend</option>
            <option value={TransactionType.Earn}>Earn / Reward</option>
          </select>
        </div>
      </div>

      <div className="table-container glass-panel">
        {loading ? (
          <div className="state-panel" style={{ minHeight: '200px' }}>
            <div className="spinner"></div>
          </div>
        ) : !data || data.items.length === 0 ? (
          <div className="state-panel empty-state" style={{ minHeight: '200px' }}>
            <p>No transactions found for this portfolio.</p>
          </div>
        ) : (
          <div className="table-scroll">
            <table className="ledger-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Asset</th>
                  <th>Type</th>
                  <th className="num-col">Quantity</th>
                  <th className="num-col">Price</th>
                  <th className="num-col">Total</th>
                  <th className="num-col">PnL chưa chốt</th>
                  <th className="action-col"></th>
                </tr>
              </thead>
              <tbody>
                {data.items.map(t => (
                  <tr key={t.id}>
                    <td className="date-cell">{formatVietnamDateTime(t.date)}</td>
                    <td className="asset-cell">
                      <span className="asset-name">{t.assetName}</span>
                      <span className="asset-sym">{t.symbol}</span>
                    </td>
                    <td>
                      <span className={`badge ${getTypeName(t.type).toLowerCase()}`}>
                        {getTypeName(t.type)}
                      </span>
                    </td>
                    <td className="num-col">{t.quantity.toLocaleString(undefined, { maximumFractionDigits: 8 })}</td>
                    <td className="num-col">{formatCurrency(t.price, t.currency)}</td>
                    <td className="num-col strong">{formatCurrency(t.quantity * t.price, t.currency)}</td>
                    <td className="num-col">
                      <TransactionPnlCell
                        remainingQuantity={t.remainingQuantity}
                        unrealizedPnl={t.unrealizedPnl}
                        isClosed={t.isClosed}
                        currency={t.currency}
                        formatCurrency={formatCurrency}
                      />
                    </td>
                    <td className="action-col">
                      <button className="btn-icon" onClick={() => handleDelete(t.id)} aria-label="Delete transaction">
                        <span aria-hidden="true">×</span>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {data && data.totalCount > 0 && (
        <div className="pagination">
          <button 
            className="btn btn-outline"
            disabled={page === 1}
            onClick={() => setPage(p => p - 1)}
          >
            ← Prev
          </button>
          <span className="page-info">
            {page} / {Math.ceil(data.totalCount / data.pageSize)}
          </span>
          <button 
            className="btn btn-outline"
            disabled={page >= Math.ceil(data.totalCount / data.pageSize)}
            onClick={() => setPage(p => p + 1)}
          >
            Next →
          </button>
        </div>
      )}
    </div>
  );
};
