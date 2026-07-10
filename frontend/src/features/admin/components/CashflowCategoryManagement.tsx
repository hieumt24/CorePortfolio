import { useEffect, useMemo, useState } from 'react';
import { useNotification } from '../../../context/NotificationContext';
import { cashflowsApi } from '../../cashflows/api/cashflowsApi';
import { CashflowType, type CashflowCategory } from '../../cashflows/types/cashflows';
import './CashflowCategoryManagement.css';

const COMMON_EMOJIS = [
  '💰', '💵', '💳', '🧾', '📈', '📉', '🏦', '🛍️',
  '🍔', '☕', '🛒', '🚗', '✈️', '🏠', '🎮', '👕',
  '🏥', '💊', '📚', '🎓', '🎁', '💡', '💧', '🛡️',
];

const TYPE_META: Record<CashflowType, { label: string; description: string; className: string }> = {
  [CashflowType.Income]: {
    label: 'Thu nhập',
    description: 'Lương, thưởng và các nguồn tiền đi vào.',
    className: 'income',
  },
  [CashflowType.Expense]: {
    label: 'Chi tiêu',
    description: 'Chi phí sinh hoạt, hóa đơn và mua sắm.',
    className: 'expense',
  },
  [CashflowType.Investment]: {
    label: 'Đầu tư',
    description: 'Dòng tiền dành cho cổ phiếu, crypto, quỹ.',
    className: 'investment',
  },
  [CashflowType.Saving]: {
    label: 'Tiết kiệm',
    description: 'Quỹ khẩn cấp, mục tiêu tích lũy và dự phòng.',
    className: 'saving',
  },
};

const CASHFLOW_TYPES: CashflowType[] = [
  CashflowType.Income,
  CashflowType.Expense,
  CashflowType.Investment,
  CashflowType.Saving,
];

const rootCategories = (categories: CashflowCategory[]) =>
  categories.filter(category => category.parentCategoryId === null);

const flattenCategories = (categories: CashflowCategory[]): CashflowCategory[] =>
  categories.flatMap(category => [category, ...flattenCategories(category.subCategories || [])]);

