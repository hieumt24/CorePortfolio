import { useState, useEffect } from 'react';
import { settingsApi } from '../api/settingsApi';
import { useNotification } from '../../../context/NotificationContext';

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
    <section className="admin-card settings-section">
      <div className="admin-card-header">
        <h2>System Settings</h2>
        <p>Configure global application settings.</p>
      </div>
      <div className="admin-card-body">
        <div className="admin-form-group">
          <label>Exchange Rate (USD to VND)</label>
          <div style={{ display: 'flex', gap: '1rem' }}>
            <input 
              type="number" 
              className="admin-input"
              value={usdToVndRate} 
              onChange={(e) => setUsdToVndRate(e.target.value)}
              placeholder="e.g., 26309"
              style={{ flex: 1 }}
            />
            <button 
              className="admin-btn" 
              onClick={handleUpdateRate}
              disabled={isUpdatingRate}
              style={{ width: 'auto' }}
            >
              {isUpdatingRate ? 'Updating...' : 'Update Rate'}
            </button>
          </div>
        </div>
      </div>
    </section>
  );
}
