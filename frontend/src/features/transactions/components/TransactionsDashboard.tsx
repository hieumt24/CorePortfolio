import React, { useEffect, useState } from 'react';
import { getAllTransactions, deleteTransaction } from '../api/transactionApi';
import type { GlobalTransactionDto, PaginatedResult } from '../types';
import { TransactionType } from '../types';
import { useNotification } from '../../../context/NotificationContext';
import { GlobalCreateTransactionModal } from './GlobalCreateTransactionModal';
import './TransactionsDashboard.css';

export const TransactionsDashboard: React.FC = () => {
  const [data, setData] = useState<PaginatedResult<GlobalTransactionDto> | null>(null);
  const [loading, setLoading] = useState(true);
  
  // Filters
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [typeFilter, setTypeFilter] = useState<number | ''>('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const { showNotification } = useNotification();

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      const params: any = { page, pageSize };
      if (typeFilter !== '') params.type = typeFilter;
      if (startDate) params.startDate = new Date(startDate).toISOString();
      if (endDate) params.endDate = new Date(endDate).toISOString();

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
  }, [page, pageSize, typeFilter, startDate, endDate]);

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
      default: return 'Unknown';
    }
  };

  return (
    <div className="transactions-dashboard">
      <div className="transactions-header">
        <h1>Transactions</h1>
        <button className="btn btn-primary glass-panel" onClick={() => setIsCreateModalOpen(true)}>
          + Add Transaction
        </button>
      </div>

      <div className="filters-container glass-panel">
        <select 
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
        </select>

        <input 
          type="date" 
          className="filter-input"
          value={startDate}
          onChange={(e) => { setStartDate(e.target.value); setPage(1); }}
          title="Start Date"
        />
        <input 
          type="date" 
          className="filter-input"
          value={endDate}
          onChange={(e) => { setEndDate(e.target.value); setPage(1); }}
          title="End Date"
        />
      </div>

      <div className="transactions-table-container glass-panel">
        {loading ? (
          <div style={{ padding: '2rem', textAlign: 'center' }}>Loading...</div>
        ) : !data || data.items.length === 0 ? (
          <div style={{ padding: '2rem', textAlign: 'center' }}>No transactions found.</div>
        ) : (
          <div className="table-responsive">
            <table className="transactions-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Portfolio</th>
                  <th>Asset</th>
                  <th>Type</th>
                  <th>Quantity</th>
                  <th>Price</th>
                  <th>Total</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map(t => (
                  <tr key={t.id}>
                    <td>{new Date(t.date).toLocaleString()}</td>
                    <td>{t.portfolioName}</td>
                    <td>{t.assetName} ({t.symbol})</td>
                    <td>
                      <span className={`type-badge ${getTypeName(t.type).toLowerCase()}`}>
                        {getTypeName(t.type)}
                      </span>
                    </td>
                    <td>{t.quantity.toLocaleString()}</td>
                    <td>{formatCurrency(t.price, t.currency)}</td>
                    <td>{formatCurrency(t.quantity * t.price, t.currency)}</td>
                    <td>
                      <div className="action-buttons">
                        <button className="btn-icon delete" onClick={() => handleDelete(t.id)}>
                          Del
                        </button>
                      </div>
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
            className="btn btn-secondary glass-panel"
            disabled={page === 1}
            onClick={() => setPage(p => p - 1)}
          >
            Previous
          </button>
          <span className="page-info">
            Page {data.page} of {Math.ceil(data.totalCount / data.pageSize)} ({data.totalCount} total)
          </span>
          <button 
            className="btn btn-secondary glass-panel"
            disabled={page >= Math.ceil(data.totalCount / data.pageSize)}
            onClick={() => setPage(p => p + 1)}
          >
            Next
          </button>
        </div>
      )}

      {isCreateModalOpen && (
        <GlobalCreateTransactionModal 
          onClose={() => setIsCreateModalOpen(false)}
          onSuccess={() => {
            setIsCreateModalOpen(false);
            showNotification('Transaction created successfully', 'success');
            fetchTransactions();
          }}
        />
      )}
    </div>
  );
};