export function CashflowCategoryManagement() {
  const { showNotification } = useNotification();
  const [categories, setCategories] = useState<CashflowCategory[]>([]);
  const [activeType, setActiveType] = useState<CashflowType>(CashflowType.Expense);
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null);
  const [newCatName, setNewCatName] = useState('');
  const [newCatType, setNewCatType] = useState<CashflowType>(CashflowType.Expense);
  const [newCatIcon, setNewCatIcon] = useState('💰');
  const [newCatColor, setNewCatColor] = useState('#60a5fa');
  const [newCatParentId, setNewCatParentId] = useState<string>('');
  const [newCatSortOrder, setNewCatSortOrder] = useState<number>(0);

  useEffect(() => {
    loadCategories();
  }, []);

  const flatCategories = useMemo(() => flattenCategories(categories), [categories]);
  const visibleRoots = useMemo(
    () => rootCategories(categories).filter(category => category.type === activeType),
    [categories, activeType]
  );
  const parentCandidates = useMemo(
    () => rootCategories(categories).filter(category => category.type === newCatType && category.id !== editingCategoryId),
    [categories, newCatType, editingCategoryId]
  );
  const editingCategory = useMemo(
    () => flatCategories.find(category => category.id === editingCategoryId) || null,
    [flatCategories, editingCategoryId]
  );
  const editingHasChildren = Boolean(editingCategory?.subCategories?.length);

  const typeCounts = useMemo(() => {
    const counts = {
      [CashflowType.Income]: 0,
      [CashflowType.Expense]: 0,
      [CashflowType.Investment]: 0,
      [CashflowType.Saving]: 0,
    } as Record<CashflowType, number>;

    flatCategories.forEach(category => {
      counts[category.type] += 1;
    });
    return counts;
  }, [flatCategories]);

  const loadCategories = async () => {
    try {
      const categoriesRes = await cashflowsApi.getCategories();
      setCategories(categoriesRes?.filter(category => category.isGlobal) || []);
    } catch (err) {
      console.error('Failed to load categories', err);
    }
  };

  const handleSaveCategory = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const command = {
        name: newCatName.trim(),
        type: newCatType,
        icon: newCatIcon,
        color: newCatColor,
        isGlobal: true,
        sortOrder: newCatSortOrder,
        parentCategoryId: newCatParentId || null,
      };

      if (editingCategoryId) {
        await cashflowsApi.updateCategory(editingCategoryId, command);
      } else {
        await cashflowsApi.createCategory(command);
      }

      resetCategoryForm();
      await loadCategories();
      showNotification('Đã lưu category', 'success');
    } catch (error) {
      console.error('Failed to save category', error);
      showNotification('Không thể lưu category. Kiểm tra parent/type và thử lại.', 'error');
    }
  };

  const handleEditCategory = (category: CashflowCategory) => {
    setEditingCategoryId(category.id);
    setNewCatName(category.name);
    setNewCatType(category.type);
    setNewCatIcon(category.icon);
    setNewCatColor(category.color);
    setNewCatParentId(category.parentCategoryId || '');
    setNewCatSortOrder(category.sortOrder);
    setActiveType(category.type);
  };

  const handleDeleteCategory = async (id: string) => {
    if (!window.confirm('Bạn chắc chắn muốn xóa category này?')) return;
    try {
      await cashflowsApi.deleteCategory(id);
      showNotification('Đã xóa category', 'success');
      await loadCategories();
    } catch (error) {
      console.error('Failed to delete category', error);
      showNotification('Không thể xóa category vì đang được sử dụng hoặc có child.', 'error');
    }
  };

  const resetCategoryForm = () => {
    setEditingCategoryId(null);
    setNewCatName('');
    setNewCatType(activeType);
    setNewCatIcon('💰');
    setNewCatColor('#60a5fa');
    setNewCatParentId('');
    setNewCatSortOrder(0);
  };

  const changeType = (type: CashflowType) => {
    setNewCatType(type);
    setNewCatParentId('');
    setActiveType(type);
  };

  const renderCategoryNode = (category: CashflowCategory) => {
    const meta = TYPE_META[category.type];
    const isParent = category.parentCategoryId === null;

    return (
      <div key={category.id} className={`category-tree-node ${isParent ? 'parent' : 'child'} ${meta.className}`}>
        <div className="category-node-main">
          <div className="category-node-icon" style={{ color: category.color, borderColor: `${category.color}70` }}>
            {category.icon}
          </div>
          <div className="category-node-copy">
            <div className="category-node-title">
              <h4>{category.name}</h4>
              <span>{isParent ? 'Parent' : 'Child'}</span>
            </div>
            <p>{meta.label} · sort {category.sortOrder} · {category.subCategories?.length || 0} child</p>
          </div>
        </div>

        <div className="category-node-actions">
          <button onClick={() => handleEditCategory(category)} className="btn btn-outline btn-sm">Sửa</button>
          <button onClick={() => handleDeleteCategory(category.id)} className="btn btn-outline btn-sm danger">Xóa</button>
        </div>

        {category.subCategories?.length > 0 && (
          <div className="category-child-list">
            {category.subCategories.map(child => renderCategoryNode(child))}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="admin-page-container cashflow-admin-page">
      <div className="cashflow-admin-hero">
        <div>
          <span className="admin-kicker">Category pattern</span>
          <h2>Cashflow Categories</h2>
          <p>Quản lý category cha/con cho thu nhập, chi tiêu, đầu tư và tiết kiệm. Mỗi parent chỉ có một cấp child để báo cáo luôn gọn.</p>
        </div>
      </div>

      <section className="category-pattern-grid">
        {CASHFLOW_TYPES.map(type => {
          const meta = TYPE_META[type];
          return (
            <button
              key={type}
              type="button"
              className={`category-pattern-card glass-panel ${meta.className} ${activeType === type ? 'active' : ''}`}
              onClick={() => changeType(type)}
            >
              <span>{meta.label}</span>
              <strong>{typeCounts[type]}</strong>
              <small>{meta.description}</small>
            </button>
          );
        })}
      </section>

      <div className="cashflow-category-layout">
        <div className="glass-panel form-section category-builder">
          <div className="builder-header">
            <div>
              <h3>{editingCategoryId ? 'Sửa category' : 'Tạo category mới'}</h3>
              <p>{newCatParentId ? 'Category này sẽ là child của parent đã chọn.' : 'Để trống parent nếu muốn tạo category cha.'}</p>
            </div>
            {editingCategoryId && (
              <button type="button" onClick={resetCategoryForm} className="btn btn-outline btn-sm">Hủy sửa</button>
            )}
          </div>

          <form onSubmit={handleSaveCategory} className="glass-form">
            <div className="form-group">
              <label>Tên category</label>
              <input
                type="text"
                required
                value={newCatName}
                onChange={e => setNewCatName(e.target.value)}
                placeholder="VD: Ăn uống, Lương chính, Quỹ khẩn cấp"
              />
            </div>

            <div className="form-row compact">
              <div className="form-group">
                <label>Pattern</label>
                <select
                  value={newCatType}
                  onChange={e => {
                    setNewCatType(Number(e.target.value) as CashflowType);
                    setNewCatParentId('');
                  }}
                  disabled={editingHasChildren}
                >
                  <option value={CashflowType.Income}>Thu nhập</option>
                  <option value={CashflowType.Expense}>Chi tiêu</option>
                  <option value={CashflowType.Investment}>Đầu tư</option>
                  <option value={CashflowType.Saving}>Tiết kiệm</option>
                </select>
              </div>
              <div className="form-group">
                <label>Sort order</label>
                <input
                  type="number"
                  value={newCatSortOrder}
                  onChange={e => setNewCatSortOrder(Number(e.target.value))}
                />
              </div>
            </div>

            <div className="form-group">
              <label>Parent category</label>
              <select
                value={newCatParentId}
                onChange={e => setNewCatParentId(e.target.value)}
                disabled={editingHasChildren}
              >
                <option value="">Không có parent - tạo category cha</option>
                {parentCandidates.map(category => (
                  <option key={category.id} value={category.id}>
                    {category.icon} {category.name}
                  </option>
                ))}
              </select>
              {editingHasChildren && <small className="field-hint">Category đang có child nên không thể đổi type hoặc chuyển thành child.</small>}
            </div>

            <div className="form-group">
              <label>Icon</label>
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
                <input
                  type="text"
                  value={newCatIcon}
                  onChange={e => setNewCatIcon(e.target.value)}
                  placeholder="Icon"
                  maxLength={4}
                />
              </div>
            </div>

            <div className="form-group">
              <label>Màu theme</label>
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

            <button type="submit" className="btn btn-primary">
              {editingCategoryId ? 'Lưu thay đổi' : 'Thêm category'}
            </button>
          </form>
        </div>

        <div className="category-tree-section">
          <div className="tree-section-header glass-panel">
            <div>
              <h3>{TYPE_META[activeType].label}</h3>
              <p>{visibleRoots.length} parent category · {typeCounts[activeType]} category tổng cộng</p>
            </div>
          </div>

          <div className="category-tree-list">
            {visibleRoots.length === 0 ? (
              <div className="empty-state glass-panel">
                <strong>Chưa có category cho pattern này.</strong>
                <span>Tạo parent category trước, sau đó thêm child bên dưới parent đó.</span>
              </div>
            ) : (
              visibleRoots.map(category => renderCategoryNode(category))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
