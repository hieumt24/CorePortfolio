import React, { useState } from 'react';
import { createTransaction } from '../api/transactionApi';
import { TransactionType } from '../types';
import type { AssetSummaryDto } from '../../portfolios/types';
import { NumericFormat } from 'react-number-format';
import '../../portfolios/components/CreatePortfolioModal.css';
import { calculateCashImpact } from '../utils/transactionImpact';

interface CreateTransactionModalProps {
  portfolioId: string;
  asset: AssetSummaryDto;
  onClose: () => void;
  onSuccess: () => void;
}

export const CreateTransactionModal: React.FC<CreateTransactionModalProps> = ({ portfolioId, asset, onClose, onSuccess }) => {
  const [type, setType] = useState<TransactionType>(TransactionType.Buy);
  const [quantity, setQuantity] = useState('');
  const [price, setPrice] = useState('');
  const [fee, setFee] = useState('0');
  const [notes, setNotes] = useState('');
  const [currency, setCurrency] = useState(asset.currency || 'USD');
  const [timestamp, setTimestamp] = useState(new Date().toISOString().slice(0, 16));
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
      await createTransaction({
        portfolioId,
        assetId: asset.assetId,
        type: Number(type) as TransactionType,
        quantity: Number(quantity),
        price: Number(price),
        fee: Number(fee),
        notes,
        currency,
        timestamp: new Date(timestamp).toISOString()
      });
      onSuccess();
    } catch (err: any) {
      setError(err.message || 'Failed to add transaction');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose} style={{ zIndex: 1001 }}>
      <div className="modal-content glass-panel" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>New Transaction: {asset.symbol}</h2>
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
            <label htmlFor="price">
              {type === TransactionType.Dividend ? 'Dividend per Unit/Share' : 'Price Per Unit'}
            </label>
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
            <label htmlFor="fee">Phí giao dịch</label>
            <NumericFormat
              id="fee"
              value={fee}
              onValueChange={(values) => setFee(values.value)}
              className="glass-input"
              thousandSeparator="."
              decimalSeparator=","
              allowNegative={false}
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="notes">Ghi chú</label>
            <input id="notes" value={notes} onChange={e => setNotes(e.target.value)}
              className="glass-input" maxLength={500} disabled={loading} />
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

          {quantity && price && (
            <div className="glass-panel" style={{ padding: '0.75rem', marginBottom: '1rem' }}>
              Tác động tiền mặt: <strong>{new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
                style: 'currency', currency,
              }).format(calculateCashImpact(type, Number(quantity), Number(price), Number(fee)))}</strong>
            </div>
          )}

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className={`btn ${type === TransactionType.Buy ? 'btn-primary' : 'btn-outline'}`} disabled={loading}>
              {loading ? 'Processing...' : 'Confirm'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
