import React, { useState, useEffect } from 'react';
import { createAsset } from '../../assets/api/assetApi';
import { categoriesApi } from '../../admin/api/categories';
import { marketAssetsApi } from '../../admin/api/marketAssets';
import type { AssetCategory, MarketAsset } from '../../admin/types';
import '../../portfolios/components/CreatePortfolioModal.css';

interface CreateAssetModalProps {
  portfolioId: string;
  existingAssetIds?: string[];
  onClose: () => void;
  onSuccess: () => void;
}

export const CreateAssetModal: React.FC<CreateAssetModalProps> = ({ portfolioId, existingAssetIds = [], onClose, onSuccess }) => {
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [marketAssets, setMarketAssets] = useState<MarketAsset[]>([]);
  
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>('');
  const [selectedMarketAssetId, setSelectedMarketAssetId] = useState<string>('');
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadCategories();
  }, []);

  useEffect(() => {
    if (selectedCategoryId) {
      loadMarketAssets(selectedCategoryId);
      setSelectedMarketAssetId(''); // reset selection when category changes
    } else {
      setMarketAssets([]);
      setSelectedMarketAssetId('');
    }
  }, [selectedCategoryId]);

  const loadCategories = async () => {
    try {
      const res = await categoriesApi.getCategories();
      setCategories(res || []);
    } catch (err) {
      console.error(err);
    }
  };

  const loadMarketAssets = async (categoryId: string) => {
    try {
      const res = await marketAssetsApi.getMarketAssets(categoryId);
      if (res) {
        const filtered = res.filter(m => !existingAssetIds.includes(m.id));
        setMarketAssets(filtered);
      } else {
        setMarketAssets([]);
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedMarketAssetId) {
      setError('Vui lòng chọn Market Asset');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      await createAsset({ portfolioId, marketAssetId: selectedMarketAssetId });
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
            <label htmlFor="category">Category</label>
            <select
              id="category"
              value={selectedCategoryId}
              onChange={e => setSelectedCategoryId(e.target.value)}
              disabled={loading}
              className="glass-input glass-select"
            >
              <option value="">-- Chọn Category --</option>
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="marketAsset">Market Asset</label>
            <select
              id="marketAsset"
              value={selectedMarketAssetId}
              onChange={e => setSelectedMarketAssetId(e.target.value)}
              disabled={loading || !selectedCategoryId}
              className="glass-input glass-select"
            >
              <option value="">-- Chọn Market Asset --</option>
              {marketAssets.map(m => (
                <option key={m.id} value={m.id}>{m.symbol} - {m.name}</option>
              ))}
            </select>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading || !selectedMarketAssetId}>
              {loading ? 'Adding...' : 'Add Asset'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
