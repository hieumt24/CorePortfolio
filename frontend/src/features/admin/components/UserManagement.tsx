import { useEffect, useState } from 'react';
import { useAuth } from '../../../context/AuthContext';
import { useNotification } from '../../../context/NotificationContext';
import { adminApi } from '../api/adminApi';
import type { AdminUser } from '../types';
import './AdminOperations.css';

const formatDate = (value: string | null) => value
  ? new Date(value).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' })
  : 'Chưa đăng nhập';

export function UserManagement() {
  const { user: currentUser } = useAuth();
  const { showNotification } = useNotification();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [role, setRole] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [updatingId, setUpdatingId] = useState<string | null>(null);

  const loadUsers = async () => {
    setLoading(true);
    setError('');
    try {
      const result = await adminApi.getUsers({
        search, role, isActive: status === '' ? undefined : status === 'active', page, pageSize: 20,
      });
      setUsers(result.items);
      setTotalCount(result.totalCount);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Không thể tải danh sách người dùng.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void loadUsers(); }, [search, role, status, page]);

  const updateAccess = async (user: AdminUser, nextRole: AdminUser['role'], isActive: boolean) => {
    const action = !isActive ? 'khóa' : nextRole !== user.role ? 'đổi vai trò của' : 'mở khóa';
    if (!window.confirm(`Xác nhận ${action} tài khoản ${user.username}?`)) return;
    setUpdatingId(user.id);
    try {
      const updated = await adminApi.updateUserAccess(user.id, nextRole, isActive);
      setUsers(items => items.map(item => item.id === updated.id ? updated : item));
      showNotification('Đã cập nhật quyền truy cập.', 'success');
    } catch (updateError) {
      showNotification(updateError instanceof Error ? updateError.message : 'Cập nhật thất bại.', 'error');
    } finally {
      setUpdatingId(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / 20));

  return (
    <div className="admin-page-container admin-operations">
      <div className="operations-hero compact">
        <div><span className="admin-kicker">Identity & access</span><h1>Quản lý người dùng</h1><p>Tìm kiếm, phân quyền và kiểm soát trạng thái đăng nhập.</p></div>
        <span className="user-total">{totalCount} tài khoản</span>
      </div>

      <form className="user-filters" onSubmit={event => { event.preventDefault(); setPage(1); setSearch(searchInput.trim()); }}>
        <label className="user-search"><span className="sr-only">Tìm người dùng</span><input value={searchInput} onChange={event => setSearchInput(event.target.value)} placeholder="Tìm theo username..." /><button>Tìm</button></label>
        <select value={role} onChange={event => { setRole(event.target.value); setPage(1); }} aria-label="Lọc vai trò"><option value="">Mọi vai trò</option><option value="Admin">Admin</option><option value="User">User</option></select>
        <select value={status} onChange={event => { setStatus(event.target.value); setPage(1); }} aria-label="Lọc trạng thái"><option value="">Mọi trạng thái</option><option value="active">Đang hoạt động</option><option value="inactive">Đã khóa</option></select>
      </form>

      <div className="users-table-panel">
        {loading ? <div className="admin-loading"><span className="admin-loader" />Đang tải người dùng...</div> : error ? <div className="admin-error"><p>{error}</p><button onClick={loadUsers}>Thử lại</button></div> : users.length === 0 ? <div className="admin-empty"><strong>Không tìm thấy tài khoản</strong><span>Hãy thay đổi bộ lọc hoặc từ khóa tìm kiếm.</span></div> : (
          <div className="table-responsive"><table className="users-table"><thead><tr><th>Người dùng</th><th>Vai trò</th><th>Hoạt động</th><th>Dữ liệu</th><th>Ngày tạo</th><th>Thao tác</th></tr></thead><tbody>
            {users.map(user => {
              const isSelf = currentUser?.id === user.id;
              const busy = updatingId === user.id;
              return <tr key={user.id}>
                <td><div className="user-identity"><span>{user.username.slice(0, 1).toUpperCase()}</span><div><strong>{user.username}</strong><small className={user.isActive ? 'active' : 'inactive'}>{user.isActive ? 'Đang hoạt động' : 'Đã khóa'}{isSelf ? ' · Bạn' : ''}</small></div></div></td>
                <td><select className="role-select" value={user.role} disabled={busy || isSelf} onChange={event => updateAccess(user, event.target.value as AdminUser['role'], user.isActive)}><option value="User">User</option><option value="Admin">Admin</option></select></td>
                <td><strong className="last-login">{formatDate(user.lastLoginAt)}</strong></td>
                <td><div className="user-data"><span>{user.portfolioCount} portfolio</span><span>{user.transactionCount} giao dịch</span></div></td>
                <td>{formatDate(user.createdAt)}</td>
                <td><button className={`access-toggle ${user.isActive ? 'danger' : 'success'}`} disabled={busy || isSelf} onClick={() => updateAccess(user, user.role, !user.isActive)}>{busy ? 'Đang lưu...' : user.isActive ? 'Khóa' : 'Mở khóa'}</button></td>
              </tr>;
            })}
          </tbody></table></div>
        )}
        {!loading && !error && totalCount > 0 && <div className="users-pagination"><span>Trang {page}/{totalPages}</span><div><button disabled={page === 1} onClick={() => setPage(value => value - 1)}>Trước</button><button disabled={page === totalPages} onClick={() => setPage(value => value + 1)}>Sau</button></div></div>}
      </div>
    </div>
  );
}
