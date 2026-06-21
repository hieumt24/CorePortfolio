import React, { useState } from 'react';
import type { AssetSummaryDto } from '../../portfolios/types';
import { useTransactions } from '../../transactions/hooks/useTransactions';
import { CreateTransactionModal } from '../../transactions/components/CreateTransactionModal';
import { EditTransactionModal } from '../../transactions/components/EditTransactionModal';
import { UpdatePriceModal } from './UpdatePriceModal';
import { TransactionType } from '../../transactions/types';
import type { TransactionDto } from '../../transactions/types';
import { deleteTransaction } from '../../transactions/api/transactionApi';
import { deleteAsset } from '../api/assetApi';
import { useNotification } from '../../../context/NotificationContext';
import { useAuth } from '../../../context/AuthContext';
import './AssetDetailsModal.css';

interface AssetDetailsModalProps {
  asset: AssetSummaryDto;
  portfolioId: string;
  onClose: () => void;
  onDataChanged: () => void;
}

export const AssetDetailsModal: React.FC<AssetDetailsModalProps> = ({ asset, portfolioId, onClose, onDataChanged }) => {
  const { showNotification } = useNotification();
  const { isAdmin } = useAuth();
  const { transactions, loading, error, refetch } = useTransactions(asset.assetId);
  const [isTxModalOpen, setIsTxModalOpen] = useState(false);
  const [editingTx, setEditingTx] = useState<TransactionDto | null>(null);
  const [deletingTxId, setDeletingTxId] = useState<string | null>(null);
  const [isDeletingAsset, setIsDeletingAsset] = useState(false);
  const [isUpdatePriceModalOpen, setIsUpdatePriceModalOpen] = useState(false);

  const handleDelete = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this transaction?')) {
      try {
        setDeletingTxId(id);
        await deleteTransaction(id);
        refetch();
        onDataChanged();
        showNotification('Transaction deleted successfully', 'success');
      } catch (err) {
        console.error('Failed to delete transaction', err);
        showNotification('Failed to delete transaction.', 'error');
      } finally {
        setDeletingTxId(null);
      }
    }
  };

  const handleDeleteAsset = async () => {
    if (window.confirm('CẢNH BÁO: Hành động này sẽ xóa vĩnh viễn Asset này khỏi danh mục cùng TOÀN BỘ lịch sử giao dịch. Bạn có chắc chắn?')) {
      try {
        setIsDeletingAsset(true);
        await deleteAsset(portfolioId, asset.assetId);
        onDataChanged();
        onClose();
        showNotification('Asset and its transactions deleted successfully', 'success');
      } catch (err) {
        console.error('Failed to delete asset', err);
        showNotification('Failed to delete asset.', 'error');
      } finally {
        setIsDeletingAsset(false);
      }
    }
  };

  const formatCurrency = (value: number | undefined | null, currency: string | null | undefined) => {
    if (value === undefined || value === null) return '0';
    let validCurrency = currency && currency.trim() !== '' ? currency : 'VND';
    const isVND = validCurrency === 'VND';
    
    try {
      return new Intl.NumberFormat(isVND ? 'vi-VN' : 'en-US', {
        style: 'currency',
        currency: validCurrency,
        minimumFractionDigits: isVND ? 0 : 2,
        maximumFractionDigits: isVND ? 0 : 2,
      }).format(value);
    } catch (e) {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 2,
      }).format(value);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="asset-details-content glass-panel" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <div className="header-info">
            <h2>{asset.name} ({asset.symbol})</h2>
            <div className="asset-badges">
              <span className="badge">{asset.categoryName}</span>
              <span className="badge value-badge">Total Value: {formatCurrency(asset.currentValue, asset.currency)}</span>
              {isAdmin && (
                <button 
                  className="btn btn-sm btn-outline" 
                  style={{ marginLeft: '10px', padding: '2px 8px', fontSize: '12px' }}
                  onClick={() => setIsUpdatePriceModalOpen(true)}
                >
                  ✎ Update Price
                </button>
              )}
            </div>
          </div>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>

        <div className="actions-bar" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <button className="btn btn-primary glass-panel" onClick={() => setIsTxModalOpen(true)}>
            + Add Transaction
          </button>
          <button 
            className="btn glass-panel" 
            style={{ backgroundColor: 'var(--sell-color)' }}
            disabled={isDeletingAsset}
            onClick={handleDeleteAsset}
          >
            {isDeletingAsset ? 'Deleting...' : 'Delete Asset'}
          </button>
        </div>

        <div className="transactions-list">
          <h3>Transaction History</h3>
          {loading ? (
            <p>Loading transactions...</p>
          ) : error ? (
            <p className="error">{error}</p>
          ) : transactions.length === 0 ? (
            <p className="empty">No transactions found for this asset.</p>
          ) : (
            <div className="table-responsive">
              <table className="glass-table">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Type</th>
                    <th>Quantity</th>
                    <th>Price</th>
                    <th>Total</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {transactions.map(tx => (
                    <tr key={tx.id}>
                      <td>{new Date(tx.timestamp).toLocaleString()}</td>
                      <td>
                        <span className={`tx-type ${
                          tx.type === TransactionType.Buy ? 'buy' : 
                          tx.type === TransactionType.Sell ? 'sell' :
                          tx.type === TransactionType.Dividend ? 'dividend' : ''
                        }`}>
                          {tx.type === TransactionType.Buy ? 'Buy' : 
                           tx.type === TransactionType.Sell ? 'Sell' : 
                           tx.type === TransactionType.Dividend ? 'Dividend' :
                           tx.type === TransactionType.Deposit ? 'Deposit' :
                           tx.type === TransactionType.Withdrawal ? 'Withdrawal' : 'Unknown'}
                        </span>
                      </td>
                      <td>{tx.quantity.toLocaleString(undefined, { maximumFractionDigits: 8 })}</td>
                      <td>{formatCurrency(tx.price, asset.currency)}</td>
                      <td>{formatCurrency(tx.quantity * tx.price, asset.currency)}</td>
                      <td>
                        <button className="btn btn-sm btn-outline" style={{marginRight: '8px', padding: '4px 8px'}} onClick={() => setEditingTx(tx)}>Edit</button>
                        <button className="btn btn-sm" style={{padding: '4px 8px', backgroundColor: 'var(--sell-color)'}} disabled={deletingTxId === tx.id} onClick={() => handleDelete(tx.id)}>
                          {deletingTxId === tx.id ? '...' : 'Del'}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {isTxModalOpen && (
        <CreateTransactionModal
          portfolioId={portfolioId}
          asset={asset}
          onClose={() => setIsTxModalOpen(false)}
          onSuccess={() => {
            setIsTxModalOpen(false);
            refetch();
            onDataChanged();
          }}
        />
      )}

      {editingTx && (
        <EditTransactionModal
          transaction={editingTx}
          asset={asset}
          onClose={() => setEditingTx(null)}
          onSuccess={() => {
            setEditingTx(null);
            refetch();
            onDataChanged();
          }}
        />
      )}

      {isUpdatePriceModalOpen && (
        <UpdatePriceModal
          asset={asset}
          onClose={() => setIsUpdatePriceModalOpen(false)}
          onSuccess={() => {
            setIsUpdatePriceModalOpen(false);
            onDataChanged();
          }}
        />
      )}
    </div>
  );
};
