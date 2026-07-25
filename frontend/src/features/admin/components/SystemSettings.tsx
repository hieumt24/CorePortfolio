import { useEffect, useState } from 'react';
import { NumericFormat } from 'react-number-format';
import { useNotification } from '../../../context/NotificationContext';
import { settingsApi, type NavigationFeature } from '../api/settingsApi';
import './SystemSettings.css';

const navigationLabels: Record<string, string> = {
  NAV_DASHBOARD: 'Dashboard',
  NAV_PORTFOLIOS: 'My Portfolios',
  NAV_TRANSACTIONS: 'Transactions',
  NAV_REPORTS: 'Global Report',
  NAV_CASHFLOW: 'Cashflow',
  NAV_WATCHLIST: 'Watchlist',
  NAV_BUDGETS: 'Budgets',
  NAV_SAVING_GOALS: 'Mục tiêu tiết kiệm',
  NAV_ANALYTICS: 'Analytics',
  NAV_REBALANCING: 'Tái cân bằng',
  NAV_DCA_PLANS: 'Lịch DCA',
};

export function SystemSettings() {
  const { showNotification } = useNotification();
  const [usdToVndRate, setUsdToVndRate] = useState('');
  const [isUpdatingRate, setIsUpdatingRate] = useState(false);
  const [navigationFeatures, setNavigationFeatures] = useState<NavigationFeature[]>([]);
  const [isLoadingFeatures, setIsLoadingFeatures] = useState(true);
  const [updatingFeatureKey, setUpdatingFeatureKey] = useState<string | null>(null);
  const [featuresError, setFeaturesError] = useState(false);

  const loadSettings = async () => {
    const [rate, features] = await Promise.all([
      settingsApi.getSetting('USD_TO_VND'),
      settingsApi.getNavigationFeatures().catch(() => null),
    ]);

    if (rate) setUsdToVndRate(rate);
    if (features) {
      setNavigationFeatures(features);
      setFeaturesError(false);
    } else {
      setFeaturesError(true);
    }
    setIsLoadingFeatures(false);
  };

  useEffect(() => {
    loadSettings();
  }, []);

  const handleUpdateRate = async () => {
    if (!usdToVndRate) return;
    setIsUpdatingRate(true);
    const success = await settingsApi.updateSetting('USD_TO_VND', usdToVndRate);
    setIsUpdatingRate(false);
    showNotification(
      success ? 'Cập nhật tỷ giá thành công!' : 'Có lỗi xảy ra khi cập nhật tỷ giá.',
      success ? 'success' : 'error',
    );
  };

  const handleToggleFeature = async (feature: NavigationFeature) => {
    const nextValue = !feature.isEnabled;
    setUpdatingFeatureKey(feature.key);
    try {
      await settingsApi.updateNavigationFeature(feature.key, nextValue);
      setNavigationFeatures(current =>
        current.map(item => item.key === feature.key ? { ...item, isEnabled: nextValue } : item),
      );
      showNotification(
        `${nextValue ? 'Đã mở' : 'Đã đóng'} ${navigationLabels[feature.key] ?? feature.key} trên navbar.`,
        'success',
      );
    } catch {
      showNotification('Không thể cập nhật trạng thái tính năng.', 'error');
    } finally {
      setUpdatingFeatureKey(null);
    }
  };

  const retryFeatures = () => {
    setIsLoadingFeatures(true);
    setFeaturesError(false);
    loadSettings();
  };

  return (
    <div className="admin-page-container">
      <div className="admin-page-header">
        <h2>System Settings</h2>
        <p className="admin-page-subtitle">Configure global application settings and user navigation.</p>
      </div>

      <div className="admin-card">
        <div className="settings-section-heading">
          <div>
            <h3>Exchange Rate Configuration</h3>
            <p>Set the global conversion rate for USD to VND.</p>
          </div>
        </div>

        <div className="exchange-rate-form">
          <label htmlFor="usd-to-vnd-rate">USD to VND Rate</label>
          <div className="exchange-rate-actions">
            <NumericFormat
              id="usd-to-vnd-rate"
              className="modern-input"
              value={usdToVndRate}
              onValueChange={values => setUsdToVndRate(values.value)}
              placeholder="e.g., 25,400"
              thousandSeparator="."
              decimalSeparator=","
              allowNegative={false}
            />
            <button className="btn btn-primary" onClick={handleUpdateRate} disabled={isUpdatingRate}>
              {isUpdatingRate ? 'Updating...' : 'Save Settings'}
            </button>
          </div>
        </div>
      </div>

      <div className="admin-card">
        <div className="settings-section-heading">
          <div>
            <h3>Navbar Feature Access</h3>
            <p>Bật hoặc tắt các mục tính năng hiển thị trên thanh điều hướng của người dùng.</p>
          </div>
          <span className="feature-count">
            {navigationFeatures.filter(feature => feature.isEnabled).length}/{navigationFeatures.length} đang bật
          </span>
        </div>

        {isLoadingFeatures && <div className="settings-state">Đang tải cấu hình navbar...</div>}
        {!isLoadingFeatures && featuresError && (
          <div className="settings-state settings-error">
            <span>Không thể tải cấu hình navbar.</span>
            <button className="btn btn-outline" onClick={retryFeatures}>Thử lại</button>
          </div>
        )}
        {!isLoadingFeatures && !featuresError && (
          <div className="feature-toggle-grid">
            {navigationFeatures.map(feature => (
              <div className="feature-toggle-row" key={feature.key}>
                <div>
                  <strong>{navigationLabels[feature.key] ?? feature.key}</strong>
                  <small>{feature.isEnabled ? 'Đang hiển thị cho người dùng' : 'Đang ẩn khỏi navbar'}</small>
                </div>
                <button
                  type="button"
                  role="switch"
                  aria-checked={feature.isEnabled}
                  aria-label={`${feature.isEnabled ? 'Đóng' : 'Mở'} ${navigationLabels[feature.key] ?? feature.key}`}
                  className={`feature-switch ${feature.isEnabled ? 'enabled' : ''}`}
                  disabled={updatingFeatureKey === feature.key}
                  onClick={() => handleToggleFeature(feature)}
                >
                  <span />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
