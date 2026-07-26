import { useEffect, useMemo, useState } from 'react';
import { useNotification } from '../../../context/NotificationContext';
import { formatVietnamDate, formatVietnamDateTime } from '../../../shared/utils/dateTime';
import { watchlistApi } from '../api/watchlistApi';
import type { WatchlistDto } from '../types';
import { AddWatchlistModal } from './AddWatchlistModal';
import './WatchlistDashboard.css';

const formatMoney = (amount: number, currency: string) =>
  new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
  }).format(amount);

export function WatchlistPage() {
  const { showNotification } = useNotification();
  const [items, setItems] = useState<WatchlistDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('all');
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [removingId, setRemovingId] = useState<string | null>(null);

  const loadItems = async () => {
    try {
      setLoading(true);
      setError(null);
      setItems(await watchlistApi.getWatchlist());
    } catch {
      setError('Không thể tải danh sách theo dõi.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let active = true;
    watchlistApi.getWatchlist()
      .then(result => {
        if (active) setItems(result);
      })
      .catch(() => {
        if (active) setError('Không thể tải danh sách theo dõi.');
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  const categories = useMemo(
    () => [...new Set(items.map(item => item.assetCategoryName).filter(Boolean))].sort(),
    [items],
  );
  const filteredItems = useMemo(() => {
    const query = search.trim().toLocaleLowerCase('vi-VN');
    return items.filter(item =>
      (category === 'all' || item.assetCategoryName === category)
      && (!query
        || item.symbol.toLocaleLowerCase('vi-VN').includes(query)
        || item.name.toLocaleLowerCase('vi-VN').includes(query)),
    );
  }, [items, search, category]);

  const targetCount = items.filter(item => item.targetPrice && item.targetPrice > 0).length;
  const staleCount = items.filter(item =>
    ['stale', 'error'].includes(item.priceStatus.toLocaleLowerCase('en-US')),
  ).length;

  const removeItem = async (item: WatchlistDto) => {
    try {
      setRemovingId(item.id);
      await watchlistApi.removeFromWatchlist(item.id);
      setItems(current => current.filter(candidate => candidate.id !== item.id));
      showNotification(`Đã xóa ${item.symbol} khỏi danh sách theo dõi`, 'success');
    } catch {
      showNotification('Không thể xóa tài sản lúc này', 'error');
    } finally {
      setRemovingId(null);
    }
  };

  return (
    <main className="watchlist-page container">
      <header className="watchlist-hero">
        <div className="watchlist-heading">
          <span className="watchlist-kicker">RADAR TÀI SẢN</span>
          <h1>Theo dõi</h1>
          <p>Lưu những tài sản bạn quan tâm và theo dõi giá mục tiêu trong một góc nhìn tập trung.</p>
        </div>
        <div className="watchlist-pulse glass-panel" aria-label="Tổng quan danh sách theo dõi">
          <div className="pulse-primary">
            <strong>{items.length.toLocaleString('vi-VN')}</strong>
            <span>tài sản đang theo dõi</span>
          </div>
          <div className="pulse-detail">
            <span><strong>{targetCount}</strong> có giá mục tiêu</span>
            <span className={staleCount > 0 ? 'pulse-warning' : ''}>
              <strong>{staleCount}</strong> giá cần cập nhật
            </span>
          </div>
        </div>
        <div className="watchlist-hero-action">
          <button className="watchlist-add-btn" onClick={() => setIsAddOpen(true)}>
            <span aria-hidden="true">＋</span>
            Thêm tài sản
          </button>
        </div>
      </header>

      <section className="watchlist-workbench glass-panel">
        <div className="watchlist-toolbar">
          <label className="watchlist-search">
            <span className="sr-only">Tìm tài sản</span>
            <span aria-hidden="true">⌕</span>
            <input
              type="search"
              value={search}
              onChange={event => setSearch(event.target.value)}
              placeholder="Tìm theo mã hoặc tên tài sản"
            />
          </label>
          <div className="watchlist-categories" aria-label="Lọc theo danh mục">
            <button className={category === 'all' ? 'active' : ''} onClick={() => setCategory('all')}>
              Tất cả <span>{items.length}</span>
            </button>
            {categories.map(name => (
              <button
                key={name}
                className={category === name ? 'active' : ''}
                onClick={() => setCategory(name)}
              >
                {name}
              </button>
            ))}
          </div>
        </div>

        {loading && (
          <div className="watchlist-skeletons" aria-label="Đang tải danh sách">
            {[0, 1, 2].map(item => <div className="watchlist-skeleton" key={item} />)}
          </div>
        )}

        {!loading && error && (
          <div className="watchlist-state is-error">
            <span aria-hidden="true">!</span>
            <h2>Chưa thể mở radar tài sản</h2>
            <p>{error}</p>
            <button className="btn btn-outline" onClick={loadItems}>Thử lại</button>
          </div>
        )}

        {!loading && !error && items.length === 0 && (
          <div className="watchlist-state">
            <span aria-hidden="true">◎</span>
            <h2>Radar của bạn đang trống</h2>
            <p>Thêm tài sản đầu tiên để bắt đầu theo dõi giá và mục tiêu.</p>
            <button className="btn btn-primary" onClick={() => setIsAddOpen(true)}>
              Thêm tài sản đầu tiên
            </button>
          </div>
        )}

        {!loading && !error && items.length > 0 && filteredItems.length === 0 && (
          <div className="watchlist-state compact">
            <span aria-hidden="true">⌕</span>
            <h2>Không tìm thấy tài sản phù hợp</h2>
            <button className="btn btn-outline" onClick={() => { setSearch(''); setCategory('all'); }}>
              Xóa bộ lọc
            </button>
          </div>
        )}

        {!loading && !error && filteredItems.length > 0 && (
          <div className="watchlist-ledger">
            <div className="watchlist-ledger-head" aria-hidden="true">
              <span>Tài sản</span><span>Giá hiện tại</span><span>Mục tiêu</span><span>Cập nhật</span><span />
            </div>
            {filteredItems.map(item => {
              const distance = item.targetPrice && item.currentPrice > 0
                ? ((item.targetPrice - item.currentPrice) / item.currentPrice) * 100
                : null;
              return (
                <article key={item.id} className="watchlist-row">
                  <div className="watchlist-identity">
                    <div className="watchlist-monogram">{item.symbol.slice(0, 2)}</div>
                    <div>
                      <div className="watchlist-symbol-line">
                        <h2>{item.symbol}</h2>
                        <span>{item.assetCategoryName}</span>
                      </div>
                      <p>{item.name}</p>
                      <small>Thêm ngày {formatVietnamDate(item.addedAt)}</small>
                    </div>
                  </div>
                  <div className="watchlist-price" data-label="Giá hiện tại">
                    <strong>{formatMoney(item.currentPrice, item.currency)}</strong>
                    <span className={`price-status ${item.priceStatus.toLocaleLowerCase('en-US')}`}>
                      {item.priceStatus}
                    </span>
                  </div>
                  <div className="watchlist-target" data-label="Mục tiêu">
                    {item.targetPrice ? (
                      <>
                        <strong>{formatMoney(item.targetPrice, item.currency)}</strong>
                        <span className={distance !== null && distance >= 0 ? 'target-up' : 'target-down'}>
                          {distance !== null && distance > 0 ? '+' : ''}{distance?.toFixed(1)}%
                        </span>
                      </>
                    ) : <span className="target-empty">Chưa thiết lập</span>}
                  </div>
                  <time className="watchlist-updated" dateTime={item.priceUpdatedAt} data-label="Cập nhật">
                    {formatVietnamDateTime(item.priceUpdatedAt)}
                  </time>
                  <button
                    className="watchlist-remove"
                    onClick={() => removeItem(item)}
                    disabled={removingId === item.id}
                    aria-label={`Xóa ${item.symbol} khỏi danh sách theo dõi`}
                  >
                    {removingId === item.id ? <span className="button-spinner" /> : 'Xóa'}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      {isAddOpen && (
        <AddWatchlistModal
          onClose={() => setIsAddOpen(false)}
          onSuccess={() => {
            setIsAddOpen(false);
            void loadItems();
          }}
        />
      )}
    </main>
  );
}
