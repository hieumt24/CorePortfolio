import { useState, useEffect } from 'react';
import { cashflowsApi } from '../../cashflows/api/cashflowsApi';
import type { CashflowCategory } from '../../cashflows/types/cashflows';
import { CashflowType } from '../../cashflows/types/cashflows';
import { useNotification } from '../../../context/NotificationContext';
import './AdminDashboard.css';
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
      // Hiện tại Admin quản lý Global Categories
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
        isGlobal: true
      };

      if (editingCategoryId) {
        await cashflowsApi.updateCategory(editingCategoryId, command);
      } else {
        await cashflowsApi.createCategory(command);
      }
      resetCategoryForm();
      loadCategories();
      showNotification('Đã lưu Cashflow Category', 'success');
    } catch (error) {
      console.error('Failed to save category', error);
      showNotification('Đã xảy ra lỗi khi lưu Category', 'error');
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
    if (!window.confirm('Bạn có chắc chắn muốn xóa Category này?')) return;
    try {
      await cashflowsApi.deleteCategory(id);
      showNotification('Xóa Category thành công', 'success');
      loadCategories();
    } catch (error: any) {
      console.error('Failed to delete category', error);
      showNotification('Không thể xóa Category này vì nó đang chứa dữ liệu giao dịch!', 'error');
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
    <div className="admin-card">
      <div className="admin-card-header">
        <h2>Cashflow Category Management</h2>
      </div>
      
      <div className="admin-card-body cashflow-category-layout">
        
        {/* Left Side: Form */}
        <div className="category-form-section">
          <form onSubmit={handleSaveCategory} className="admin-form">
            <div className="admin-form-group">
              <label>Tên danh mục (Category Name)</label>
              <input
                type="text"
                required
                value={newCatName}
                onChange={e => setNewCatName(e.target.value)}
                className="admin-input"
                placeholder="E.g. Lương, Ăn uống, Giải trí..."
              />
            </div>
            
            <div className="admin-form-group">
              <label>Loại (Type)</label>
              <select
                value={newCatType}
                onChange={e => setNewCatType(Number(e.target.value) as CashflowType)}
                className="admin-input admin-select"
              >
                <option value={CashflowType.Income}>Thu nhập (Income)</option>
                <option value={CashflowType.Expense}>Chi tiêu (Expense)</option>
              </select>
            </div>

            <div className="admin-form-group">
              <label>Biểu tượng (Icon / Emoji)</label>
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
                  <label>Hoặc nhập/paste Emoji tuỳ chỉnh:</label>
                  <input
                    type="text"
                    value={newCatIcon}
                    onChange={e => setNewCatIcon(e.target.value)}
                    className="admin-input"
                    placeholder="🍔"
                    maxLength={2}
                    style={{ width: '80px', textAlign: 'center', fontSize: '1.25rem', padding: '0.5rem' }}
                  />
                </div>
              </div>
            </div>

            <div className="admin-form-group">
              <label>Màu sắc (Theme Color)</label>
              <div className="color-picker-container">
                <div className="color-preview" style={{ backgroundColor: newCatColor }}>
                  <input
                    type="color"
                    value={newCatColor}
                    onChange={e => setNewCatColor(e.target.value)}
                  />
                </div>
                <span className="color-hex">{newCatColor.toUpperCase()}</span>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1rem' }}>
              <button type="submit" className="admin-btn" style={{ flex: 1 }}>
                {editingCategoryId ? '💾 Lưu thay đổi' : '✨ Thêm Danh mục'}
              </button>
              {editingCategoryId && (
                 <button type="button" onClick={resetCategoryForm} className="admin-btn" style={{ background: 'rgba(255,255,255,0.1)', color: 'white', border: '1px solid rgba(255,255,255,0.2)', boxShadow: 'none' }}>
                  Hủy (Cancel)
                </button>
              )}
            </div>
          </form>
        </div>

        {/* Right Side: List/Grid */}
        <div className="category-list-section">
          <div className="filter-bar" style={{ borderRadius: '12px', marginBottom: '1rem', background: 'rgba(15, 23, 42, 0.3)' }}>
            <span style={{ color: 'var(--text-secondary)', fontWeight: 500 }}>
              Đang quản lý: {categories.length} danh mục Global
            </span>
          </div>

          <div className="category-grid-list">
            {categories.map(c => (
              <div key={c.id} className="category-card">
                <div 
                  className="category-icon-wrapper"
                  style={{ 
                    backgroundColor: `${c.color}22`, // 22 is hex alpha for roughly 15% opacity
                    color: c.color,
                    border: `1px solid ${c.color}44`
                  }}
                >
                  {c.icon}
                </div>
                
                <div className="category-info">
                  <span className="category-name">{c.name}</span>
                  <span className={`category-type-badge ${c.type === CashflowType.Income ? 'category-type-income' : 'category-type-expense'}`}>
                    {c.type === CashflowType.Income ? 'Thu nhập' : 'Chi tiêu'}
                  </span>
                </div>

                <div className="category-actions">
                  <button onClick={() => handleEditCategory(c)} className="action-btn edit-btn" style={{ flex: 1 }}>Sửa</button>
                  <button onClick={() => handleDeleteCategory(c.id)} className="action-btn delete-btn" style={{ flex: 1 }}>Xóa</button>
                </div>
              </div>
            ))}
            
            {categories.length === 0 && (
              <div className="admin-empty-state" style={{ gridColumn: '1 / -1' }}>
                Chưa có danh mục nào. Hãy tạo danh mục đầu tiên ở form bên trái!
              </div>
            )}
          </div>
        </div>

      </div>
    </div>
  );
}
