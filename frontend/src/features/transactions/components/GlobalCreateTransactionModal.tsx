import React, { useRef, useState, useEffect } from 'react';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';
import { createTransaction } from '../api/transactionApi';
import { getPortfolios } from '../../portfolios/api/portfolioApi';
import type { PortfolioDto } from '../../portfolios/types';
import { searchAvailableMarketAssets } from '../../assets/api/assetApi';
import type { AvailableMarketAsset } from '../../assets/types';
import { categoriesApi } from '../../admin/api/categories';
import type { AssetCategory } from '../../admin/types';
import { TransactionType } from '../types';
import { useNotification } from '../../../context/NotificationContext';
import { NumericFormat } from 'react-number-format';
import { isCryptoCategory } from '../utils/assetCategory';
import { calculateCashImpact } from '../utils/transactionImpact';
import {
  deriveTransactionAmount,
  type TransactionAmountField,
} from '../utils/transactionAmount';
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

const toInputValue = (value: number) => {
  if (!Number.isFinite(value) || value < 0) return '';
  return String(Number(value.toPrecision(12)));
};

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
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [assets, setAssets] = useState<AvailableMarketAsset[]>([]);
  
  const [selectedPortfolioId, setSelectedPortfolioId] = useState(initialPortfolioId || '');
  const [selectedCategoryId, setSelectedCategoryId] = useState('');
  const [selectedMarketAssetId, setSelectedMarketAssetId] = useState('');
  const [assetSearch, setAssetSearch] = useState('');
  
  const selectedAsset = assets.find(asset => asset.id === selectedMarketAssetId);
  const selectedCategoryName = selectedAsset?.categoryName
    ?? categories.find(category => category.id === selectedCategoryId)?.name
    ?? '';
  const cryptoCategorySelected = isCryptoCategory(selectedCategoryName);
  
  const [type, setType] = useState<number>(TransactionType.Buy);
  const [quantity, setQuantity] = useState('');
  const [price, setPrice] = useState('');
  const [total, setTotal] = useState('');
  const [fee, setFee] = useState('0');
  const [currency, setCurrency] = useState<'VND' | 'USD'>('VND');
  const [date, setDate] = useState<Date>(new Date());
  const amountInputOrder = useRef<TransactionAmountField[]>([]);
  
  const [loading, setLoading] = useState(false);
  const [assetsLoading, setAssetsLoading] = useState(false);
  const [categoryLoading, setCategoryLoading] = useState(false);
  const [assetsError, setAssetsError] = useState('');
  const { showNotification } = useNotification();

  const quantityValue = Number(quantity) || 0;
  const priceValue = Number(price) || 0;
  const feeValue = Number(fee) || 0;
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
    categoriesApi.getCategories()
      .then(data => {
        setCategories(data || []);
        if (initialCategory) {
          const normalizedInitialCategory = initialCategory.toLowerCase();
          const preferred = data.find(category =>
            category.name.toLowerCase().includes(normalizedInitialCategory));
          setSelectedCategoryId(preferred?.id ?? '');
        }
      })
      .catch(() => showNotification('Failed to load asset categories', 'error'));
  }, [initialCategory]);

  useEffect(() => {
    if (!selectedPortfolioId) {
      setAssets([]);
      setSelectedMarketAssetId('');
      return;
    }

    const timer = window.setTimeout(() => {
      const fetchAssets = async () => {
        try {
          setAssetsLoading(true);
          setAssetsError('');
          const results = await searchAvailableMarketAssets(selectedPortfolioId, {
            search: assetSearch.trim() || undefined,
            categoryId: selectedCategoryId || undefined,
            limit: 50,
            includeExisting: true,
          });
          setAssets(results);
          setSelectedMarketAssetId(current =>
            results.some(asset => asset.id === current) ? current : '');
        } catch (err) {
          setAssets([]);
          setAssetsError('Failed to load assets for portfolio');
        } finally {
          setAssetsLoading(false);
        }
      };
      fetchAssets();
    }, 250);

    return () => window.clearTimeout(timer);
  }, [selectedPortfolioId, selectedCategoryId, assetSearch]);

  useEffect(() => {
    setSelectedMarketAssetId('');
  }, [selectedCategoryId]);

  useEffect(() => {
    if (type === TransactionType.Earn && !cryptoCategorySelected) {
      setType(TransactionType.Buy);
      setPrice('');
    }
  }, [type, cryptoCategorySelected]);

  useEffect(() => {
    if (!selectedAsset) return;
    const assetCurrency = selectedAsset.currency?.toUpperCase();
    setCurrency(assetCurrency === 'USD' ? 'USD' : 'VND');
  }, [selectedAsset]);

  const updateAmountField = (field: TransactionAmountField, value: string) => {
    const setters = { quantity: setQuantity, price: setPrice, total: setTotal };
    setters[field](value);

    const nextOrder = [...amountInputOrder.current.filter(item => item !== field), field].slice(-2);
    amountInputOrder.current = nextOrder;
    if (nextOrder.length < 2) return;

    const values = {
      quantity: field === 'quantity' ? Number(value) : Number(quantity),
      price: field === 'price' ? Number(value) : Number(price),
      total: field === 'total' ? Number(value) : Number(total),
    };
    const [firstField, secondField] = nextOrder;
    if (values[firstField] <= 0 || values[secondField] <= 0) return;

    const derived = deriveTransactionAmount(values, nextOrder);
    if (derived.total !== undefined) setTotal(toInputValue(derived.total));
    if (derived.price !== undefined) setPrice(toInputValue(derived.price));
    if (derived.quantity !== undefined) setQuantity(toInputValue(derived.quantity));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPortfolioId || !selectedAsset || quantityValue <= 0 || priceValue < 0 || !date) return;

    try {
      setLoading(true);
      await createTransaction({
        portfolioId: selectedPortfolioId,
        assetId: selectedAsset.portfolioAssetId || undefined,
        marketAssetId: selectedAsset.portfolioAssetId ? undefined : selectedAsset.id,
        type: type as TransactionType,
        quantity: Number(quantity),
        price: Number(price),
        fee: feeValue,
        currency,
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
              value={selectedCategoryId}
              onChange={e => {
                setCategoryLoading(true);
                setSelectedCategoryId(e.target.value);
                window.setTimeout(() => setCategoryLoading(false), 220);
              }}
              className="glass-input"
              disabled={!selectedPortfolioId || categories.length === 0}
            >
              <option value="">All categories</option>
              {categories.map(category => (
                <option key={category.id} value={category.id}>{category.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="transaction-asset-search">Search asset</label>
            <input
              id="transaction-asset-search"
              type="search"
              value={assetSearch}
              onChange={event => {
                setAssetSearch(event.target.value);
                setSelectedMarketAssetId('');
              }}
              className="glass-input"
              placeholder="Symbol or asset name"
              disabled={!selectedPortfolioId || loading}
            />
          </div>

          <div className="form-group">
            <label>Asset</label>
            <div className="asset-select-wrap">
            <select 
              value={selectedMarketAssetId}
              onChange={e => {
                setSelectedMarketAssetId(e.target.value);
                setQuantity('');
                setPrice('');
                setTotal('');
                amountInputOrder.current = [];
              }}
              required
              className="glass-input"
              disabled={assetsLoading || categoryLoading || !selectedPortfolioId || assets.length === 0}
            >
              <option value="">{assetsLoading ? 'Loading assets…' : categoryLoading ? 'Loading category…' : assets.length === 0 ? 'No matching assets' : 'Select Asset'}</option>
              {assets.map(asset => (
                <option key={asset.id} value={asset.id}>
                  {asset.symbol} - {asset.name}{asset.portfolioAssetId ? ' (in portfolio)' : ''}
                </option>
              ))}
            </select>
            {(assetsLoading || categoryLoading) && <span className="field-spinner" aria-label="Loading" />}
            </div>
            {assetsError && (
              <small style={{ color: '#ef4444', marginTop: '0.5rem', display: 'block' }}>
                {assetsError}
              </small>
            )}
            {selectedAsset && !selectedAsset.portfolioAssetId && (
              <small className="field-hint">
                This asset will be added to the portfolio automatically when the transaction is saved.
              </small>
            )}
          </div>

          <div className="form-group">
            <label>Type</label>
            <select value={type} onChange={e => {
              const nextType = Number(e.target.value);
              setType(nextType);
              if (nextType === TransactionType.Earn) {
                setPrice('0');
                setTotal('0');
                amountInputOrder.current = ['quantity'];
              } else if (type === TransactionType.Earn) {
                setPrice('');
                setTotal('');
                amountInputOrder.current = [];
              }
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

          <div className="transaction-amount-grid">
            <div className="form-group">
              <label>Quantity</label>
              <NumericFormat
                value={quantity} 
                onValueChange={(values, sourceInfo) => {
                  if (sourceInfo.source === 'event') updateAmountField('quantity', values.value);
                }}
                className="glass-input"
                thousandSeparator="."
                decimalSeparator=","
                allowNegative={false}
                disabled={loading}
              />
            </div>
            <div className="form-group">
              <label>{type === TransactionType.Dividend ? 'Dividend per unit' : type === TransactionType.Earn ? 'Acquisition cost' : 'Price'}</label>
              <NumericFormat
                value={price} 
                onValueChange={(values, sourceInfo) => {
                  if (sourceInfo.source === 'event') updateAmountField('price', values.value);
                }}
                className="glass-input"
                thousandSeparator="."
                decimalSeparator=","
                allowNegative={false}
                disabled={loading || type === TransactionType.Earn}
              />
              {type === TransactionType.Earn && <small className="field-hint">Rewards add quantity at zero cost and do not create a purchase cash flow.</small>}
            </div>
            <div className="form-group transaction-total-input">
              <label>Transaction total</label>
              <div className="currency-input-group">
                <NumericFormat
                  value={total}
                  onValueChange={(values, sourceInfo) => {
                    if (sourceInfo.source === 'event') updateAmountField('total', values.value);
                  }}
                  className="glass-input"
                  thousandSeparator="."
                  decimalSeparator=","
                  allowNegative={false}
                  disabled={loading || type === TransactionType.Earn}
                  aria-describedby="transaction-total-hint"
                />
                <select
                  value={currency}
                  onChange={event => setCurrency(event.target.value as 'VND' | 'USD')}
                  className="glass-input currency-select"
                  disabled={loading}
                  aria-label="Transaction currency"
                >
                  <option value="VND">VND</option>
                  <option value="USD">USD</option>
                </select>
              </div>
              <small id="transaction-total-hint" className="field-hint">
                Enter any two of quantity, price, and total. The remaining value is calculated automatically.
              </small>
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
            <button
              type="submit"
              className="btn btn-primary"
              disabled={loading || !selectedAsset || quantityValue <= 0 || priceValue < 0}
            >
              {loading ? 'Saving...' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
