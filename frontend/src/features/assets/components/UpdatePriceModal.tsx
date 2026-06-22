import React, { useState } from 'react';
import { updateAssetPrice } from '../api/assetApi';
import type { AssetSummaryDto } from '../../portfolios/types';
import { NumericFormat } from 'react-number-format';
import '../../portfolios/components/CreatePortfolioModal.css'; // Reuse styles

interface UpdatePriceModalProps {
  asset: AssetSummaryDto;
  onClose: () => void;
  onSuccess: () => void;
}

export const UpdatePriceModal: React.FC<UpdatePriceModalProps> = ({ asset, onClose, onSuccess }) => {
  const [price, setPrice] = useState(asset.currentPrice.toString());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!price) {
      setError('Price is required');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      await updateAssetPrice(asset.marketAssetId, Number(price));
      onSuccess();
    } catch (err: any) {
      setError(err.message || 'Failed to update price');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose} style={{ zIndex: 1002 }}>
      <div className="modal-content glass-panel" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Update Price: {asset.symbol}</h2>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>
        
        {error && <div className="error-alert">{error}</div>}

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label htmlFor="price">Current Market Price ({asset.currency || 'USD'})</label>
            <NumericFormat
              id="price"
              value={price}
              onValueChange={(values) => setPrice(values.value)}
              placeholder="e.g. 150.00"
              className="glass-input"
              disabled={loading}
              thousandSeparator="."
              decimalSeparator=","
              allowNegative={false}
              autoFocus
            />
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Processing...' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
