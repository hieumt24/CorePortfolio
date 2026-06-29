import { useState, useEffect } from 'react';
import { cashflowsApi } from '../../cashflows/api/cashflowsApi';
import type { CashflowCategory } from '../../cashflows/types/cashflows';
import { CashflowType } from '../../cashflows/types/cashflows';
import { useNotification } from '../../../context/NotificationContext';
import './CashflowCategoryManagement.css';

const COMMON_EMOJIS = [
  '💰', '💵', '💳', '🧾', '📈', '📉', '🏦', '🛍️', 
  '🍔', '☕', '🛒', '🚗', '✈️', '🏠', '🎮', '👗', 
  '🏥', '💊', '📚', '🎓', '🎁', '🐶', '💡', '💧'
];

export function CashflowCategoryManagement() {
  const { showNotification } = useNotification();
  const [categories, setCategories] = useState<CashflowCategory[]>([]);
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null);
  
  const [newCatName, setNewCatName] = useState('');
  const [newCatType, setNewCatType] = useState<CashflowType>(CashflowType.Expense);
  const [newCatIcon, setNewCatIcon] = useState('💰');
  const [newCatColor, setNewCatColor] = useState('#60a5fa');

  useEffect(() => {
    loadCategories();
  }, []);

  const loadCategories = async () => {
    try {
      const categoriesRes = await cashflowsApi.getCategories();
      setCategories(categoriesRes?.filter(c => c.isGlobal) || []);
    } catch (err: any) {
      console.error('Failed to load categories', err);
    }
  };

  const handleSaveCategory = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const command = {
        name: newCatName,
        type: newCatType,
        icon: newCatIcon,
        color: newCatColor,
        isGlobal: true,
        sortOrder: 0
      };

      if (editingCategoryId) {
        await cashflowsApi.updateCategory(editingCategoryId, command);
      } else {
        await cashflowsApi.createCategory(command);
      }
      resetCategoryForm();
      loadCategories();
      showNotification('Cashflow Category saved', 'success');
    } catch (error) {
      console.error('Failed to save category', error);
      showNotification('Error saving Category', 'error');
    }
  };

  const handleEditCategory = (c: CashflowCategory) => {
    setEditingCategoryId(c.id);
    setNewCatName(c.name);
    setNewCatType(c.type);
    setNewCatIcon(c.icon);
    setNewCatColor(c.color);
  };

  const handleDeleteCategory = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this Category?')) return;
    try {
      await cashflowsApi.deleteCategory(id);
      showNotification('Category deleted', 'success');
      loadCategories();
    } catch (error: any) {
      console.error('Failed to delete category', error);
      showNotification('Cannot delete Category because it is in use!', 'error');
    }
  };

  const resetCategoryForm = () => {
    setEditingCategoryId(null);
    setNewCatName('');
    setNewCatType(CashflowType.Expense);
    setNewCatIcon('💰');
    setNewCatColor('#60a5fa');
  };

  return (
    <div className="admin-page-container">
      <div className="admin-page-header">
        <h2>Cashflow Categories</h2>
        <p className="admin-page-subtitle">Manage categories for income and expenses</p>
      </div>
      
      <div className="cashflow-category-layout">
        
        {/* Left Side: Form */}
        <div className="glass-panel form-section">
          <div className="glass-panel-header">
            <h3>{editingCategoryId ? 'Edit Category' : 'Create New Category'}</h3>
          </div>

          <form onSubmit={handleSaveCategory} className="glass-form">
            <div className="form-group">
              <label>Category Name</label>
              <input
                type="text"
                required
                value={newCatName}
                onChange={e => setNewCatName(e.target.value)}
                className="modern-input"
                placeholder="e.g. Salary, Food, Entertainment..."
              />
            </div>
            
            <div className="form-group">
              <label>Type</label>
              <select
                value={newCatType}
                onChange={e => setNewCatType(Number(e.target.value) as CashflowType)}
                className="modern-select"
              >
                <option value={CashflowType.Income}>Income (Thu nhập)</option>
                <option value={CashflowType.Expense}>Expense (Chi tiêu)</option>
              </select>
            </div>

            <div className="form-group">
              <label>Icon / Emoji</label>
              <div className="emoji-picker-container">
                <div className="emoji-grid">
                  {COMMON_EMOJIS.map(emoji => (
                    <button
                      key={emoji}
                      type="button"
                      className={`emoji-btn ${newCatIcon === emoji ? 'active' : ''}`}
                      onClick={() => setNewCatIcon(emoji)}
                    >
                      {emoji}
                    </button>
                  ))}
                </div>
                <div className="emoji-custom-input">
                  <input
                    type="text"
                    value={newCatIcon}
                    onChange={e => setNewCatIcon(e.target.value)}
                    className="modern-input"
                    placeholder="Custom 🍔"
                    maxLength={2}
                    title="Paste a custom emoji here"
                  />
                </div>
              </div>
            </div>

            <div className="form-group">
              <label>Theme Color</label>
              <div className="color-picker-wrapper">
                <input
                  type="color"
                  value={newCatColor}
                  onChange={e => setNewCatColor(e.target.value)}
                  className="modern-color-picker"
                />
                <span className="color-hex">{newCatColor.toUpperCase()}</span>
              </div>
            </div>

            <div className="form-actions" style={{ marginTop: '1rem' }}>
              <button type="submit" className="btn-primary glow-effect" style={{ flex: 1 }}>
                {editingCategoryId ? '💾 Save Changes' : '✨ Add Category'}
              </button>
              {editingCategoryId && (
                 <button type="button" onClick={resetCategoryForm} className="btn-secondary">
                  Cancel
                </button>
              )}
            </div>
          </form>
        </div>

        {/* Right Side: List/Grid */}
        <div className="list-section">
          <div className="glass-panel" style={{ padding: '1.5rem', marginBottom: '1.5rem' }}>
             <p style={{ margin: 0, fontWeight: 500, color: 'rgba(255,255,255,0.7)' }}>
                Managing {categories.length} Global Categories
             </p>
          </div>

          <div className="category-grid">
            {categories.map(c => (
              <div key={c.id} className={`cf-category-card ${c.type === CashflowType.Income ? 'income' : 'expense'}`}>
                <div className="cf-card-top">
                  <div 
                    className="cf-icon"
                    style={{ 
                      backgroundColor: `${c.color}20`,
                      color: c.color,
                      boxShadow: `0 0 15px ${c.color}30`
                    }}
                  >
                    {c.icon}
                  </div>
                </div>
                
                <div className="cf-info">
                  <h4 className="cf-name">{c.name}</h4>
                  <span className={`cf-type-badge ${c.type === CashflowType.Income ? 'income' : 'expense'}`}>
                    {c.type === CashflowType.Income ? 'INCOME' : 'EXPENSE'}
                  </span>
                </div>

                <div className="cf-actions">
                  <button onClick={() => handleEditCategory(c)} className="btn-outline-small">Edit</button>
                  <button onClick={() => handleDeleteCategory(c.id)} className="btn-outline-small danger">Delete</button>
                </div>
              </div>
            ))}
            
            {categories.length === 0 && (
              <div className="empty-state">
                <div className="empty-icon">📁</div>
                <p>No categories yet. Create your first one on the left!</p>
              </div>
            )}
          </div>
        </div>

      </div>
    </div>
  );
}
