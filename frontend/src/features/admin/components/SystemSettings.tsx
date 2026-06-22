import { useState, useEffect } from 'react';
import { settingsApi } from '../api/settingsApi';
import { useNotification } from '../../../context/NotificationContext';
import { NumericFormat } from 'react-number-format';

export function SystemSettings() {
  const { showNotification } = useNotification();
  const [usdToVndRate, setUsdToVndRate] = useState<string>('');
  const [isUpdatingRate, setIsUpdatingRate] = useState<boolean>(false);

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    const rate = await settingsApi.getSetting('USD_TO_VND');
    if (rate) setUsdToVndRate(rate);
  };

  const handleUpdateRate = async () => {
    if (!usdToVndRate) return;
    setIsUpdatingRate(true);
    const success = await settingsApi.updateSetting('USD_TO_VND', usdToVndRate);
    setIsUpdatingRate(false);
    if (success) {
      showNotification('Cập nhật tỷ giá thành công!', 'success');
    } else {
      showNotification('Có lỗi xảy ra khi cập nhật tỷ giá.', 'error');
    }
  };

  return (
    <div className="admin-page-container">
      <div className="admin-page-header">
        <h2>System Settings</h2>
        <p className="admin-page-subtitle">Configure global application settings and exchange rates.</p>
      </div>

      <div className="admin-card">
        <div style={{ marginBottom: '1.5rem' }}>
          <h3 style={{ margin: 0, fontSize: '1.1rem', color: '#e2e8f0' }}>Exchange Rate Configuration</h3>
          <p style={{ margin: '0.5rem 0 0 0', color: '#94a3b8', fontSize: '0.9rem' }}>Set the global conversion rate for USD to VND.</p>
        </div>
        
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', maxWidth: '400px' }}>
          <label style={{ fontSize: '0.9rem', color: '#cbd5e1', fontWeight: 500 }}>USD to VND Rate</label>
          <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
            <NumericFormat 
              className="modern-input"
              value={usdToVndRate} 
              onValueChange={(values) => setUsdToVndRate(values.value)}
              placeholder="e.g., 25,400"
              style={{ flex: 1, padding: '0.8rem 1rem', background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.1)', borderRadius: '12px', color: '#fff', fontSize: '1rem' }}
              thousandSeparator="."
              decimalSeparator=","
              allowNegative={false}
            />
            <button 
              className="btn btn-primary" 
              onClick={handleUpdateRate}
              disabled={isUpdatingRate}
              style={{ padding: '0.8rem 1.5rem', background: 'linear-gradient(135deg, #a78bfa, #8b5cf6)', border: 'none', borderRadius: '12px', color: '#fff', fontWeight: 600, cursor: 'pointer', whiteSpace: 'nowrap', transition: 'all 0.2s ease', opacity: isUpdatingRate ? 0.7 : 1 }}
            >
              {isUpdatingRate ? 'Updating...' : 'Save Settings'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
