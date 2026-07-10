import { useEffect, useMemo, useState } from 'react';
import { categoriesApi } from '../api/categories';
import { marketAssetsApi } from '../api/marketAssets';
import { useNotification } from '../../../context/NotificationContext';
import type { AssetCategory, MarketAsset, PriceRefreshResult } from '../types';
import { MarketAssetModal } from './MarketAssetModal';
import './MarketAssetManagement.css';

const formatPrice = (value: number) =>
  new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 8 }).format(value);

const formatDateTime = (value: string) => {
  if (!value) return 'Never';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Never';
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
};

const getSourceTone = (source: string) => {
  const normalized = source.toLowerCase();
  if (normalized === 'dnse') return 'dnse';
  if (normalized === 'coingecko') return 'coingecko';
  return 'manual';
};

const getStatusTone = (status: string) => {
  const normalized = status.toLowerCase();
  if (normalized === 'fresh') return 'fresh';
  if (normalized === 'stale') return 'stale';
  if (normalized === 'error') return 'error';
  return 'manual';
};

export function MarketAssetManagement() {
  const { showNotification } = useNotification();
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [marketAssets, setMarketAssets] = useState<MarketAsset[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>('');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [assetToEdit, setAssetToEdit] = useState<MarketAsset | null>(null);
  const [isUpdatingAll, setIsUpdatingAll] = useState(false);
  const [refreshingAssetId, setRefreshingAssetId] = useState<string | null>(null);
  const [inlineErrors, setInlineErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    loadCategories();
  }, []);

  useEffect(() => {
    loadMarketAssets(selectedCategoryId || undefined, currentPage, pageSize);
  }, [selectedCategoryId, currentPage, pageSize]);

  const automaticAssetsCount = useMemo(
    () => marketAssets.filter(asset => asset.priceSource.toLowerCase() !== 'manual').length,
    [marketAssets]
  );

  const loadCategories = async () => {
    try {
      const categoriesRes = await categoriesApi.getCategories();
      setCategories(categoriesRes || []);
    } catch (err) {
      console.error('Failed to load categories', err);
    }
  };

  const loadMarketAssets = async (categoryId?: string, page = 1, size = 10) => {
    try {
      const response = await marketAssetsApi.getMarketAssets(categoryId, page, size);
      setMarketAssets(response.items || []);
      setTotalCount(response.totalCount || 0);
      setCurrentPage(response.page || 1);
      const serverErrors = (response.items || []).reduce<Record<string, string>>((acc, asset) => {
        if (asset.lastPriceError) acc[asset.id] = asset.lastPriceError;
        return acc;
      }, {});
      setInlineErrors(serverErrors);
    } catch (error) {
      console.error('Failed to load market assets', error);
      showNotification('Failed to load market assets', 'error');
    }
  };

  const handleFilterChange = (categoryId: string) => {
    setSelectedCategoryId(categoryId);
    setCurrentPage(1);
  };

  const handleOpenAddModal = () => {
    setAssetToEdit(null);
    setIsModalOpen(true);
  };

  const handleEditMarketAsset = (asset: MarketAsset) => {
    setAssetToEdit(asset);
    setIsModalOpen(true);
  };

  const handleDeleteMarketAsset = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this Asset?')) return;
    try {
      await marketAssetsApi.deleteMarketAsset(id);
      showNotification('Asset deleted', 'success');
      loadMarketAssets(selectedCategoryId || undefined, currentPage, pageSize);
    } catch (error) {
      console.error('Failed to delete asset', error);
      showNotification('Cannot delete this Asset because it is currently used in a Portfolio!', 'error');
    }
  };

  const applyRefreshResults = (results: PriceRefreshResult[]) => {
    const nextErrors = { ...inlineErrors };
    for (const result of results) {
      if (result.error) nextErrors[result.marketAssetId] = result.error;
      else delete nextErrors[result.marketAssetId];
    }
    setInlineErrors(nextErrors);
    return results.filter(result => !result.error).length;
  };

  const handleRefreshAsset = async (asset: MarketAsset) => {
    if (asset.priceSource.toLowerCase() === 'manual') return;
    setRefreshingAssetId(asset.id);
    try {
      const results = await marketAssetsApi.refreshPrice(asset.id);
      const successCount = applyRefreshResults(results);
      showNotification(successCount > 0 ? `Refreshed ${asset.symbol}` : `Cannot refresh ${asset.symbol}`, successCount > 0 ? 'success' : 'error');
      await loadMarketAssets(selectedCategoryId || undefined, currentPage, pageSize);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Refresh failed';
      setInlineErrors(prev => ({ ...prev, [asset.id]: message }));
      showNotification(message, 'error');
    } finally {
      setRefreshingAssetId(null);
    }
  };

  const handleUpdateAll = async () => {
    setIsUpdatingAll(true);
    try {
      const results = await marketAssetsApi.refreshPrices();
      const successCount = applyRefreshResults(results);
      const failedCount = results.length - successCount;
      showNotification(`Refresh complete: ${successCount} success, ${failedCount} failed.`, failedCount > 0 ? 'info' : 'success');
      await loadMarketAssets(selectedCategoryId || undefined, currentPage, pageSize);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to refresh prices.';
      console.error('Failed to refresh all assets', error);
      showNotification(message, 'error');
    } finally {
      setIsUpdatingAll(false);
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="admin-page-container">
      <div className="market-assets-header">
        <div>
          <span className="admin-kicker">Market data operations</span>
          <h2>Market Assets</h2>
          <p className="admin-page-subtitle">Manage asset metadata, pricing source, refresh status, and provider errors.</p>
        </div>
        <div className="market-assets-actions">
          <button className="btn-outline market-refresh-all" onClick={handleUpdateAll} disabled={isUpdatingAll || automaticAssetsCount === 0}>
            {isUpdatingAll ? 'Refreshing...' : `Refresh auto prices (${automaticAssetsCount})`}
          </button>
          <button className="btn-primary glow-effect" onClick={handleOpenAddModal}>
            Add Market Asset
          </button>
        </div>
      </div>

      <div className="modern-tabs">
        <button className={`tab-btn ${!selectedCategoryId ? 'active' : ''}`} onClick={() => handleFilterChange('')}>
          All Assets
        </button>
        {categories.map(category => (
          <button
            key={category.id}
            className={`tab-btn ${selectedCategoryId === category.id ? 'active' : ''}`}
            onClick={() => handleFilterChange(category.id)}
          >
            {category.name}
          </button>
        ))}
      </div>

      <div className="market-assets-table-panel">
        <div className="table-responsive">
          <table className="modern-data-table market-assets-table">
            <thead>
              <tr>
                <th>Symbol</th>
                <th>Name</th>
                <th>Category</th>
                <th>Price Source</th>
                <th className="text-right">Current Price</th>
                <th>Last Updated</th>
                <th>Status</th>
                <th className="text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              {marketAssets.map(asset => {
                const error = inlineErrors[asset.id];
                return (
                  <tr key={asset.id} className={error ? 'has-inline-error' : ''}>
                    <td className="symbol-cell">
                      <span className="symbol-text">{asset.symbol}</span>
                    </td>
                    <td className="name-cell">
                      <strong>{asset.name}</strong>
                      {asset.externalId && <small>{asset.externalId}</small>}
                    </td>
                    <td>
                      <span className="badge category-badge">{asset.categoryName}</span>
                    </td>
                    <td>
                      <span className={`source-badge ${getSourceTone(asset.priceSource)}`}>{asset.priceSource}</span>
                    </td>
                    <td className="text-right price-cell">{formatPrice(asset.currentPrice)}</td>
                    <td className="updated-cell">{formatDateTime(asset.lastUpdated)}</td>
                    <td>
                      <span className={`status-badge ${getStatusTone(asset.priceStatus)}`}>{asset.priceStatus}</span>
                      {error && <div className="inline-price-error">{error}</div>}
                    </td>
                    <td className="text-center actions-cell">
                      <div className="row-actions">
                        <button
                          onClick={() => handleRefreshAsset(asset)}
                          className="icon-btn refresh-btn"
                          title="Refresh price"
                          disabled={asset.priceSource.toLowerCase() === 'manual' || refreshingAssetId === asset.id}
                        >
                          {refreshingAssetId === asset.id ? '...' : 'Refresh'}
                        </button>
                        <button onClick={() => handleEditMarketAsset(asset)} className="icon-btn edit-btn" title="Edit">
                          Edit
                        </button>
                        <button onClick={() => handleDeleteMarketAsset(asset.id)} className="icon-btn delete-btn" title="Delete">
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}

              {marketAssets.length === 0 && (
                <tr>
                  <td colSpan={8}>
                    <div className="empty-state market-empty">
                      <p>No market assets found for this filter.</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {totalCount > 0 && (
          <div className="pagination-bar">
            <div className="pagination-info">
              Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, totalCount)} of {totalCount} entries
            </div>

            <div className="pagination-controls">
              <select
                value={pageSize}
                onChange={event => {
                  setPageSize(Number(event.target.value));
                  setCurrentPage(1);
                }}
                className="modern-select pagination-select"
              >
                <option value={10}>10 / page</option>
                <option value={20}>20 / page</option>
                <option value={50}>50 / page</option>
              </select>

              <div className="pagination-buttons">
                <button className="page-btn nav-btn" disabled={currentPage === 1} onClick={() => setCurrentPage(page => Math.max(1, page - 1))}>
                  Prev
                </button>
                <span className="page-current">{currentPage}</span>
                <button className="page-btn nav-btn" disabled={currentPage >= totalPages} onClick={() => setCurrentPage(page => Math.min(totalPages, page + 1))}>
                  Next
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      <MarketAssetModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSaved={() => loadMarketAssets(selectedCategoryId || undefined, currentPage, pageSize)}
        assetToEdit={assetToEdit}
        categories={categories}
        defaultCategoryId={selectedCategoryId}
      />
    </div>
  );
}
