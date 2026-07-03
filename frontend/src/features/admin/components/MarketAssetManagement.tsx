import { useState, useEffect } from 'react';
import { categoriesApi } from '../api/categories';
import { marketAssetsApi } from '../api/marketAssets';
import { useNotification } from '../../../context/NotificationContext';
import type { AssetCategory, MarketAsset } from '../types';
import { MarketAssetModal } from './MarketAssetModal';
import './MarketAssetManagement.css';

export function MarketAssetManagement() {
  const { showNotification } = useNotification();
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [marketAssets, setMarketAssets] = useState<MarketAsset[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>('');
  
  // Pagination State
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [assetToEdit, setAssetToEdit] = useState<MarketAsset | null>(null);

  // Update All State
  const [isUpdatingAll, setIsUpdatingAll] = useState(false);
  const [updateProgress, setUpdateProgress] = useState({ current: 0, total: 0 });

  useEffect(() => {
    loadCategories();
  }, []);

  useEffect(() => {
    loadMarketAssets(selectedCategoryId || undefined, currentPage, pageSize);
  }, [selectedCategoryId, currentPage, pageSize]);

  const loadCategories = async () => {
    try {
      const categoriesRes = await categoriesApi.getCategories();
      setCategories(categoriesRes || []);
      
      if (categoriesRes && categoriesRes.length > 0 && !selectedCategoryId) {
        setSelectedCategoryId(''); // Default to All
      }
    } catch (err: any) {
      console.error('Failed to load categories', err);
    }
  };

  const loadMarketAssets = async (categoryId?: string, page = 1, size = 10) => {
    try {
      const response = await marketAssetsApi.getMarketAssets(categoryId, page, size);
      if (response) {
        setMarketAssets(response.items || []);
        setTotalCount(response.totalCount || 0);
        setCurrentPage(response.page || 1);
      }
    } catch (error) {
      console.error('Failed to load market assets', error);
      showNotification('Failed to load market assets', 'error');
    }
  };

  const handleFilterChange = (categoryId: string) => {
    setSelectedCategoryId(categoryId);
    setCurrentPage(1); // Reset to first page when changing category
  };

  const handleOpenAddModal = () => {
    setAssetToEdit(null);
    setIsModalOpen(true);
  };

  const handleEditMarketAsset = (m: MarketAsset) => {
    setAssetToEdit(m);
    setIsModalOpen(true);
  };

  const handleDeleteMarketAsset = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this Asset?')) return;
    try {
      await marketAssetsApi.deleteMarketAsset(id);
      showNotification('Asset deleted', 'success');
      loadMarketAssets(selectedCategoryId || undefined, currentPage, pageSize);
    } catch (error: any) {
      console.error('Failed to delete asset', error);
      showNotification('Cannot delete this Asset because it is currently used in a Portfolio!', 'error');
    }
  };

  const isStockCategory = () => {
    const cat = categories.find(c => c.id === selectedCategoryId);
    if (!cat) return false;
    const name = cat.name.toLowerCase();
    return name.includes('stock') || name.includes('cổ phiếu') || name.includes('chứng khoán');
  };

  const handleUpdateAll = async () => {
    if (!selectedCategoryId) return;
    
    setIsUpdatingAll(true);
    try {
      const response = await marketAssetsApi.getMarketAssets(selectedCategoryId, 1, 1000);
      const assetsToUpdate = response.items || [];
      
      if (assetsToUpdate.length === 0) {
        showNotification('Không có tài sản nào để cập nhật.', 'info');
        setIsUpdatingAll(false);
        return;
      }

      setUpdateProgress({ current: 0, total: assetsToUpdate.length });

      let successCount = 0;
      for (let i = 0; i < assetsToUpdate.length; i++) {
        const asset = assetsToUpdate[i];
        try {
          const priceData = await marketAssetsApi.fetchDnsePrice(asset.symbol);
          if (priceData && priceData.price) {
            await marketAssetsApi.updateMarketAsset(asset.id, {
              categoryId: asset.categoryId,
              symbol: asset.symbol,
              name: asset.name,
              currentPrice: priceData.price
            });
            successCount++;
          }
        } catch (err) {
          console.error(`Failed to update price for ${asset.symbol}`, err);
        }
        setUpdateProgress(prev => ({ ...prev, current: i + 1 }));
      }
      
      showNotification(`Cập nhật thành công ${successCount}/${assetsToUpdate.length} tài sản.`, 'success');
      loadMarketAssets(selectedCategoryId || undefined, currentPage, pageSize);
    } catch (error) {
      console.error('Failed to update all assets', error);
      showNotification('Đã xảy ra lỗi khi cập nhật tất cả.', 'error');
    } finally {
      setIsUpdatingAll(false);
      setUpdateProgress({ current: 0, total: 0 });
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="admin-page-container">
      <div className="admin-page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h2>Market Assets</h2>
          <p className="admin-page-subtitle">View, add, edit, and delete global market assets.</p>
        </div>
        <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
          {isStockCategory() && (
            <button 
              className="btn-outline glow-effect" 
              onClick={handleUpdateAll}
              disabled={isUpdatingAll}
              style={{ border: '1px solid rgba(59, 130, 246, 0.5)', color: '#3b82f6', background: 'rgba(59, 130, 246, 0.1)', cursor: isUpdatingAll ? 'wait' : 'pointer' }}
            >
              {isUpdatingAll ? `⏳ Updating ${updateProgress.current}/${updateProgress.total}...` : '⚡ Update All Prices'}
            </button>
          )}
          <button className="btn-primary glow-effect" onClick={handleOpenAddModal}>
            ✨ Add Market Asset
          </button>
        </div>
      </div>

      <div className="modern-tabs">
        <button
          className={`tab-btn ${!selectedCategoryId ? 'active' : ''}`}
          onClick={() => handleFilterChange('')}
        >
          All Assets
        </button>
        {categories.map(c => (
          <button
            key={c.id}
            className={`tab-btn ${selectedCategoryId === c.id ? 'active' : ''}`}
            onClick={() => handleFilterChange(c.id)}
          >
            {c.name}
          </button>
        ))}
      </div>

      <div className="glass-panel" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-responsive">
          <table className="modern-data-table">
            <thead>
              <tr>
                <th>Symbol</th>
                <th>Asset Name</th>
                <th>Category</th>
                <th className="text-right">Price</th>
                <th className="text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              {marketAssets.map(m => (
                <tr key={m.id}>
                  <td className="symbol-cell">
                    <span className="symbol-text">{m.symbol}</span>
                  </td>
                  <td className="name-cell">{m.name}</td>
                  <td>
                    <span className="badge category-badge">{m.categoryName}</span>
                  </td>
                  <td className="text-right price-cell">
                    {m.currentPrice.toLocaleString(undefined, { maximumFractionDigits: 8 })}
                  </td>
                  <td className="text-center actions-cell">
                    <div className="row-actions">
                      <button onClick={() => handleEditMarketAsset(m)} className="icon-btn edit-btn" title="Edit">
                        ✏️
                      </button>
                      <button onClick={() => handleDeleteMarketAsset(m.id)} className="icon-btn delete-btn" title="Delete">
                        🗑️
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              
              {marketAssets.length === 0 && (
                <tr>
                  <td colSpan={5}>
                    <div className="empty-state" style={{ padding: '3rem 0', border: 'none' }}>
                      <div className="empty-icon">📈</div>
                      <p>No market assets found for this filter.</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination Controls */}
        {totalCount > 0 && (
          <div className="pagination-bar">
            <div className="pagination-info">
              Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, totalCount)} of {totalCount} entries
            </div>
            
            <div className="pagination-controls">
              <select 
                value={pageSize} 
                onChange={(e) => {
                  setPageSize(Number(e.target.value));
                  setCurrentPage(1);
                }}
                className="modern-select pagination-select"
              >
                <option value={10}>10 / page</option>
                <option value={20}>20 / page</option>
                <option value={50}>50 / page</option>
              </select>

              <div className="pagination-buttons">
                <button 
                  className="page-btn nav-btn" 
                  disabled={currentPage === 1}
                  onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                >
                  « Prev
                </button>
                <span className="page-current">
                  {currentPage}
                </span>
                <button 
                  className="page-btn nav-btn" 
                  disabled={currentPage >= totalPages}
                  onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                >
                  Next »
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
