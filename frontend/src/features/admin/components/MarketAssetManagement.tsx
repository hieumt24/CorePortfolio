import { useState, useEffect } from 'react';
import { categoriesApi } from '../api/categories';
import { marketAssetsApi } from '../api/marketAssets';
import { useNotification } from '../../../context/NotificationContext';
import type { AssetCategory, MarketAsset } from '../types';
import { MarketAssetModal } from './MarketAssetModal';

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
    if (!window.confirm('Bạn có chắc chắn muốn xóa Asset này?')) return;
    try {
      await marketAssetsApi.deleteMarketAsset(id);
      showNotification('Xóa Asset thành công', 'success');
      loadMarketAssets(selectedCategoryId || undefined, currentPage, pageSize);
    } catch (error: any) {
      console.error('Failed to delete asset', error);
      showNotification('Không thể xóa Asset này vì nó đã được sử dụng trong Portfolio của người dùng!', 'error');
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="admin-card" style={{ gridColumn: '1 / -1' }}>
      <div className="admin-card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h2>Market Asset Management</h2>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginTop: '0.25rem' }}>View, add, edit, and delete market assets.</p>
        </div>
        <button className="admin-btn" style={{ background: 'var(--primary)' }} onClick={handleOpenAddModal}>
          + Add Market Asset
        </button>
      </div>

      <div className="admin-tabs">
        <button
          className={`admin-tab-btn ${!selectedCategoryId ? 'active' : ''}`}
          onClick={() => handleFilterChange('')}
        >
          All Assets
        </button>
        {categories.map(c => (
          <button
            key={c.id}
            className={`admin-tab-btn ${selectedCategoryId === c.id ? 'active' : ''}`}
            onClick={() => handleFilterChange(c.id)}
          >
            {c.name}
          </button>
        ))}
      </div>

      <div className="admin-card-body" style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {/* List Section */}
        <div className="admin-list-container">
          {marketAssets.map(m => (
            <div key={m.id} className="admin-list-item" style={{ flexDirection: 'column', alignItems: 'flex-start', gap: '0.5rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%', alignItems: 'center' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                  <span className="item-title">{m.symbol}</span>
                  <span className="admin-badge admin-badge-gray">{m.categoryName}</span>
                </div>
                <span className="item-price">{m.currentPrice.toLocaleString()}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%', alignItems: 'center' }}>
                <span className="item-subtitle">{m.name}</span>
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button onClick={() => handleEditMarketAsset(m)} className="action-btn edit-btn">Edit</button>
                  <button onClick={() => handleDeleteMarketAsset(m.id)} className="action-btn delete-btn">Delete</button>
                </div>
              </div>
            </div>
          ))}
          
          {marketAssets.length === 0 && (
            <div className="admin-empty-state">
              No market assets found.
            </div>
          )}
        </div>

        {/* Pagination Controls */}
        {totalCount > 0 && (
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1rem', padding: '1rem', background: 'rgba(0,0,0,0.1)', borderRadius: '8px' }}>
            <div style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
              Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, totalCount)} of {totalCount} entries
            </div>
            
            <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
              <select 
                value={pageSize} 
                onChange={(e) => {
                  setPageSize(Number(e.target.value));
                  setCurrentPage(1);
                }}
                className="admin-input admin-select"
                style={{ padding: '0.25rem 0.5rem', minWidth: '80px' }}
              >
                <option value={10}>10 / page</option>
                <option value={20}>20 / page</option>
                <option value={50}>50 / page</option>
              </select>

              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button 
                  className="admin-btn" 
                  style={{ background: 'var(--surface)', border: '1px solid var(--border)', padding: '0.25rem 0.75rem' }}
                  disabled={currentPage === 1}
                  onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                >
                  Prev
                </button>
                <span style={{ padding: '0.25rem 0.5rem', background: 'var(--primary)', color: 'white', borderRadius: '4px', minWidth: '32px', textAlign: 'center' }}>
                  {currentPage}
                </span>
                <button 
                  className="admin-btn" 
                  style={{ background: 'var(--surface)', border: '1px solid var(--border)', padding: '0.25rem 0.75rem' }}
                  disabled={currentPage >= totalPages}
                  onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                >
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
