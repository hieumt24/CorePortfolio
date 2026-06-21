import React, { useState } from 'react';
import { useCreateCashflow, useCashflowCategories } from '../hooks/useCashflows';
import { usePortfolios } from '../../portfolios/hooks/usePortfolios';
import { CashflowType } from '../types/cashflows';
import './AddCashflowModal.css';

interface AddCashflowModalProps {
  onClose: () => void;
  defaultType?: CashflowType;
}

export const AddCashflowModal: React.FC<AddCashflowModalProps> = ({ onClose, defaultType = CashflowType.Income }) => {
  const [type, setType] = useState<CashflowType>(defaultType);
  const [amount, setAmount] = useState<string>('');
  const [portfolioId, setPortfolioId] = useState<string>('');
  const [categoryId, setCategoryId] = useState<string>('');
  const [currency, setCurrency] = useState<string>('VND');
  const [date, setDate] = useState<string>(new Date().toISOString().slice(0, 16));
  const [description, setDescription] = useState<string>('');

  const { categories } = useCashflowCategories();
  const { portfolios } = usePortfolios();
  const createCashflow = useCreateCashflow();

  const filteredCategories = categories?.filter((c) => c.type === type) || [];

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!amount || !portfolioId || !categoryId) return;

    createCashflow.mutate(
      {
        portfolioId,
        categoryId,
        amount: parseFloat(amount),
        currency,
        date: new Date(date).toISOString(),
        description,
      },
      {
        onSuccess: () => {
          onClose();
        },
      }
    );
  };

  return (
    <div className="modal-overlay">
      <div className="cashflow-modal">
        <div className="modal-header">
          <h2>Ghi chép Thu / Chi</h2>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>
        <div className="modal-body">
          <div className="type-toggle">
            <button
              className={type === CashflowType.Income ? 'active income' : ''}
              onClick={() => setType(CashflowType.Income)}
            >
              Thu nhập
            </button>
            <button
              className={type === CashflowType.Expense ? 'active expense' : ''}
              onClick={() => setType(CashflowType.Expense)}
            >
              Chi tiêu
            </button>
          </div>

          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Số tiền</label>
              <div className="amount-input-group">
                <input
                  type="number"
                  className="glass-input-light"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  placeholder="0.00"
                  required
                  min="0"
                  step="any"
                />
                <select
                  className="glass-input-light currency-select"
                  value={currency}
                  onChange={(e) => setCurrency(e.target.value)}
                >
                  <option value="VND">VND</option>
                  <option value="USD">USD</option>
                </select>
              </div>
            </div>

            <div className="form-group">
              <label>Danh mục Đầu tư (Portfolio)</label>
              <select
                className="glass-input-light"
                value={portfolioId}
                onChange={(e) => setPortfolioId(e.target.value)}
                required
              >
                <option value="">Chọn Portfolio liên kết...</option>
                {portfolios?.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
              <small className="help-text">
                {type === CashflowType.Income 
                  ? 'Khoản tiền này sẽ được Nạp (Deposit) vào Tiền mặt của Portfolio đã chọn.' 
                  : 'Khoản tiền này sẽ được Rút (Withdraw) khỏi Tiền mặt của Portfolio đã chọn.'}
              </small>
            </div>

            <div className="form-group">
              <label>Nhóm phân loại (Category)</label>
              <select
                className="glass-input-light"
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                required
              >
                <option value="">Chọn nhóm phân loại...</option>
                {filteredCategories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.icon} {c.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Ngày giao dịch</label>
              <input
                type="datetime-local"
                className="glass-input-light"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label>Mô tả / Ghi chú</label>
              <textarea
                className="glass-input-light"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Nhập ghi chú (không bắt buộc)..."
                rows={2}
              ></textarea>
            </div>

            <button
              type="submit"
              className={`submit-btn ${type === CashflowType.Income ? 'income-btn' : 'expense-btn'}`}
              disabled={createCashflow.isPending}
            >
              {createCashflow.isPending ? 'Đang lưu...' : 'Lưu Giao Dịch'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
