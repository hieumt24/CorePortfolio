import React, { useEffect, useState } from 'react';
import { createAsset, searchAvailableMarketAssets } from '../api/assetApi';
import { categoriesApi } from '../../admin/api/categories';
import type { AssetCategory } from '../../admin/types';
import type { AvailableMarketAsset } from '../types';
import '../../portfolios/components/CreatePortfolioModal.css';

interface CreateAssetModalProps {
  portfolioId: string;
  onClose: () => void;
  onSuccess: () => void;
}

const formatPrice = (value: number, currency: string) =>
  new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 4,
  }).format(value);

export const CreateAssetModal: React.FC<CreateAssetModalProps> = ({
  portfolioId,
  onClose,
  onSuccess,
}) => {
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [categoryId, setCategoryId] = useState('');
  const [search, setSearch] = useState('');
  const [results, setResults] = useState<AvailableMarketAsset[]>([]);
  const [selectedAsset, setSelectedAsset] = useState<AvailableMarketAsset | null>(null);
  const [isSearching, setIsSearching] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [retryNonce, setRetryNonce] = useState(0);
  const [searchError, setSearchError] = useState('');
  const [submitError, setSubmitError] = useState('');

  useEffect(() => {
    categoriesApi.getCategories()
      .then(data => setCategories(data || []))
      .catch(error => {
        console.error('Failed to load asset categories', error);
        setSearchError('Không thể tải danh mục tài sản.');
      });
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(async () => {
      setIsSearching(true);
      setSearchError('');
      try {
        const data = await searchAvailableMarketAssets(portfolioId, {
          search: search.trim() || undefined,
          categoryId: categoryId || undefined,
          limit: 20,
        });
        setResults(data || []);
      } catch (error) {
        setResults([]);
        setSearchError(error instanceof Error ? error.message : 'Không thể tìm Market Asset.');
      } finally {
        setIsSearching(false);
      }
    }, 250);

    return () => window.clearTimeout(timer);
  }, [portfolioId, search, categoryId, retryNonce]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedAsset) {
      setSubmitError('Vui lòng chọn một Market Asset.');
      return;
    }

    setIsSubmitting(true);
    setSubmitError('');
    try {
      await createAsset({ portfolioId, marketAssetId: selectedAsset.id });
      onSuccess();
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : 'Không thể thêm asset.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSearchChange = (value: string) => {
    setSearch(value);
    setSelectedAsset(null);
    setSubmitError('');
  };

  const handleCategoryChange = (value: string) => {
    setCategoryId(value);
    setSelectedAsset(null);
    setSubmitError('');
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content glass-panel asset-picker-modal" onClick={event => event.stopPropagation()}>
        <div className="modal-header">
          <div>
            <span className="asset-picker-kicker">Portfolio asset</span>
            <h2>Thêm tài sản</h2>
          </div>
          <button type="button" className="close-btn" onClick={onClose} aria-label="Đóng">&times;</button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="asset-picker-controls">
            <label className="form-group">
              <span>Danh mục</span>
              <select
                value={categoryId}
                onChange={event => handleCategoryChange(event.target.value)}
                disabled={isSubmitting}
                className="glass-input glass-select"
              >
                <option value="">Tất cả danh mục</option>
                {categories.map(category => (
                  <option key={category.id} value={category.id}>{category.name}</option>
                ))}
              </select>
            </label>

            <label className="form-group asset-picker-search">
              <span>Tìm theo mã hoặc tên</span>
              <input
                type="search"
                value={search}
                onChange={event => handleSearchChange(event.target.value)}
                disabled={isSubmitting}
                className="glass-input"
                placeholder="Ví dụ: HPG hoặc Hòa Phát"
                autoFocus
              />
            </label>
          </div>

          <div className="asset-picker-results" aria-live="polite">
            {isSearching && <div className="asset-picker-state">Đang tìm Market Asset…</div>}

            {!isSearching && searchError && (
              <div className="asset-picker-state error">
                <span>{searchError}</span>
                <button type="button" onClick={() => setRetryNonce(value => value + 1)}>Thử lại</button>
              </div>
            )}

            {!isSearching && !searchError && results.length === 0 && (
              <div className="asset-picker-state">
                Không tìm thấy asset phù hợp hoặc asset đã có trong portfolio.
              </div>
            )}

            {!isSearching && !searchError && results.map(asset => (
              <button
                type="button"
                key={asset.id}
                className={`asset-picker-option ${selectedAsset?.id === asset.id ? 'selected' : ''}`}
                onClick={() => {
                  setSelectedAsset(asset);
                  setSubmitError('');
                }}
              >
                <span className="asset-picker-symbol">{asset.symbol}</span>
                <span className="asset-picker-name">
                  <strong>{asset.name}</strong>
                  <small>{asset.categoryName} · {asset.priceSource} · {asset.priceStatus}</small>
                </span>
                <span className="asset-picker-price">{formatPrice(asset.currentPrice, asset.currency)}</span>
              </button>
            ))}
          </div>

          {selectedAsset && (
            <div className="asset-picker-selected">
              <span>Đã chọn</span>
              <strong>{selectedAsset.symbol} · {selectedAsset.name}</strong>
            </div>
          )}

          {submitError && <div className="modal-error">{submitError}</div>}

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={isSubmitting}>
              Hủy
            </button>
            <button type="submit" className="btn btn-primary" disabled={isSubmitting || !selectedAsset}>
              {isSubmitting ? 'Đang thêm…' : 'Thêm vào portfolio'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
