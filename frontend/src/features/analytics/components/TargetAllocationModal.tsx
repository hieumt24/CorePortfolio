import React, { useState, useEffect } from 'react';
import type { TargetAllocationDto, TargetAllocationInput } from '../types';
import { analyticsApi } from '../api/analyticsApi';
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
      const data = await analyticsApi.getTargetAllocations();
      setAllocations(data);
    } catch (err) {
      setError('Lỗi tải dữ liệu tỷ trọng mục tiêu');
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
    const total = allocations.reduce((sum, a) => sum + a.targetPercentage, 0);
    if (total > 100) {
      setError('Tổng tỷ trọng mục tiêu không được vượt quá 100%');
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
      setError('Lỗi khi lưu tỷ trọng mục tiêu');
    } finally {
      setSaving(false);
    }
  };

  if (!isOpen) return null;

  const currentTotal = allocations.reduce((sum, a) => sum + a.targetPercentage, 0);

  return (
    <div className="modal-overlay">
      <div className="modal-content glass-panel" style={{ maxWidth: '500px' }}>
        <div className="modal-header">
          <h2>Cài đặt Tỷ trọng Mục tiêu</h2>
          <button className="icon-btn" onClick={onClose}>×</button>
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
                <div style={{ color: currentTotal > 100 ? '#ef4444' : (currentTotal === 100 ? '#10b981' : '#f59e0b') }}>
                  <strong>{currentTotal.toFixed(1)}%</strong>
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button className="btn-secondary" onClick={onClose} disabled={saving}>Hủy</button>
          <button className="btn-primary" onClick={handleSave} disabled={saving || currentTotal > 100}>
            {saving ? 'Đang lưu...' : 'Lưu cài đặt'}
          </button>
        </div>
      </div>
    </div>
  );
};
