import React, { useState } from 'react';
import type { AssetSummaryDto } from '../../portfolios/types';
import { useTransactions } from '../../transactions/hooks/useTransactions';
import { CreateTransactionModal } from '../../transactions/components/CreateTransactionModal';
import { TransactionType } from '../../transactions/types';
import './AssetDetailsModal.css';

interface AssetDetailsModalProps {
  asset: AssetSummaryDto;
  portfolioId: string;
  onClose: () => void;
  onDataChanged: () => void;
}

export const AssetDetailsModal: React.FC<AssetDetailsModalProps> = ({ asset, portfolioId, onClose, onDataChanged }) => {
  const { transactions, loading, error, refetch } = useTransactions(asset.assetId);
  const [isTxModalOpen, setIsTxModalOpen] = useState(false);

  const formatCurrency = (value: number | undefined | null, currency: string = 'USD') => {
    if (value === undefined || value === null) return '0.00';
    const validCurrency = currency && currency.trim() !== '' ? currency : 'USD';
    try {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: validCurrency,
        minimumFractionDigits: 2,
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
              <span className="badge">{asset.type === 0 ? 'Crypto' : asset.type === 1 ? 'Stock' : 'Fund'}</span>
              <span className="badge value-badge">Total Value: {formatCurrency(asset.currentValue, asset.currency)}</span>
            </div>
          </div>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>

        <div className="actions-bar">
          <button className="btn btn-primary glass-panel" onClick={() => setIsTxModalOpen(true)}>
            + Add Transaction
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
            <table className="glass-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Type</th>
                  <th>Quantity</th>
                  <th>Price</th>
                  <th>Total</th>
                </tr>
              </thead>
              <tbody>
                {transactions.map(tx => (
                  <tr key={tx.id}>
                    <td>{new Date(tx.timestamp).toLocaleString()}</td>
                    <td>
                      <span className={`tx-type ${tx.type === TransactionType.Buy ? 'buy' : 'sell'}`}>
                        {tx.type === TransactionType.Buy ? 'Buy' : 'Sell'}
                      </span>
                    </td>
                    <td>{tx.quantity.toLocaleString()}</td>
                    <td>{formatCurrency(tx.price, asset.currency)}</td>
                    <td>{formatCurrency(tx.quantity * tx.price, asset.currency)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
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
    </div>
  );
};
