import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '../../../context/AuthContext';
import { useNotification } from '../../../context/NotificationContext';
import { adminApi } from '../api/adminApi';
import type { AdminUser } from '../types';
import './AdminOperations.css';

const PAGE_SIZE = 20;
const PRESENCE_REFRESH_INTERVAL_MS = 60_000;

const formatDate = (value: string | null, fallback = 'Chưa ghi nhận') => value
  ? new Date(value).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' })
  : fallback;

export function UserManagement() {
  const { user: currentUser } = useAuth();
  const { showNotification } = useNotification();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [role, setRole] = useState('');
  const [accountStatus, setAccountStatus] = useState('');
  const [presence, setPresence] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [updatingId, setUpdatingId] = useState<string | null>(null);

  const loadUsers = useCallback(async (showLoading = true) => {
    if (showLoading) {
      setLoading(true);
      setError('');
    }

    try {
      const result = await adminApi.getUsers({
        search,
        role,
        isActive: accountStatus === '' ? undefined : accountStatus === 'active',
        isOnline: presence === '' ? undefined : presence === 'online',
        page,
        pageSize: PAGE_SIZE,
      });
      setUsers(result.items);
      setTotalCount(result.totalCount);
    } catch (loadError) {
      if (showLoading) {
        setError(loadError instanceof Error
          ? loadError.message
          : 'Không thể tải danh sách người dùng.');
      }
    } finally {
      if (showLoading) setLoading(false);
    }
  }, [accountStatus, page, presence, role, search]);

  useEffect(() => {
    void loadUsers();
    const refreshTimer = window.setInterval(
      () => void loadUsers(false),
      PRESENCE_REFRESH_INTERVAL_MS,
    );
    return () => window.clearInterval(refreshTimer);
  }, [loadUsers]);

  const updateAccess = async (
    user: AdminUser,
    nextRole: AdminUser['role'],
    isActive: boolean,
  ) => {
    const action = !isActive
      ? 'khóa'
      : nextRole !== user.role
        ? 'đổi vai trò của'
        : 'mở khóa';
    if (!window.confirm(`Xác nhận ${action} tài khoản ${user.username}?`)) return;

    setUpdatingId(user.id);
    try {
      const updated = await adminApi.updateUserAccess(user.id, nextRole, isActive);
      setUsers(items => items.map(item => item.id === updated.id ? updated : item));
      showNotification('Đã cập nhật quyền truy cập.', 'success');
    } catch (updateError) {
      showNotification(
        updateError instanceof Error ? updateError.message : 'Cập nhật thất bại.',
        'error',
      );
    } finally {
      setUpdatingId(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="admin-page-container admin-operations">
      <div className="operations-hero compact">
        <div>
          <span className="admin-kicker">Identity & access</span>
          <h1>Quản lý người dùng</h1>
          <p>Theo dõi truy cập, trạng thái hiện diện và kiểm soát quyền tài khoản.</p>
        </div>
        <span className="user-total">{totalCount} tài khoản</span>
      </div>

      <form
        className="user-filters"
        onSubmit={event => {
          event.preventDefault();
          setPage(1);
          setSearch(searchInput.trim());
        }}
      >
        <label className="user-search">
          <span className="sr-only">Tìm người dùng</span>
          <input
            value={searchInput}
            onChange={event => setSearchInput(event.target.value)}
            placeholder="Tìm username, tên hoặc email..."
          />
          <button>Tìm</button>
        </label>
        <select
          value={role}
          onChange={event => {
            setRole(event.target.value);
            setPage(1);
          }}
          aria-label="Lọc vai trò"
        >
          <option value="">Mọi vai trò</option>
          <option value="Admin">Admin</option>
          <option value="User">User</option>
        </select>
        <select
          value={accountStatus}
          onChange={event => {
            setAccountStatus(event.target.value);
            setPage(1);
          }}
          aria-label="Lọc trạng thái tài khoản"
        >
          <option value="">Mọi tài khoản</option>
          <option value="active">Được phép truy cập</option>
          <option value="inactive">Đã khóa</option>
        </select>
        <select
          value={presence}
          onChange={event => {
            setPresence(event.target.value);
            setPage(1);
          }}
          aria-label="Lọc trạng thái hiện diện"
        >
          <option value="">Online & Offline</option>
          <option value="online">Đang online</option>
          <option value="offline">Đang offline</option>
        </select>
      </form>

      <div className="users-table-panel">
        {loading ? (
          <div className="admin-loading">
            <span className="admin-loader" />
            Đang tải người dùng...
          </div>
        ) : error ? (
          <div className="admin-error">
            <p>{error}</p>
            <button onClick={() => void loadUsers()}>Thử lại</button>
          </div>
        ) : users.length === 0 ? (
          <div className="admin-empty">
            <strong>Không tìm thấy tài khoản</strong>
            <span>Hãy thay đổi bộ lọc hoặc từ khóa tìm kiếm.</span>
          </div>
        ) : (
          <div className="table-responsive">
            <table className="users-table">
              <thead>
                <tr>
                  <th>Người dùng</th>
                  <th>Quyền truy cập</th>
                  <th>Hiện diện</th>
                  <th>Lần đăng nhập cuối</th>
                  <th>Dữ liệu</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {users.map(user => {
                  const isSelf = currentUser?.id === user.id;
                  const busy = updatingId === user.id;

                  return (
                    <tr key={user.id}>
                      <td>
                        <div className="user-identity">
                          <span>{user.username.slice(0, 1).toUpperCase()}</span>
                          <div>
                            <strong>{user.username}</strong>
                            <small>Tạo {formatDate(user.createdAt)}</small>
                          </div>
                        </div>
                      </td>
                      <td>
                        <div className="user-access">
                          <select
                            className="role-select"
                            value={user.role}
                            disabled={busy || isSelf}
                            onChange={event => void updateAccess(
                              user,
                              event.target.value as AdminUser['role'],
                              user.isActive,
                            )}
                          >
                            <option value="User">User</option>
                            <option value="Admin">Admin</option>
                          </select>
                          <span className={`account-state ${user.isActive ? 'active' : 'inactive'}`}>
                            {user.isActive ? 'Được truy cập' : 'Đã khóa'}
                            {isSelf ? ' · Bạn' : ''}
                          </span>
                        </div>
                      </td>
                      <td>
                        <div className={`presence-state ${user.isOnline ? 'online' : 'offline'}`}>
                          <span className="presence-dot" aria-hidden="true" />
                          <div>
                            <strong>{user.isOnline ? 'Online' : 'Offline'}</strong>
                            <small>
                              {user.lastActivityAt
                                ? `Gần nhất ${formatDate(user.lastActivityAt)}`
                                : 'Chưa có hoạt động'}
                            </small>
                          </div>
                        </div>
                      </td>
                      <td>
                        <div className="login-telemetry">
                          <strong>{formatDate(user.lastLoginAt, 'Chưa đăng nhập')}</strong>
                          <code title="IP của lần đăng nhập thành công gần nhất">
                            {user.lastLoginIpAddress ?? 'Chưa ghi nhận IP'}
                          </code>
                        </div>
                      </td>
                      <td>
                        <div className="user-data">
                          <span>{user.portfolioCount} portfolio</span>
                          <span>{user.transactionCount} giao dịch</span>
                        </div>
                      </td>
                      <td>
                        <button
                          className={`access-toggle ${user.isActive ? 'danger' : 'success'}`}
                          disabled={busy || isSelf}
                          onClick={() => void updateAccess(user, user.role, !user.isActive)}
                        >
                          {busy ? 'Đang lưu...' : user.isActive ? 'Khóa' : 'Mở khóa'}
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {!loading && !error && totalCount > 0 && (
          <div className="users-pagination">
            <span>Trang {page}/{totalPages}</span>
            <div>
              <button disabled={page === 1} onClick={() => setPage(value => value - 1)}>
                Trước
              </button>
              <button
                disabled={page === totalPages}
                onClick={() => setPage(value => value + 1)}
              >
                Sau
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
