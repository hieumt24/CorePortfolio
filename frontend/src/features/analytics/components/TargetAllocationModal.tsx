import React, { useState, useEffect } from 'react';
import type { TargetAllocationDto, TargetAllocationInput } from '../types';
import { analyticsApi } from '../api/analyticsApi';
import { getTargetAllocationDraftState } from '../utils/targetAllocationValidation';
import '../../cashflows/components/CashflowDashboard.css';

interface TargetAllocationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSaved: () => void;
}

export const TargetAllocationModal: React.FC<TargetAllocationModalProps> = ({ isOpen, onClose, onSaved }) => {
  const [allocations, setAllocations] = useState<TargetAllocationDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (isOpen) {
      loadAllocations();
    }
  }, [isOpen]);

  const loadAllocations = async () => {
    setLoading(true);
    setError('');
    try {
      const plan = await analyticsApi.getTargetAllocations();
      setAllocations(plan.allocations);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lỗi tải dữ liệu tỷ trọng mục tiêu');
    } finally {
      setLoading(false);
    }
  };

  const handlePercentageChange = (categoryId: string, value: string) => {
    let numValue = parseFloat(value);
    if (isNaN(numValue)) numValue = 0;
    
    setAllocations(prev => prev.map(a => 
      a.categoryId === categoryId ? { ...a, targetPercentage: numValue } : a
    ));
  };

  const handleSave = async () => {
    const draft = getTargetAllocationDraftState(allocations);
    if (!draft.canSave) {
      setError('Tổng tỷ trọng phải bằng 100%, hoặc bằng 0% để xóa kế hoạch mục tiêu.');
      return;
    }

    setSaving(true);
    setError('');
    try {
      const inputs: TargetAllocationInput[] = allocations.map(a => ({
        categoryId: a.categoryId,
        targetPercentage: a.targetPercentage
      }));
      await analyticsApi.updateTargetAllocations(inputs);
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lỗi khi lưu tỷ trọng mục tiêu');
    } finally {
      setSaving(false);
    }
  };

  if (!isOpen) return null;

  const draft = getTargetAllocationDraftState(allocations);
  const currentTotal = draft.total;
  const canSave = draft.canSave;

  return (
    <div className="modal-overlay" role="presentation">
      <div
        aria-labelledby="target-allocation-title"
        aria-modal="true"
        className="modal-content glass-panel"
        role="dialog"
        style={{ maxWidth: '500px' }}
      >
        <div className="modal-header">
          <h2 id="target-allocation-title">Cài đặt tỷ trọng mục tiêu</h2>
          <button aria-label="Đóng" className="icon-btn" onClick={onClose} type="button">×</button>
        </div>

        <div className="modal-body">
          {error && <div className="error-message" style={{ marginBottom: '1rem', color: '#ef4444' }}>{error}</div>}
          
          {loading ? (
            <div className="loading-state"><div className="spinner"></div></div>
          ) : (
            <div className="form-grid">
              {allocations.map(a => (
                <div key={a.categoryId} className="form-group" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <label>{a.categoryName}</label>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <input 
                      type="number" 
                      min="0" max="100" step="0.1"
                      className="modern-input"
                      style={{ width: '100px', textAlign: 'right' }}
                      value={a.targetPercentage}
                      onChange={(e) => handlePercentageChange(a.categoryId, e.target.value)}
                    />
                    <span>%</span>
                  </div>
                </div>
              ))}

              <div className="form-group" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid rgba(255,255,255,0.1)' }}>
                <label><strong>Tổng cộng</strong></label>
                <div style={{ color: draft.isComplete ? '#10b981' : (draft.isCleared ? '#94a3b8' : '#f59e0b') }}>
                  <strong>{currentTotal.toFixed(1)}%</strong>
                </div>
              </div>
              {!canSave && (
                <p className="target-allocation-hint" role="status">
                  Cần phân bổ đủ 100%. Đặt tất cả về 0% nếu muốn xóa kế hoạch mục tiêu.
                </p>
              )}
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button className="btn-secondary" onClick={onClose} disabled={saving} type="button">Hủy</button>
          <button className="btn-primary" onClick={handleSave} disabled={saving || !canSave} type="button">
            {saving ? 'Đang lưu...' : 'Lưu cài đặt'}
          </button>
        </div>
      </div>
    </div>
  );
};
