import React, { useState, useEffect } from 'react';
import { watchlistApi } from '../api/watchlistApi';
import { categoriesApi } from '../../admin/api/categories';
import { marketAssetsApi } from '../../admin/api/marketAssets';
import type { AssetCategory, MarketAsset } from '../../admin/types';
import './AddWatchlistModal.css';

interface AddWatchlistModalProps {
  onClose: () => void;
  onSuccess: () => void;
}

export const AddWatchlistModal: React.FC<AddWatchlistModalProps> = ({ onClose, onSuccess }) => {
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [marketAssets, setMarketAssets] = useState<MarketAsset[]>([]);
  
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>('');
  const [selectedMarketAssetId, setSelectedMarketAssetId] = useState<string>('');
  const [search, setSearch] = useState('');
  const [targetPrice, setTargetPrice] = useState('');
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    categoriesApi.getCategories()
      .then(result => {
        if (active) setCategories(result || []);
      })
      .catch(() => {
        if (active) setError('Không thể tải danh mục tài sản.');
      });
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!selectedCategoryId) return;
    let active = true;
    marketAssetsApi.getMarketAssets(selectedCategoryId, 1, 1000)
      .then(result => {
        if (active) setMarketAssets(result?.items || []);
      })
      .catch(() => {
        if (active) setError('Không thể tải tài sản trong danh mục.');
      });
    return () => {
      active = false;
    };
  }, [selectedCategoryId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedMarketAssetId) {
      setError('Vui lòng chọn Market Asset');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      await watchlistApi.addToWatchlist({
        marketAssetId: selectedMarketAssetId,
        targetPrice: targetPrice ? Number(targetPrice) : undefined,
      });
      onSuccess();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể thêm vào danh sách theo dõi');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="watchlist-modal glass-panel" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <span className="watchlist-modal-kicker">RADAR TÀI SẢN</span>
            <h2>Thêm vào danh sách theo dõi</h2>
          </div>
          <button className="close-btn" onClick={onClose} aria-label="Đóng">&times;</button>
        </div>
        
        {error && <div className="error-alert">{error}</div>}

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label htmlFor="category">Danh mục</label>
            <select
              id="category"
              value={selectedCategoryId}
              onChange={e => {
                setSelectedCategoryId(e.target.value);
                setSelectedMarketAssetId('');
                setMarketAssets([]);
                setSearch('');
              }}
              disabled={loading}
              className="glass-input glass-select"
            >
              <option value="">Chọn danh mục</option>
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="watchlistSearch">Tìm tài sản</label>
            <input
              id="watchlistSearch"
              type="search"
              value={search}
              onChange={event => setSearch(event.target.value)}
              placeholder="Nhập mã hoặc tên tài sản"
              disabled={loading || !selectedCategoryId}
            />
          </div>

          <div className="form-group">
            <label htmlFor="marketAsset">Tài sản</label>
            <select
              id="marketAsset"
              value={selectedMarketAssetId}
              onChange={e => setSelectedMarketAssetId(e.target.value)}
              disabled={loading || !selectedCategoryId}
              className="glass-input glass-select"
            >
              <option value="">Chọn tài sản</option>
              {marketAssets.filter(asset => {
                const query = search.trim().toLocaleLowerCase('vi-VN');
                return !query
                  || asset.symbol.toLocaleLowerCase('vi-VN').includes(query)
                  || asset.name.toLocaleLowerCase('vi-VN').includes(query);
              }).map(m => (
                <option key={m.id} value={m.id}>{m.symbol} - {m.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="targetPrice">Giá mục tiêu <span className="optional-label">Không bắt buộc</span></label>
            <input
              id="targetPrice"
              type="number"
              min="0"
              step="any"
              value={targetPrice}
              onChange={event => setTargetPrice(event.target.value)}
              placeholder="Nhập mức giá bạn muốn theo dõi"
              disabled={loading}
            />
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Hủy
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading || !selectedMarketAssetId}>
              {loading ? 'Đang thêm...' : 'Thêm vào theo dõi'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
