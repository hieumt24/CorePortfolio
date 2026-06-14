import { useState, useEffect } from 'react';
import { categoriesApi } from '../api/categories';
import { useNotification } from '../../../context/NotificationContext';
import type { AssetCategory } from '../types';

export function CategoryManagement() {
  const { showNotification } = useNotification();
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null);
  const [newCatName, setNewCatName] = useState('');
  const [newCatCurrency, setNewCatCurrency] = useState('VND');

  useEffect(() => {
    loadCategories();
  }, []);

  const loadCategories = async () => {
    try {
      const categoriesRes = await categoriesApi.getCategories();
      setCategories(categoriesRes || []);
    } catch (err: any) {
      console.error('Failed to load categories', err);
    }
  };

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

  return (
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
  );
}
