import { useState, useEffect } from 'react';
import ReactDOM from 'react-dom';
import { useNotification } from '../../../context/NotificationContext';
import { marketAssetsApi } from '../api/marketAssets';
import type { AssetCategory, MarketAsset, KbsInstrument } from '../types';
import { NumericFormat } from 'react-number-format';

interface MarketAssetModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSaved: () => void;
  assetToEdit: MarketAsset | null;
  categories: AssetCategory[];
  defaultCategoryId?: string;
}

export function MarketAssetModal({ isOpen, onClose, onSaved, assetToEdit, categories, defaultCategoryId }: MarketAssetModalProps) {
  const { showNotification } = useNotification();
  
  const [categoryId, setCategoryId] = useState('');
  const [symbol, setSymbol] = useState('');
  const [name, setName] = useState('');
  const [price, setPrice] = useState('');
  const [priceSource, setPriceSource] = useState('Manual');
  const [externalId, setExternalId] = useState('');
  const [isFetchingPrice, setIsFetchingPrice] = useState(false);

  const [instrumentSuggestions, setInstrumentSuggestions] = useState<KbsInstrument[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [preventSearch, setPreventSearch] = useState(false);

  useEffect(() => {
    if (isOpen) {
      if (assetToEdit) {
        setCategoryId(assetToEdit.categoryId);
        setSymbol(assetToEdit.symbol);
        setName(assetToEdit.name);
        setPrice(assetToEdit.currentPrice.toString());
        setPriceSource(assetToEdit.priceSource || 'Manual');
        setExternalId(assetToEdit.externalId || '');
      } else {
        setCategoryId(defaultCategoryId || '');
        setSymbol('');
        setName('');
        setPrice('');
        setPriceSource('Manual');
        setExternalId('');
      }
      setInstrumentSuggestions([]);
      setShowSuggestions(false);
      setPreventSearch(false);
    }
  }, [isOpen, assetToEdit, defaultCategoryId]);

  const isCryptoCategory = () => {
    const cat = categories.find(c => c.id === categoryId);
    return cat && (cat.name.toLowerCase().includes('crypto') || cat.name.toLowerCase().includes('coin'));
  };

  const isKbsCategory = (() => {
    const cat = categories.find(c => c.id === categoryId);
    if (!cat) return false;
    const name = cat.name
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '')
      .replace(/đ/g, 'd')
      .toLowerCase();
    return name.includes('stock')
      || name.includes('co phieu')
      || name.includes('chung khoan')
      || name.includes('etf');
  })();

  useEffect(() => {
    if (!isOpen || !isKbsCategory || !symbol || symbol.length < 2 || preventSearch) {
      setInstrumentSuggestions([]);
      setShowSuggestions(false);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        const results = await marketAssetsApi.searchKbsInstruments(symbol);
        if (!preventSearch) {
          setInstrumentSuggestions(results || []);
          setShowSuggestions(true);
        }
      } catch (e) {
        console.error('Failed to search instruments', e);
      }
    }, 500);

    return () => clearTimeout(timer);
  }, [symbol, categoryId, preventSearch, isOpen, isKbsCategory]);

  if (!isOpen) return null;

  const handleSymbolChange = (val: string) => {
    setPreventSearch(false);
    setSymbol(val);
  };

  const handleSelectSuggestion = (inst: KbsInstrument) => {
    setPreventSearch(true);
    setSymbol(inst.symbol);
    setName(`${inst.shortName || inst.name}`);
    setShowSuggestions(false);
  };

  const fetchCoinGeckoPrice = async () => {
    if (!name) {
      showNotification('Please enter the Asset Full Name first (e.g. bitcoin, ethereum)', 'info');
      return;
    }
    
    try {
      setIsFetchingPrice(true);
      const coinId = name.trim().toLowerCase().replace(/\s+/g, '-');
      const data = await marketAssetsApi.fetchCoinGeckoPrice(coinId);
      
      if (data && typeof data.price === 'number' && data.price > 0) {
        setPrice(data.price.toString());
        setPriceSource('CoinGecko');
        setExternalId(coinId);
        showNotification(`Đã lấy giá CoinGecko: $${data.price}`, 'success');
      } else {
        showNotification(`Không tìm thấy giá cho ID "${coinId}". Hãy đảm bảo Full Name khớp với CoinGecko ID.`, 'error');
      }
    } catch (error) {
      console.error('CoinGecko API error', error);
      showNotification('Lỗi kết nối tới Backend để lấy giá CoinGecko', 'error');
    } finally {
      setIsFetchingPrice(false);
    }
  };

  const fetchKbsPrice = async () => {
    if (!symbol) {
      showNotification('Please enter the Symbol first (e.g. HPG)', 'info');
      return;
    }
    
    try {
      setIsFetchingPrice(true);
      const symbolUpper = symbol.trim().toUpperCase();
      const data = await marketAssetsApi.fetchKbsPrice(symbolUpper);
      
      if (data && data.price) {
        setPrice(data.price.toString());
        setPriceSource('KBS');
        setExternalId(symbolUpper);
        
        // Auto-fill the asset name from the cached KBS instrument catalog.
        try {
          const instruments = await marketAssetsApi.searchKbsInstruments(symbolUpper);
          const exactMatch = instruments?.find(i => i.symbol.toUpperCase() === symbolUpper);
          if (exactMatch) {
            setName(`${exactMatch.shortName || exactMatch.name}`);
            showNotification(`Đã lấy giá và tên từ KBS: ${data.price}`, 'success');
          } else {
            showNotification(`Đã lấy giá KBS: ${data.price}`, 'success');
          }
        } catch (searchErr) {
          console.error('Failed to search instrument for name filling', searchErr);
          showNotification(`Đã lấy giá KBS: ${data.price}`, 'success');
        }
      } else {
        showNotification(`Không tìm thấy giá cho Symbol "${symbol}".`, 'error');
      }
    } catch (error) {
      console.error('KBS API error', error);
      showNotification(error instanceof Error ? error.message : 'Lỗi kết nối tới Backend để lấy giá KBS', 'error');
    } finally {
      setIsFetchingPrice(false);
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!categoryId) {
      showNotification('Vui lòng chọn Category!', 'info');
      return;
    }
    try {
      const payload = {
        categoryId,
        symbol,
        name,
        currentPrice: Number(price),
        priceSource,
        externalId: externalId.trim() || null
      };

      if (assetToEdit) {
        await marketAssetsApi.updateMarketAsset(assetToEdit.id, payload);
        showNotification('Cập nhật Market Asset thành công', 'success');
      } else {
        await marketAssetsApi.createMarketAsset(payload);
        showNotification('Thêm Market Asset thành công', 'success');
      }
      onSaved();
      onClose();
    } catch (error) {
      console.error('Failed to save market asset', error);
      showNotification('Đã xảy ra lỗi khi lưu Market Asset', 'error');
    }
  };

  const modalContent = (
    <div className="modal-overlay">
      <div className="modal-content admin-modal" style={{ maxWidth: '500px' }}>
        <div className="modal-header">
          <h2>{assetToEdit ? 'Edit Market Asset' : 'Add New Market Asset'}</h2>
          <button type="button" className="close-btn" onClick={onClose}>&times;</button>
        </div>
        
        <form onSubmit={handleSave} className="admin-form" style={{ marginTop: '1rem' }}>
          <div className="admin-form-group">
            <label>Target Category</label>
            <select
              required
              value={categoryId}
              onChange={e => setCategoryId(e.target.value)}
              className="admin-input admin-select"
            >
              <option value="">-- Select Category --</option>
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
          
          <div className="grid-cols-2" style={{ gap: '1rem' }}>
            <div className="admin-form-group">
              <label>Symbol</label>
              <div className="autocomplete-container">
                <input
                  type="text"
                  required
                  value={symbol}
                  onChange={e => handleSymbolChange(e.target.value)}
                  onBlur={() => setTimeout(() => setShowSuggestions(false), 200)}
                  className="admin-input"
                  placeholder="e.g. AAPL or HPG"
                  style={{ width: '100%' }}
                />
                {showSuggestions && instrumentSuggestions.length > 0 && (
                  <div className="autocomplete-dropdown">
                    {instrumentSuggestions.map(inst => (
                      <div 
                        key={`${inst.marketId}-${inst.symbol}`} 
                        className="autocomplete-item"
                        onMouseDown={() => handleSelectSuggestion(inst)}
                      >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <span className="autocomplete-symbol">{inst.symbol}</span>
                          <span className="admin-badge" style={{ fontSize: '0.65rem' }}>{inst.marketId}</span>
                        </div>
                        <span className="autocomplete-name">{inst.name}</span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
            
            <div className="admin-form-group">
              <label style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span>Current Price</span>
              </label>
              <NumericFormat
                required
                value={price}
                onValueChange={(values) => setPrice(values.value)}
                className="admin-input"
                placeholder="e.g. 150.00"
                thousandSeparator="."
                decimalSeparator=","
                allowNegative={false}
              />
            </div>
          </div>
          
          <div className="admin-form-group" style={{ marginBottom: '0.5rem' }}>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
                {isCryptoCategory() && (
                  <button 
                    type="button" 
                    onClick={fetchCoinGeckoPrice}
                    disabled={isFetchingPrice}
                    style={{ 
                      background: 'rgba(16, 185, 129, 0.1)', border: '1px solid rgba(16, 185, 129, 0.3)', color: '#10b981', 
                      cursor: isFetchingPrice ? 'wait' : 'pointer', fontSize: '0.8rem', padding: '0.5rem', borderRadius: '6px', fontWeight: 600, opacity: isFetchingPrice ? 0.6 : 1, flex: 1
                    }}
                  >
                    {isFetchingPrice ? 'Fetching...' : '⚡ Fetch CoinGecko Price'}
                  </button>
                )}
                {isKbsCategory && (
                  <button 
                    type="button" 
                    onClick={fetchKbsPrice}
                    disabled={isFetchingPrice}
                    style={{ 
                      background: 'rgba(59, 130, 246, 0.1)', border: '1px solid rgba(59, 130, 246, 0.3)', color: '#3b82f6', 
                      cursor: isFetchingPrice ? 'wait' : 'pointer', fontSize: '0.8rem', padding: '0.5rem', borderRadius: '6px', fontWeight: 600, opacity: isFetchingPrice ? 0.6 : 1, flex: 1
                    }}
                  >
                    {isFetchingPrice ? 'Fetching...' : '⚡ Fetch KBS Price'}
                  </button>
                )}
            </div>
          </div>

          <div className="grid-cols-2" style={{ gap: '1rem' }}>
            <div className="admin-form-group">
              <label>Price Source</label>
              <select
                value={priceSource}
                onChange={e => setPriceSource(e.target.value)}
                className="admin-input admin-select"
              >
                <option value="Manual">Manual</option>
                <option value="KBS">KBS</option>
                <option value="CoinGecko">CoinGecko</option>
              </select>
            </div>
            <div className="admin-form-group">
              <label>External ID</label>
              <input
                type="text"
                value={externalId}
                onChange={e => setExternalId(e.target.value)}
                className="admin-input"
                placeholder="HPG or bitcoin"
              />
            </div>
          </div>
          
          <div className="admin-form-group">
            <label>Asset Full Name</label>
            <input
              type="text"
              required
              value={name}
              onChange={e => setName(e.target.value)}
              className="admin-input"
              placeholder="e.g. Apple Inc."
            />
          </div>
          
          <div className="modal-actions" style={{ marginTop: '1.5rem', display: 'flex', gap: '1rem', justifyContent: 'flex-end' }}>
            <button type="button" onClick={onClose} className="btn btn-outline">Cancel</button>
            <button type="submit" className="btn btn-primary glow-effect">
              {assetToEdit ? '💾 Save Changes' : '✨ Add Market Asset'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );

  return ReactDOM.createPortal(modalContent, document.body);
}
