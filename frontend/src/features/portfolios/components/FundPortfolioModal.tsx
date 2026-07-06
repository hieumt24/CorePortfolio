import React, { useState } from 'react';
import { cashAccountsApi } from '../api/cashAccountsApi';
import type { AdjustCashBalanceCommand } from '../api/cashAccountsApi';
import { useNotification } from '../../../context/NotificationContext';
import './FundPortfolioModal.css';

interface FundPortfolioModalProps {
  portfolioId: string;
  onClose: () => void;
  onSuccess: () => void;
}

export const FundPortfolioModal: React.FC<FundPortfolioModalProps> = ({ portfolioId, onClose, onSuccess }) => {
  const [isDeposit, setIsDeposit] = useState(true);
  const [currency, setCurrency] = useState('VND');
  const [amount, setAmount] = useState<number | ''>('');
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(false);
  const { showNotification } = useNotification();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!amount || amount <= 0) {
      showNotification('Vui lòng nhập số tiền hợp lệ (>0).', 'error');
      return;
    }

    setLoading(true);
    try {
      const command: AdjustCashBalanceCommand = {
        portfolioId,
        currency,
        amount: Number(amount),
        isDeposit,
        description: description.trim() || (isDeposit ? 'Nạp tiền' : 'Rút tiền'),
        occurredAt: new Date().toISOString()
      };
      
      await cashAccountsApi.adjustBalance(command);
      onSuccess();
    } catch (error: any) {
      showNotification(error.message || 'Có lỗi xảy ra', 'error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content glass-panel fund-modal">
        <div className="modal-header">
          <h2>Quản lý Tiền mặt (Deposit / Withdraw)</h2>
          <button className="close-btn" onClick={onClose} disabled={loading}>×</button>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="form-group type-selector">
            <button 
              type="button" 
              className={`type-btn ${isDeposit ? 'active deposit' : ''}`} 
              onClick={() => setIsDeposit(true)}
            >
              Deposit (Nạp tiền)
            </button>
            <button 
              type="button" 
              className={`type-btn ${!isDeposit ? 'active withdraw' : ''}`} 
              onClick={() => setIsDeposit(false)}
            >
              Withdraw (Rút tiền)
            </button>
          </div>

          <div className="form-group">
            <label>Currency (Loại tiền tệ)</label>
            <select 
              value={currency} 
              onChange={e => setCurrency(e.target.value)}
              disabled={loading}
              className="glass-input"
            >
              <option value="VND">VND</option>
              <option value="USD">USD</option>
            </select>
          </div>

          <div className="form-group">
            <label>Amount (Số tiền)</label>
            <input 
              type="number" 
              step="0.01" 
              min="0.01"
              value={amount} 
              onChange={e => setAmount(e.target.value ? Number(e.target.value) : '')} 
              required
              disabled={loading}
              className="glass-input"
              placeholder="Nhập số tiền..."
            />
          </div>

          <div className="form-group">
            <label>Description (Ghi chú - Tùy chọn)</label>
            <input 
              type="text" 
              value={description} 
              onChange={e => setDescription(e.target.value)} 
              disabled={loading}
              className="glass-input"
              placeholder={isDeposit ? 'Ví dụ: Chuyển lương tháng này...' : 'Ví dụ: Rút tiền tiêu xài...'}
            />
          </div>

          <div className="modal-actions">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Hủy
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Đang xử lý...' : 'Xác nhận'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
