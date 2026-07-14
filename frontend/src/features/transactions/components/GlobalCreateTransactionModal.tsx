import React, { useState, useEffect } from 'react';
import { createTransaction } from '../api/transactionApi';
import { getPortfolios, getPortfolioSummary } from '../../portfolios/api/portfolioApi';
import type { PortfolioDto, AssetSummaryDto } from '../../portfolios/types';
import { TransactionType } from '../types';
import { useNotification } from '../../../context/NotificationContext';
import { NumericFormat } from 'react-number-format';
import './GlobalCreateTransactionModal.css';

interface GlobalCreateTransactionModalProps {
  initialPortfolioId?: string;
  onClose: () => void;
  onSuccess: () => void;
}

export const GlobalCreateTransactionModal: React.FC<GlobalCreateTransactionModalProps> = ({ initialPortfolioId, onClose, onSuccess }) => {
  const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
  const [assets, setAssets] = useState<AssetSummaryDto[]>([]);
  
  const [selectedPortfolioId, setSelectedPortfolioId] = useState(initialPortfolioId || '');
  const [selectedCategoryName, setSelectedCategoryName] = useState('');
  const [selectedAssetId, setSelectedAssetId] = useState('');
  
  const availableCategories = Array.from(new Set(assets.map(a => a.categoryName)));
  const filteredAssets = selectedCategoryName 
    ? assets.filter(a => a.categoryName === selectedCategoryName)
    : assets;
  
  const [type, setType] = useState<number>(TransactionType.Buy);
  const [quantity, setQuantity] = useState('');
  const [price, setPrice] = useState('');
  const [date, setDate] = useState(new Date().toISOString().slice(0, 16));
  
  const [loading, setLoading] = useState(false);
  const { showNotification } = useNotification();

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
        const summary = await getPortfolioSummary(selectedPortfolioId);
        setAssets(summary.assets);
        setSelectedCategoryName('');
        setSelectedAssetId('');
      } catch (err) {
        showNotification('Failed to load assets for portfolio', 'error');
      }
    };
    fetchAssets();
  }, [selectedPortfolioId]);

  useEffect(() => {
    setSelectedAssetId('');
  }, [selectedCategoryName]);

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
        timestamp: new Date(date).toISOString(),
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
              onChange={e => setSelectedCategoryName(e.target.value)}
              className="glass-input"
              disabled={!selectedPortfolioId || availableCategories.length === 0}
            >
              <option value="">{availableCategories.length === 0 && selectedPortfolioId ? 'No categories in portfolio' : 'Select Category'}</option>
              {availableCategories.map(cat => (
                <option key={cat} value={cat}>{cat}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Asset</label>
            <select 
              value={selectedAssetId} 
              onChange={e => setSelectedAssetId(e.target.value)}
              required
              className="glass-input"
              disabled={!selectedCategoryName || filteredAssets.length === 0}
            >
              <option value="">{filteredAssets.length === 0 && selectedCategoryName ? 'No assets in this category' : 'Select Asset'}</option>
              {filteredAssets.map(a => (
                <option key={a.assetId} value={a.assetId}>{a.symbol} - {a.name}</option>
              ))}
            </select>
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
            <select value={type} onChange={e => setType(Number(e.target.value))} className="glass-input">
              {selectedCategoryName === 'Fiat' ? (
                <>
                  <option value={TransactionType.Deposit}>Deposit</option>
                  <option value={TransactionType.Withdrawal}>Withdrawal</option>
                </>
              ) : (
                <>
                  <option value={TransactionType.Buy}>Buy</option>
                  <option value={TransactionType.Sell}>Sell</option>
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
              <label>{type === TransactionType.Dividend ? 'Dividend per unit' : 'Price'}</label>
              <NumericFormat
                value={price} 
                onValueChange={(values) => setPrice(values.value)}
                required
                className="glass-input"
                thousandSeparator="."
                decimalSeparator=","
                allowNegative={false}
              />
            </div>
          </div>

          <div className="form-group">
            <label>Date & Time</label>
            <input 
              type="datetime-local" 
              value={date} 
              onChange={e => setDate(e.target.value)}
              required
              className="glass-input"
            />
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
