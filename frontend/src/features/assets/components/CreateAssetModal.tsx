import React, { useState } from 'react';
import { createAsset } from '../../assets/api/assetApi';
import { AssetType } from '../../assets/types';
import '../../portfolios/components/CreatePortfolioModal.css'; // Reuse Modal CSS

interface CreateAssetModalProps {
  portfolioId: string;
  onClose: () => void;
  onSuccess: () => void;
}

export const CreateAssetModal: React.FC<CreateAssetModalProps> = ({ portfolioId, onClose, onSuccess }) => {
  const [symbol, setSymbol] = useState('');
  const [name, setName] = useState('');
  const [type, setType] = useState<AssetType>(AssetType.Stock);
  const [currency, setCurrency] = useState('VND');

  React.useEffect(() => {
    if (type === AssetType.Crypto || type === AssetType.MutualFund) {
      setCurrency('USD');
    } else {
      setCurrency('VND');
    }
  }, [type]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!symbol.trim() || !name.trim()) {
      setError('Symbol and Name are required');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      await createAsset({ portfolioId, symbol: symbol.toUpperCase(), name, type: Number(type) as AssetType, currency });
      onSuccess();
    } catch (err: any) {
      setError(err.message || 'Failed to add asset');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content glass-panel" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Add New Asset</h2>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>
        
        {error && <div className="error-alert">{error}</div>}

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label htmlFor="symbol">Symbol (Ticker/Coin)</label>
            <input
              id="symbol"
              type="text"
              value={symbol}
              onChange={e => setSymbol(e.target.value)}
              placeholder="e.g. AAPL, BTC, HPG"
              className="glass-input"
              disabled={loading}
              autoFocus
            />
          </div>

          <div className="form-group">
            <label htmlFor="name">Asset Name</label>
            <input
              id="name"
              type="text"
              value={name}
              onChange={e => setName(e.target.value)}
              placeholder="e.g. Apple Inc, Bitcoin, Hòa Phát"
              className="glass-input"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="type">Asset Type</label>
            <select
              id="type"
              value={type}
              onChange={e => setType(Number(e.target.value) as AssetType)}
              disabled={loading}
              className="glass-input glass-select"
            >
              <option value={AssetType.Crypto}>Crypto</option>
              <option value={AssetType.Stock}>Stock</option>
              <option value={AssetType.MutualFund}>Mutual Fund</option>
              <option value={AssetType.Cash}>Cash</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="currency">Currency</label>
            <select
              id="currency"
              value={currency}
              onChange={e => setCurrency(e.target.value)}
              disabled={loading}
              className="glass-input glass-select"
            >
              <option value="VND">VND (₫)</option>
              <option value="USD">USD ($)</option>
            </select>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Adding...' : 'Add Asset'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
