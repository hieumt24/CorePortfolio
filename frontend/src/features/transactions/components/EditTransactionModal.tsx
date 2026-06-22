import React, { useState } from 'react';
import { updateTransaction } from '../api/transactionApi';
import { TransactionType } from '../types';
import type { TransactionDto } from '../types';
import type { AssetSummaryDto } from '../../portfolios/types';
import { NumericFormat } from 'react-number-format';
import '../../portfolios/components/CreatePortfolioModal.css';

interface EditTransactionModalProps {
  transaction: TransactionDto;
  asset: AssetSummaryDto;
  onClose: () => void;
  onSuccess: () => void;
}

export const EditTransactionModal: React.FC<EditTransactionModalProps> = ({ transaction, asset, onClose, onSuccess }) => {
  const [type, setType] = useState<TransactionType>(transaction.type);
  const [quantity, setQuantity] = useState(transaction.quantity.toString());
  const [price, setPrice] = useState(transaction.price.toString());
  const [currency, setCurrency] = useState(asset.currency || 'VND');
  const [timestamp, setTimestamp] = useState(transaction.timestamp ? new Date(transaction.timestamp).toISOString().slice(0, 16) : new Date().toISOString().slice(0, 16));
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!quantity || !price) {
      setError('Quantity and Price are required');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      await updateTransaction(transaction.id, {
        type: Number(type) as TransactionType,
        quantity: Number(quantity),
        price: Number(price),
        currency,
        timestamp: new Date(timestamp).toISOString()
      });
      onSuccess();
    } catch (err: any) {
      setError(err.message || 'Failed to update transaction');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose} style={{ zIndex: 1001 }}>
      <div className="modal-content glass-panel" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Edit Transaction: {asset.symbol}</h2>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>
        
        {error && <div className="error-alert">{error}</div>}

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label htmlFor="type">Transaction Type</label>
            <select
              id="type"
              value={type}
              onChange={e => setType(Number(e.target.value) as TransactionType)}
              disabled={loading}
              className="glass-input glass-select"
            >
              <option value={TransactionType.Buy}>Buy</option>
              <option value={TransactionType.Sell}>Sell</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="quantity">Quantity</label>
            <NumericFormat
              id="quantity"
              value={quantity}
              onValueChange={(values) => setQuantity(values.value)}
              placeholder="e.g. 1.5"
              className="glass-input"
              disabled={loading}
              thousandSeparator="."
              decimalSeparator=","
              allowNegative={false}
              autoFocus
            />
          </div>

          <div className="form-group">
            <label htmlFor="price">Price Per Unit</label>
            <div style={{ display: 'flex', gap: '10px' }}>
              <NumericFormat
                id="price"
                value={price}
                onValueChange={(values) => setPrice(values.value)}
                placeholder="e.g. 150.00"
                className="glass-input"
                style={{ flex: 1 }}
                disabled={loading}
                thousandSeparator="."
                decimalSeparator=","
                allowNegative={false}
              />
              <select
                value={currency}
                onChange={e => setCurrency(e.target.value)}
                className="glass-input glass-select"
                disabled={loading}
                style={{ width: '100px' }}
              >
                <option value="VND">VND</option>
                <option value="USD">USD</option>
              </select>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="timestamp">Date</label>
            <input
              id="timestamp"
              type="datetime-local"
              value={timestamp}
              onChange={e => setTimestamp(e.target.value)}
              className="glass-input"
              disabled={loading}
            />
          </div>


          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className={`btn ${type === TransactionType.Buy ? 'btn-primary' : 'btn-outline'}`} disabled={loading}>
              {loading ? 'Processing...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
