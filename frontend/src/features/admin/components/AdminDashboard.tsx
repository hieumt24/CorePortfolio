import { useState, useEffect } from 'react';
import { categoriesApi } from '../api/categories';
import { marketAssetsApi } from '../api/marketAssets';
import { settingsApi } from '../api/settingsApi';
import { useNotification } from '../../../context/NotificationContext';
import type { AssetCategory, MarketAsset } from '../types';
import './AdminDashboard.css';

export function AdminDashboard() {
  const { showNotification } = useNotification();
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [marketAssets, setMarketAssets] = useState<MarketAsset[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>('');
  
  // Settings State
  const [usdToVndRate, setUsdToVndRate] = useState<string>('');
  const [isUpdatingRate, setIsUpdatingRate] = useState<boolean>(false);

  // Category Form State
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null);
  const [newCatName, setNewCatName] = useState('');
  const [newCatCurrency, setNewCatCurrency] = useState('VND');

  // Market Asset Form State
  const [editingMarketAssetId, setEditingMarketAssetId] = useState<string | null>(null);
  const [newAssetSymbol, setNewAssetSymbol] = useState('');
  const [newAssetName, setNewAssetName] = useState('');
  const [newAssetPrice, setNewAssetPrice] = useState('');
  const [newAssetCategoryId, setNewAssetCategoryId] = useState('');

  useEffect(() => {
    loadCategories();
    loadMarketAssets();
    loadSettings();
  }, []);

  const loadSettings = async () => {
    const rate = await settingsApi.getSetting('USD_TO_VND');
    if (rate) setUsdToVndRate(rate);
  };

  const handleUpdateRate = async () => {
    if (!usdToVndRate) return;
    setIsUpdatingRate(true);
    const success = await settingsApi.updateSetting('USD_TO_VND', usdToVndRate);
    setIsUpdatingRate(false);
    if (success) {
      showNotification('Cập nhật tỷ giá thành công!', 'success');
    } else {
      showNotification('Có lỗi xảy ra khi cập nhật tỷ giá.', 'error');
    }
  };

  const loadCategories = async () => {
    try {
      const categoriesRes = await categoriesApi.getCategories();
      setCategories(categoriesRes || []);
      
      if (categoriesRes && categoriesRes.length > 0 && !selectedCategoryId) {
        setSelectedCategoryId(categoriesRes[0].id);
      }
    } catch (err: any) {
      console.error('Failed to load categories', err);
    }
  };

  const loadMarketAssets = async (categoryId?: string) => {
    try {
      const marketAssetsRes = await marketAssetsApi.getMarketAssets(categoryId);
      setMarketAssets(marketAssetsRes || []);
    } catch (error) {
      console.error('Failed to load market assets', error);
    }
  };

  // ---- CATEGORY HANDLERS ----
  const handleSaveCategory = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingCategoryId) {
        await categoriesApi.updateCategory(editingCategoryId, { name: newCatName, defaultCurrency: newCatCurrency });
      } else {
        await categoriesApi.createCategory({ name: newCatName, defaultCurrency: newCatCurrency });
      }
      resetCategoryForm();
      loadCategories();
      showNotification('Đã lưu Category', 'success');
    } catch (error) {
      console.error('Failed to save category', error);
      showNotification('Đã xảy ra lỗi khi lưu Category', 'error');
    }
  };

  const handleEditCategory = (c: AssetCategory) => {
    setEditingCategoryId(c.id);
    setNewCatName(c.name);
    setNewCatCurrency(c.defaultCurrency);
  };

  const handleDeleteCategory = async (id: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa Category này?')) return;
    try {
      await categoriesApi.deleteCategory(id);
      showNotification('Xóa Category thành công', 'success');
      loadCategories();
    } catch (error: any) {
      console.error('Failed to delete category', error);
      showNotification('Không thể xóa Category này vì nó đang chứa dữ liệu Market Asset!', 'error');
    }
  };

  const resetCategoryForm = () => {
    setEditingCategoryId(null);
    setNewCatName('');
    setNewCatCurrency('VND');
  };

  // ---- MARKET ASSET HANDLERS ----
  const handleSaveMarketAsset = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newAssetCategoryId) {
      showNotification('Vui lòng chọn Category!', 'info');
      return;
    }
    try {
      const payload = {
        categoryId: newAssetCategoryId,
        symbol: newAssetSymbol,
        name: newAssetName,
        currentPrice: Number(newAssetPrice)
      };

      if (editingMarketAssetId) {
        await marketAssetsApi.updateMarketAsset(editingMarketAssetId, payload);
      } else {
        await marketAssetsApi.createMarketAsset(payload);
      }
      resetMarketAssetForm();
      loadMarketAssets(selectedCategoryId || undefined);
      showNotification('Đã lưu Market Asset', 'success');
    } catch (error) {
      console.error('Failed to save market asset', error);
      showNotification('Đã xảy ra lỗi khi lưu Market Asset', 'error');
    }
  };

  const handleEditMarketAsset = (m: MarketAsset) => {
    setEditingMarketAssetId(m.id);
    setNewAssetCategoryId(m.categoryId);
    setNewAssetSymbol(m.symbol);
    setNewAssetName(m.name);
    setNewAssetPrice(m.currentPrice.toString());
  };

  const handleDeleteMarketAsset = async (id: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa Asset này?')) return;
    try {
      await marketAssetsApi.deleteMarketAsset(id);
      showNotification('Xóa Asset thành công', 'success');
      loadMarketAssets(selectedCategoryId || undefined);
    } catch (error: any) {
      console.error('Failed to delete asset', error);
      showNotification('Không thể xóa Asset này vì nó đã được sử dụng trong Portfolio của người dùng!', 'error');
    }
  };

  const resetMarketAssetForm = () => {
    setEditingMarketAssetId(null);
    setNewAssetSymbol('');
    setNewAssetName('');
    setNewAssetPrice('');
    // Giữ nguyên CategoryId đã chọn cho tiện thêm nhiều mã
  };

  const handleFilterChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const val = e.target.value;
    setSelectedCategoryId(val);
    loadMarketAssets(val || undefined);
  };

  return (
    <div className="admin-layout">
      <div className="admin-header">
        <h1>Admin Control Panel</h1>
      </div>

      <div className="admin-container">
        <section className="admin-card settings-section">
          <div className="admin-card-header">
            <h2>System Settings</h2>
            <p>Configure global application settings.</p>
          </div>
          <div className="admin-card-body">
            <div className="admin-form-group">
              <label>Exchange Rate (USD to VND)</label>
              <div style={{ display: 'flex', gap: '1rem' }}>
                <input 
                  type="number" 
                  className="admin-input"
                  value={usdToVndRate} 
                  onChange={(e) => setUsdToVndRate(e.target.value)}
                  placeholder="e.g., 26309"
                  style={{ flex: 1 }}
                />
                <button 
                  className="admin-btn" 
                  onClick={handleUpdateRate}
                  disabled={isUpdatingRate}
                  style={{ width: 'auto' }}
                >
                  {isUpdatingRate ? 'Updating...' : 'Update Rate'}
                </button>
              </div>
            </div>
          </div>
        </section>

        <div className="admin-grid">
          {/* Categories Section */}
          <div className="admin-card">
            <div className="admin-card-header">
              <h2>Category Management</h2>
            </div>
            
            <div className="admin-card-body">
              <form onSubmit={handleSaveCategory} className="admin-form">
                <div className="admin-form-group">
                  <label>Category Name (e.g. Crypto, Stock)</label>
                  <input
                    type="text"
                    required
                    value={newCatName}
                    onChange={e => setNewCatName(e.target.value)}
                    className="admin-input"
                    placeholder="Enter category name"
                  />
                </div>
                <div className="admin-form-group">
                  <label>Default Currency (e.g. USD, VND)</label>
                  <input
                    type="text"
                    required
                    value={newCatCurrency}
                    onChange={e => setNewCatCurrency(e.target.value)}
                    className="admin-input"
                    placeholder="Enter default currency"
                  />
                </div>
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button type="submit" className="admin-btn" style={{ flex: 1 }}>
                    {editingCategoryId ? 'Save Changes' : '+ Add Category'}
                  </button>
                  {editingCategoryId && (
                    <button type="button" onClick={resetCategoryForm} className="admin-btn" style={{ background: '#94a3b8' }}>
                      Cancel
                    </button>
                  )}
                </div>
              </form>

              <div className="admin-list-container" style={{ marginTop: '2rem' }}>
                {categories.map(c => (
                  <div key={c.id} className="admin-list-item" style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', alignItems: 'flex-start' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%', alignItems: 'center' }}>
                      <span className="item-title">{c.name}</span>
                      <span className="admin-badge">{c.defaultCurrency}</span>
                    </div>
                    <div style={{ display: 'flex', gap: '0.5rem', alignSelf: 'flex-end' }}>
                      <button onClick={() => handleEditCategory(c)} className="action-btn edit-btn">Edit</button>
                      <button onClick={() => handleDeleteCategory(c.id)} className="action-btn delete-btn">Delete</button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Market Assets Section */}
          <div className="admin-card">
            <div className="admin-card-header">
              <h2>Market Asset Management</h2>
            </div>

            <div className="filter-bar">
              <span style={{ fontSize: '0.875rem', fontWeight: 500, color: '#475569' }}>Filter by:</span>
              <select
                value={selectedCategoryId}
                onChange={handleFilterChange}
                className="admin-input admin-select"
                style={{ flex: 1 }}
              >
                <option value="">All Market Assets</option>
                {categories.map(c => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            </div>

            <div className="admin-card-body">
              <form onSubmit={handleSaveMarketAsset} className="admin-form" style={{ marginBottom: '2rem' }}>
                <div className="admin-form-group">
                  <label>Target Category</label>
                  <select
                    required
                    value={newAssetCategoryId}
                    onChange={e => setNewAssetCategoryId(e.target.value)}
                    className="admin-input admin-select"
                  >
                    <option value="">-- Select Category --</option>
                    {categories.map(c => (
                      <option key={c.id} value={c.id}>{c.name}</option>
                    ))}
                  </select>
                </div>
                
                <div className="grid-cols-2">
                  <div className="admin-form-group">
                    <label>Symbol</label>
                    <input
                      type="text"
                      required
                      value={newAssetSymbol}
                      onChange={e => setNewAssetSymbol(e.target.value)}
                      className="admin-input"
                      placeholder="e.g. AAPL"
                    />
                  </div>
                  <div className="admin-form-group">
                    <label>Current Price</label>
                    <input
                      type="number"
                      required
                      min="0"
                      step="any"
                      value={newAssetPrice}
                      onChange={e => setNewAssetPrice(e.target.value)}
                      className="admin-input"
                      placeholder="e.g. 150.00"
                    />
                  </div>
                </div>
                
                <div className="admin-form-group">
                  <label>Asset Full Name</label>
                  <input
                    type="text"
                    required
                    value={newAssetName}
                    onChange={e => setNewAssetName(e.target.value)}
                    className="admin-input"
                    placeholder="e.g. Apple Inc."
                  />
                </div>
                
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button type="submit" className="admin-btn" style={{ flex: 1 }}>
                    {editingMarketAssetId ? 'Save Changes' : '+ Add Market Asset'}
                  </button>
                  {editingMarketAssetId && (
                    <button type="button" onClick={resetMarketAssetForm} className="admin-btn" style={{ background: '#94a3b8' }}>
                      Cancel
                    </button>
                  )}
                </div>
              </form>

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
                    No market assets found for this category.
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
