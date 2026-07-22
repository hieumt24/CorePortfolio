import React, { useEffect, useState } from 'react';
import { getAllTransactions, deleteTransaction } from '../api/transactionApi';
import type { GlobalTransactionDto, PaginatedResult, TransactionDto } from '../types';
import { TransactionType } from '../types';
import { useNotification } from '../../../context/NotificationContext';
import { GlobalCreateTransactionModal } from './GlobalCreateTransactionModal';
import { EditTransactionModal } from './EditTransactionModal';
import type { AssetSummaryDto } from '../../portfolios/types';
import './TransactionsDashboard.css';

export const TransactionsDashboard: React.FC = () => {
  const [data, setData] = useState<PaginatedResult<GlobalTransactionDto> | null>(null);
  const [loading, setLoading] = useState(true);
  
  // Filters
  const [page, setPage] = useState(1);
  const [pageSize] = useState(100);
  const [typeFilter, setTypeFilter] = useState<number | ''>('');
  const [assetGroup, setAssetGroup] = useState<'all' | 'crypto' | 'stock' | 'fund'>('all');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingTransaction, setEditingTransaction] = useState<GlobalTransactionDto | null>(null);
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

  const matchesGroup = (category: string, group = assetGroup) => {
    const value = category.toLowerCase();
    if (group === 'all') return true;
    if (group === 'crypto') return value.includes('crypto') || value.includes('tiền mã hóa') || value.includes('tiền điện tử');
    if (group === 'stock') return value.includes('stock') || value.includes('cổ phiếu') || value.includes('chứng khoán');
    return value.includes('fund') || value.includes('quỹ') || value.includes('ccq') || value.includes('etf');
  };
  const visibleItems = data?.items.filter(item => matchesGroup(item.categoryName)) ?? [];
  const countFor = (group: 'crypto' | 'stock' | 'fund') => data?.items.filter(item => matchesGroup(item.categoryName, group)).length ?? 0;

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

  const toEditTransaction = (item: GlobalTransactionDto): TransactionDto => ({
    id: item.id, type: item.type, quantity: item.quantity, price: item.price,
    fee: item.fee, notes: item.notes, timestamp: item.date,
  });

  const toEditAsset = (item: GlobalTransactionDto): AssetSummaryDto => ({
    assetId: item.assetId, marketAssetId: item.assetId, symbol: item.symbol, name: item.assetName,
    categoryName: item.categoryName, currency: item.currency, currentPrice: item.price,
    totalQuantity: item.quantity, totalCost: item.quantity * item.price, currentValue: item.quantity * item.price,
    totalBought: item.quantity, averageCost: item.price, realizedPnl: 0, unrealizedPnl: 0, fees: item.fee, priceUpdatedAt: item.date,
  });

  return (
    <div className="container dashboard-layout">
      {/* Decorative blurred blobs */}
      <div className="mesh-blob blob-1"></div>
      <div className="mesh-blob blob-2" style={{ left: '60%', top: '40%' }}></div>

      <header className="dashboard-header">
        <div className="header-titles">
          <h1 className="gradient-text">Transactions Ledger</h1>
          <p className="subtitle">Theo dõi crypto, cổ phiếu và CCQ trong một dòng thời gian rõ ràng</p>
        </div>
        <div className="header-actions">
          <button className="btn btn-primary" onClick={() => setIsCreateModalOpen(true)}>
            + Thêm giao dịch
          </button>
        </div>
      </header>

      <section className="transaction-groups" aria-label="Asset groups">
        {(['all', 'crypto', 'stock', 'fund'] as const).map(group => (
          <button key={group} className={`group-card ${group} ${assetGroup === group ? 'active' : ''}`} onClick={() => { setAssetGroup(group); setPage(1); }}>
            <span className="group-icon">{group === 'crypto' ? '₿' : group === 'stock' ? '↗' : group === 'fund' ? '◈' : '◎'}</span>
            <span><strong>{group === 'all' ? 'Tất cả' : group === 'crypto' ? 'Crypto' : group === 'stock' ? 'Cổ phiếu' : 'CCQ / ETF'}</strong><small>{group === 'all' ? data?.totalCount ?? 0 : countFor(group)} giao dịch</small></span>
          </button>
        ))}
      </section>

      <div className="filters-toolbar glass-panel">
        <div className="toolbar-group">
          <label htmlFor="typeFilter" className="sr-only">Type</label>
          <select 
            id="typeFilter"
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

        <div className="toolbar-group">
          <label htmlFor="startDate" className="sr-only">Start Date</label>
          <input 
            id="startDate"
            type="date" 
            className="filter-input"
            value={startDate}
            onChange={(e) => { setStartDate(e.target.value); setPage(1); }}
            title="Start Date"
          />
          <span className="toolbar-sep">to</span>
          <label htmlFor="endDate" className="sr-only">End Date</label>
          <input 
            id="endDate"
            type="date" 
            className="filter-input"
            value={endDate}
            onChange={(e) => { setEndDate(e.target.value); setPage(1); }}
            title="End Date"
          />
        </div>
      </div>

      <div className="table-container glass-panel">
        {loading ? (
          <div className="state-panel">
            <div className="spinner"></div>
          </div>
        ) : visibleItems.length === 0 ? (
          <div className="state-panel empty-state">
            <p>No transactions match your filters.</p>
          </div>
        ) : (
          <div className="table-scroll">
            <table className="ledger-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Portfolio</th>
                  <th>Asset</th>
                  <th>Type</th>
                  <th className="num-col">Quantity</th>
                  <th className="num-col">Price</th>
                  <th className="num-col">Total</th>
                  <th className="action-col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {visibleItems.map(t => (
                  <tr key={t.id}>
                    <td className="date-cell">{new Date(t.date).toLocaleDateString()}</td>
                    <td>{t.portfolioName}</td>
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
                    <td className="action-col">
                      <button className="btn-icon edit-action" onClick={() => setEditingTransaction(t)} aria-label={`Edit ${t.symbol} transaction`} title="Edit transaction">
                        <span aria-hidden="true">✎</span>
                      </button>
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
            {page} / {Math.ceil(data.totalCount / data.pageSize)} <span className="page-total">({data.totalCount} total)</span>
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

      {isCreateModalOpen && (
        <GlobalCreateTransactionModal 
          initialCategory={assetGroup === 'all' ? undefined : assetGroup === 'stock' ? 'stock' : assetGroup === 'fund' ? 'fund' : 'crypto'}
          onClose={() => setIsCreateModalOpen(false)}
          onSuccess={() => {
            setIsCreateModalOpen(false);
            showNotification('Transaction recorded successfully', 'success');
            fetchTransactions();
          }}
        />
      )}
      {editingTransaction && (
        <EditTransactionModal
          transaction={toEditTransaction(editingTransaction)}
          asset={toEditAsset(editingTransaction)}
          onClose={() => setEditingTransaction(null)}
          onSuccess={() => {
            setEditingTransaction(null);
            showNotification('Transaction updated successfully', 'success');
            fetchTransactions();
          }}
        />
      )}
    </div>
  );
};
