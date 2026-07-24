import React, { useState, useEffect } from 'react';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';
import { createTransaction } from '../api/transactionApi';
import { getPortfolios, getPortfolioSummary } from '../../portfolios/api/portfolioApi';
import type { PortfolioDto, AssetSummaryDto } from '../../portfolios/types';
import { TransactionType } from '../types';
import { useNotification } from '../../../context/NotificationContext';
import { NumericFormat } from 'react-number-format';
import { isCryptoCategory } from '../utils/assetCategory';
import { calculateCashImpact } from '../utils/transactionImpact';
import './GlobalCreateTransactionModal.css';

interface GlobalCreateTransactionModalProps {
  initialPortfolioId?: string;
  initialCategory?: string;
  onClose: () => void;
  onSuccess: () => void;
}

interface DateTimeTriggerProps {
  value?: string;
  onClick?: () => void;
}

const DateTimeTrigger = React.forwardRef<HTMLButtonElement, DateTimeTriggerProps>(
  ({ value, onClick }, ref) => (
    <button
      ref={ref}
      type="button"
      className="date-time-trigger"
      onClick={onClick}
      aria-label={`Chọn ngày và giờ giao dịch. Hiện tại: ${value}`}
    >
      <span className="date-time-icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none">
          <path d="M7 3v3M17 3v3M4.5 9.5h15M6 5h12a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Z" />
          <path d="M12 13v3l2 1" />
        </svg>
      </span>
      <span className="date-time-copy">
        <small>Thời điểm ghi nhận</small>
        <strong>{value}</strong>
      </span>
      <span className="date-time-chevron" aria-hidden="true">⌄</span>
    </button>
  ),
);
DateTimeTrigger.displayName = 'DateTimeTrigger';

