import { useState, useEffect } from 'react';
import { categoriesApi } from '../api/categories';
import { useNotification } from '../../../context/NotificationContext';
import type { AssetCategory } from '../types';
import './CategoryManagement.css';

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
      showNotification('Category saved successfully', 'success');
    } catch (error) {
      console.error('Failed to save category', error);
      showNotification('Failed to save Category', 'error');
    }
  };

  const handleEditCategory = (c: AssetCategory) => {
    setEditingCategoryId(c.id);
    setNewCatName(c.name);
    setNewCatCurrency(c.defaultCurrency);
  };

  const handleDeleteCategory = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this Category?')) return;
    try {
      await categoriesApi.deleteCategory(id);
      showNotification('Category deleted successfully', 'success');
      loadCategories();
    } catch (error: any) {
      console.error('Failed to delete category', error);
      showNotification('Cannot delete this Category because it contains Market Asset data!', 'error');
    }
  };

  const resetCategoryForm = () => {
    setEditingCategoryId(null);
    setNewCatName('');
    setNewCatCurrency('VND');
  };

  return (
    <div className="admin-page-container">
      <div className="admin-page-header">
        <h2>General Category Management</h2>
        <p className="admin-page-subtitle">Manage high-level asset categories</p>
      </div>
      
      <div className="admin-card glass-panel">
        <div className="glass-panel-header">
          <h3>{editingCategoryId ? 'Edit Category' : 'Create New Category'}</h3>
        </div>
        
        <form onSubmit={handleSaveCategory} className="glass-form">
          <div className="form-row">
            <div className="form-group">
              <label>Category Name</label>
              <input
                type="text"
                required
                value={newCatName}
                onChange={e => setNewCatName(e.target.value)}
                className="modern-input"
                placeholder="e.g. Crypto, Stock"
              />
            </div>
            <div className="form-group">
              <label>Default Currency</label>
              <select
                value={newCatCurrency}
                onChange={e => setNewCatCurrency(e.target.value)}
                className="modern-select"
              >
                <option value="VND">VND</option>
                <option value="USD">USD</option>
              </select>
            </div>
          </div>
          
          <div className="form-actions">
            <button type="submit" className="btn-primary glow-effect">
              {editingCategoryId ? '💾 Save Changes' : '➕ Add Category'}
            </button>
            {editingCategoryId && (
              <button type="button" onClick={resetCategoryForm} className="btn-secondary">
                Cancel
              </button>
            )}
          </div>
        </form>
      </div>

      <div className="admin-list-container">
        {categories.map(c => (
          <div key={c.id} className="category-card glass-item">
            <div className="category-card-content">
              <div className="category-icon">📁</div>
              <div className="category-details">
                <span className="item-title">{c.name}</span>
                <span className="badge currency-badge">{c.defaultCurrency}</span>
              </div>
            </div>
            <div className="category-actions">
              <button onClick={() => handleEditCategory(c)} className="icon-btn edit-btn" title="Edit">
                ✏️
              </button>
              <button onClick={() => handleDeleteCategory(c.id)} className="icon-btn delete-btn" title="Delete">
                🗑️
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
