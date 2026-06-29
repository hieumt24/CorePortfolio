import React, { useState } from 'react';
import { useCreateCashflow, useCashflowCategories } from '../hooks/useCashflows';
import { usePortfolios } from '../../portfolios/hooks/usePortfolios';
import { CashflowType } from '../types/cashflows';
import type { CashflowRecord } from '../types/cashflows';
import { cashflowsApi } from '../api/cashflowsApi';
import { useNotification } from '../../../context/NotificationContext';
import { NumericFormat } from 'react-number-format';
import './AddCashflowModal.css';

interface AddCashflowModalProps {
  onClose: () => void;
  defaultType?: CashflowType;
  cashflowToEdit?: CashflowRecord;
}

export const AddCashflowModal: React.FC<AddCashflowModalProps> = ({ onClose, defaultType = CashflowType.Income, cashflowToEdit }) => {
  const [type, setType] = useState<CashflowType>(cashflowToEdit?.type ?? defaultType);
  const [amount, setAmount] = useState<string>(cashflowToEdit?.amount?.toString() ?? '');
  const [portfolioId, setPortfolioId] = useState<string>(cashflowToEdit?.portfolioId ?? '');
  const [categoryId, setCategoryId] = useState<string>(cashflowToEdit?.categoryId ?? '');
  const [currency, setCurrency] = useState<string>(cashflowToEdit?.currency ?? 'VND');
  
  const [date, setDate] = useState<string>(
    cashflowToEdit ? new Date(cashflowToEdit.date).toISOString().slice(0, 16) : new Date().toISOString().slice(0, 16)
  );
  const [description, setDescription] = useState<string>(cashflowToEdit?.description ?? '');

  const { categories } = useCashflowCategories();
  const { portfolios } = usePortfolios();
  const createCashflow = useCreateCashflow();
  const { showNotification } = useNotification();
  const [isUpdating, setIsUpdating] = useState(false);

  const filteredCategories = categories?.filter((c) => c.type === type) || [];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!amount || !portfolioId || !categoryId) return;

    if (cashflowToEdit) {
      setIsUpdating(true);
      try {
        await cashflowsApi.updateCashflow(cashflowToEdit.id, {
          portfolioId,
          categoryId,
          amount: parseFloat(amount),
          currency,
          date: new Date(date).toISOString(),
          description,
        });
        showNotification('Cập nhật giao dịch thành công!', 'success');
        onClose();
      } catch (error) {
        showNotification('Có lỗi xảy ra khi cập nhật.', 'error');
        console.error(error);
      } finally {
        setIsUpdating(false);
      }
    } else {
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
    }
  };

  const getButtonClass = () => {
    switch (type) {
      case CashflowType.Income: return 'income-btn';
      case CashflowType.Expense: return 'expense-btn';
      case CashflowType.Investment: return 'investment-btn';
      case CashflowType.Saving: return 'saving-btn';
      default: return 'income-btn';
    }
  };

  return (
    <div className="modal-overlay">
      <div className="cashflow-modal">
        <div className="modal-header">
          <h2>Ghi chép Thu / Chi</h2>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>
        <div className="modal-body">
          <div className="type-toggle" style={{ gridTemplateColumns: 'repeat(4, 1fr)' }}>
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
            <button
              className={type === CashflowType.Investment ? 'active investment' : ''}
              onClick={() => setType(CashflowType.Investment)}
            >
              Đầu tư
            </button>
            <button
              className={type === CashflowType.Saving ? 'active saving' : ''}
              onClick={() => setType(CashflowType.Saving)}
            >
              Tiết kiệm
            </button>
          </div>

          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Số tiền</label>
              <div className="amount-input-group">
                <NumericFormat
                  className="glass-input-light"
                  value={amount}
                  onValueChange={(values) => {
                    setAmount(values.value);
                  }}
                  thousandSeparator="."
                  decimalSeparator=","
                  allowNegative={false}
                  placeholder="0"
                  required
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
                {cashflowToEdit && ' Lưu ý: Đổi Portfolio lúc này có thể không chuyển Transaction tương ứng, chỉ sửa thông tin.'}
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
                {filteredCategories.map((c) => {
                  if (c.subCategories && c.subCategories.length > 0) {
                    return (
                      <optgroup key={c.id} label={`${c.icon} ${c.name}`}>
                        {c.subCategories.map(sub => (
                          <option key={sub.id} value={sub.id}>
                            {sub.icon} {sub.name}
                          </option>
                        ))}
                      </optgroup>
                    );
                  }
                  return (
                    <option key={c.id} value={c.id}>
                      {c.icon} {c.name}
                    </option>
                  );
                })}
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
              className={`submit-btn ${getButtonClass()}`}
              disabled={createCashflow.isPending || isUpdating}
            >
              {createCashflow.isPending || isUpdating ? 'Đang lưu...' : (cashflowToEdit ? 'Cập Nhật Giao Dịch' : 'Lưu Giao Dịch')}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