export const GlobalCreateTransactionModal: React.FC<GlobalCreateTransactionModalProps> = ({ initialPortfolioId, initialCategory, onClose, onSuccess }) => {
  const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
  const [assets, setAssets] = useState<AssetSummaryDto[]>([]);
  
  const [selectedPortfolioId, setSelectedPortfolioId] = useState(initialPortfolioId || '');
  const [selectedCategoryName, setSelectedCategoryName] = useState('');
  const [selectedAssetId, setSelectedAssetId] = useState('');
  
  const availableCategories = Array.from(new Set(assets.map(a => a.categoryName)));
  const filteredAssets = selectedCategoryName 
    ? assets.filter(a => a.categoryName === selectedCategoryName)
    : assets;
  const cryptoCategorySelected = isCryptoCategory(selectedCategoryName);
  const selectedAsset = assets.find(asset => asset.assetId === selectedAssetId);
  
  const [type, setType] = useState<number>(TransactionType.Buy);
  const [quantity, setQuantity] = useState('');
  const [price, setPrice] = useState('');
  const [fee, setFee] = useState('0');
  const [date, setDate] = useState<Date>(new Date());
  
  const [loading, setLoading] = useState(false);
  const [assetsLoading, setAssetsLoading] = useState(false);
  const [categoryLoading, setCategoryLoading] = useState(false);
  const { showNotification } = useNotification();

  const quantityValue = Number(quantity) || 0;
  const priceValue = Number(price) || 0;
  const feeValue = Number(fee) || 0;
  const currency = selectedAsset?.currency || 'VND';
  const cashImpact = calculateCashImpact(type as TransactionType, quantityValue, priceValue, feeValue);
  const amountSummary = (() => {
    if (type === TransactionType.Buy) return { label: 'Tổng tiền đã thanh toán', hint: 'Giá trị giao dịch + phí' };
    if (type === TransactionType.Sell || type === TransactionType.Dividend) return { label: 'Tổng tiền nhận về', hint: 'Giá trị giao dịch − phí' };
    if (type === TransactionType.Deposit) return { label: 'Tổng tiền nạp', hint: 'Số tiền cộng vào tài khoản' };
    if (type === TransactionType.Withdrawal) return { label: 'Tổng tiền rút', hint: 'Số tiền trừ khỏi tài khoản' };
    return { label: 'Phí ghi nhận', hint: 'Reward không phát sinh chi phí mua' };
  })();
  const formattedCashImpact = new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
  }).format(Math.abs(cashImpact));

  useEffect(() => {
    const fetchPorts = async () => {
      try {
        const ports = await getPortfolios();
        setPortfolios(ports);
      } catch (err) {
        showNotification('Failed to load portfolios', 'error');
      }
    };
    fetchPorts();
  }, []);

  useEffect(() => {
    if (!selectedPortfolioId) {
      setAssets([]);
      setSelectedAssetId('');
      return;
    }

    const fetchAssets = async () => {
      try {
        setAssetsLoading(true);
        const summary = await getPortfolioSummary(selectedPortfolioId);
        setAssets(summary.assets);
        const categories = Array.from(new Set(summary.assets.map(a => a.categoryName)));
        const preferred = initialCategory && categories.find(category => category.toLowerCase().includes(initialCategory.toLowerCase()));
        setSelectedCategoryName(preferred ?? '');
        setSelectedAssetId('');
      } catch (err) {
        showNotification('Failed to load assets for portfolio', 'error');
      } finally {
        setAssetsLoading(false);
      }
    };
    fetchAssets();
  }, [selectedPortfolioId, initialCategory]);

  useEffect(() => {
    setSelectedAssetId('');
    if (type === TransactionType.Earn && !cryptoCategorySelected) {
      setType(TransactionType.Buy);
      setPrice('');
    }
  }, [selectedCategoryName, type, cryptoCategorySelected]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPortfolioId || !selectedAssetId || !quantity || !price || !date) return;

    try {
      setLoading(true);
      await createTransaction({
        portfolioId: selectedPortfolioId,
        assetId: selectedAssetId,
        type: type as TransactionType,
        quantity: Number(quantity),
        price: Number(price),
        fee: feeValue,
        timestamp: date.toISOString(),
      });
      onSuccess();
    } catch (error) {
      showNotification('Failed to create transaction', 'error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content glass-panel">
        <div className="modal-header">
          <h2>Add Transaction</h2>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>
        <form onSubmit={handleSubmit} className="modal-body">
          <div className="form-group">
            <label>Portfolio</label>
            <select 
              value={selectedPortfolioId} 
              onChange={e => setSelectedPortfolioId(e.target.value)}
              required
              className="glass-input"
              disabled={!!initialPortfolioId}
            >
              <option value="">Select Portfolio</option>
              {portfolios.map(p => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Category</label>
            <select 
              value={selectedCategoryName} 
              onChange={e => {
                setCategoryLoading(true);
                setSelectedCategoryName(e.target.value);
                window.setTimeout(() => setCategoryLoading(false), 220);
              }}
              className="glass-input"
              disabled={!selectedPortfolioId || availableCategories.length === 0 || assetsLoading}
            >
              <option value="">{availableCategories.length === 0 && selectedPortfolioId ? 'No categories in portfolio' : 'Select Category'}</option>
              {availableCategories.map(cat => (
                <option key={cat} value={cat}>{cat}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Asset</label>
            <div className="asset-select-wrap">
            <select 
              value={selectedAssetId} 
              onChange={e => setSelectedAssetId(e.target.value)}
              required
              className="glass-input"
              disabled={assetsLoading || categoryLoading || !selectedCategoryName || filteredAssets.length === 0}
            >
              <option value="">{assetsLoading ? 'Loading assets…' : categoryLoading ? 'Loading category…' : filteredAssets.length === 0 && selectedCategoryName ? 'No assets in this category' : 'Select Asset'}</option>
              {filteredAssets.map(a => (
                <option key={a.assetId} value={a.assetId}>{a.symbol} - {a.name}</option>
              ))}
            </select>
            {(assetsLoading || categoryLoading) && <span className="field-spinner" aria-label="Loading" />}
            </div>
            {selectedPortfolioId && !selectedCategoryName && availableCategories.length > 0 && (
              <small style={{ color: '#94a3b8', marginTop: '0.5rem', display: 'block' }}>
                Please select a category first.
              </small>
            )}
            {selectedPortfolioId && availableCategories.length === 0 && (
              <small style={{ color: '#ef4444', marginTop: '0.5rem', display: 'block' }}>
                You must add an asset to this portfolio first.
              </small>
            )}
          </div>

          <div className="form-group">
            <label>Type</label>
            <select value={type} onChange={e => {
              const nextType = Number(e.target.value);
              setType(nextType);
              if (nextType === TransactionType.Earn) setPrice('0');
            }} className="glass-input">
              {selectedCategoryName === 'Fiat' ? (
                <>
                  <option value={TransactionType.Deposit}>Deposit</option>
                  <option value={TransactionType.Withdrawal}>Withdrawal</option>
                </>
              ) : (
                <>
                  <option value={TransactionType.Buy}>Buy</option>
                  <option value={TransactionType.Sell}>Sell</option>
                  {cryptoCategorySelected && <option value={TransactionType.Earn}>Earn / Reward</option>}
                  <option value={TransactionType.Dividend}>Dividend</option>
                </>
              )}
            </select>
          </div>

          <div className="form-row" style={{ display: 'flex', gap: '1rem' }}>
            <div className="form-group" style={{ flex: 1 }}>
              <label>Quantity</label>
              <NumericFormat
                value={quantity} 
                onValueChange={(values) => setQuantity(values.value)}
                required
                className="glass-input"
                thousandSeparator="."
                decimalSeparator=","
                allowNegative={false}
              />
            </div>
            <div className="form-group" style={{ flex: 1 }}>
              <label>{type === TransactionType.Dividend ? 'Dividend per unit' : type === TransactionType.Earn ? 'Acquisition cost' : 'Price'}</label>
              <NumericFormat
                value={price} 
                onValueChange={(values) => setPrice(values.value)}
                required
                className="glass-input"
                thousandSeparator="."
                decimalSeparator=","
                allowNegative={false}
                disabled={type === TransactionType.Earn}
              />
              {type === TransactionType.Earn && <small className="field-hint">Rewards add quantity at zero cost and do not create a purchase cash flow.</small>}
            </div>
          </div>

          <div className="form-row transaction-details-row">
            <div className="form-group transaction-fee-field">
              <label htmlFor="transaction-fee">Phí giao dịch</label>
              <NumericFormat
                id="transaction-fee"
                value={fee}
                onValueChange={(values) => setFee(values.value)}
                className="glass-input"
                thousandSeparator="."
                decimalSeparator=","
                allowNegative={false}
                disabled={loading}
              />
            </div>
            <div className="form-group transaction-date-field">
              <label>Ngày & giờ</label>
              <DatePicker
                selected={date}
                onChange={(value: Date | null) => value && setDate(value)}
                showTimeSelect
                timeIntervals={5}
                timeCaption="Giờ"
                dateFormat="dd/MM/yyyy 'lúc' HH:mm"
                timeFormat="HH:mm"
                calendarStartDay={1}
                popperPlacement="bottom-end"
                calendarClassName="transaction-calendar"
                popperClassName="transaction-calendar-popper"
                customInput={<DateTimeTrigger />}
              />
            </div>
          </div>

          <div className={`transaction-total-card ${cashImpact > 0 ? 'positive' : ''}`} aria-live="polite">
            <div className="transaction-total-icon" aria-hidden="true">
              <span>{cashImpact > 0 ? '↙' : '↗'}</span>
            </div>
            <div className="transaction-total-copy">
              <span>{amountSummary.label}</span>
              <small>{amountSummary.hint}</small>
            </div>
            <strong>{formattedCashImpact}</strong>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading || !selectedAssetId}>
              {loading ? 'Saving...' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
