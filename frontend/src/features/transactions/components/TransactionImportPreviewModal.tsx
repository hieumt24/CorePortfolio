import { useCallback, useEffect, useMemo, useState } from 'react';
import { createAsset } from '../../assets/api/assetApi';
import { marketAssetsApi } from '../../admin/api/marketAssets';
import { categoriesApi } from '../../admin/api/categories';
import type { MarketAsset } from '../../admin/types';
import { getPortfolios, getPortfolioSummary } from '../../portfolios/api/portfolioApi';
import type { AssetSummaryDto, PortfolioDto } from '../../portfolios/types';
import { useAuth } from '../../../context/AuthContext';
import { useNotification } from '../../../context/NotificationContext';
import { createTransaction, getAllTransactions } from '../api/transactionApi';
import type { GlobalTransactionDto, TransactionAssetGroup, TransactionType } from '../types';
import {
  parseFlexibleNumber,
  parseTransactionDate,
  parseTransactionType,
  type TransactionImportRow,
} from '../utils/transactionFileTransfer';

interface Props {
  fileName: string;
  rows: TransactionImportRow[];
  assetGroup: TransactionAssetGroup;
  onClose: () => void;
  onImported: () => void | Promise<void>;
}

interface PortfolioDirectory {
  portfolio: PortfolioDto;
  assets: AssetSummaryDto[];
}

type PreviewStatus = 'ready' | 'duplicate' | 'invalid' | 'missing-market' | 'missing-asset' | 'out-of-scope';

interface PreviewRow {
  source: TransactionImportRow;
  rowNumber: number;
  status: PreviewStatus;
  reason: string;
  portfolio?: PortfolioDto;
  asset?: AssetSummaryDto;
  marketAsset?: MarketAsset;
  type: TransactionType | null;
  quantity: number;
  price: number;
  fee: number;
  date: Date | null;
}

const normalize = (value?: string) =>
  value?.trim().toLocaleLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd') ?? '';

const isFund = (value: string) =>
  value.includes('fund') || value.includes('quy') || value.includes('ccq') ||
  value.includes('etf') || value.includes('chung chi quy');

const matchesGroup = (category: string, group: TransactionAssetGroup) => {
  const value = normalize(category);
  if (group === 'all') return true;
  if (group === 'crypto') return value.includes('crypto') || value.includes('tien ma hoa') || value.includes('tien dien tu');
  if (group === 'stock') {
    return !isFund(value) && (value.includes('stock') || value.includes('equity') ||
      value.includes('co phieu') || value.includes('chung khoan'));
  }
  return isFund(value);
};

const fingerprint = (
  assetId: string,
  type: number,
  quantity: number,
  price: number,
  date: Date,
  notes: string,
) => [assetId, type, quantity, price, date.toISOString(), notes.trim()].join('|').toLowerCase();

const fetchEveryTransaction = async () => {
  const pageSize = 500;
  const first = await getAllTransactions({ page: 1, pageSize });
  const items = [...first.items];
  const pages = Math.ceil(first.totalCount / pageSize);
  for (let page = 2; page <= pages; page += 1) {
    items.push(...(await getAllTransactions({ page, pageSize })).items);
  }
  return items;
};

export function TransactionImportPreviewModal({
  fileName,
  rows,
  assetGroup,
  onClose,
  onImported,
}: Props) {
  const { isAdmin } = useAuth();
  const { showNotification } = useNotification();
  const [directories, setDirectories] = useState<PortfolioDirectory[]>([]);
  const [marketAssets, setMarketAssets] = useState<MarketAsset[]>([]);
  const [existingTransactions, setExistingTransactions] = useState<GlobalTransactionDto[]>([]);
  const [targetPortfolioId, setTargetPortfolioId] = useState('');
  const [loading, setLoading] = useState(true);
  const [importing, setImporting] = useState(false);
  const [resolvingSymbol, setResolvingSymbol] = useState<string | null>(null);
  const [loadError, setLoadError] = useState('');

  const loadDirectory = useCallback(async (preferredPortfolioId?: string) => {
    setLoading(true);
    setLoadError('');
    try {
      const [portfolios, marketAssetPage, transactions] = await Promise.all([
        getPortfolios(),
        marketAssetsApi.getMarketAssets(undefined, 1, 5000),
        fetchEveryTransaction(),
      ]);
      const loadedDirectories = await Promise.all(portfolios.map(async portfolio => ({
        portfolio,
        assets: (await getPortfolioSummary(portfolio.id)).assets,
      })));
      setDirectories(loadedDirectories);
      setMarketAssets(marketAssetPage.items);
      setExistingTransactions(transactions);
      setTargetPortfolioId(current =>
        preferredPortfolioId || current || (portfolios.length === 1 ? portfolios[0].id : ''),
      );
    } catch (error) {
      setLoadError(error instanceof Error ? error.message : 'Không thể tải dữ liệu đối chiếu.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadDirectory();
  }, [loadDirectory]);

  const previewRows = useMemo<PreviewRow[]>(() => {
    const existingIds = new Set(existingTransactions.map(item => item.id.toLowerCase()));
    const existingFingerprints = new Set(existingTransactions.map(item =>
      fingerprint(item.assetId, item.type, item.quantity, item.price, new Date(item.date), item.notes ?? ''),
    ));

    return rows.map((source, index) => {
      const rowNumber = index + 2;
      const type = parseTransactionType(source.type);
      const quantity = parseFlexibleNumber(source.quantity);
      const price = parseFlexibleNumber(source.price);
      const fee = parseFlexibleNumber(source.fee);
      const date = parseTransactionDate(source.date);
      const explicitPortfolio = source.portfolioId
        ? directories.find(item => item.portfolio.id.toLowerCase() === normalize(source.portfolioId))
        : directories.find(item => normalize(item.portfolio.name) === normalize(source.portfolio));
      const selectedDirectory = explicitPortfolio ??
        directories.find(item => item.portfolio.id === targetPortfolioId);
      const symbol = normalize(source.symbol || source.asset);
      const asset = selectedDirectory?.assets.find(item =>
        (source.assetId && item.assetId.toLowerCase() === normalize(source.assetId)) ||
        normalize(item.symbol) === symbol ||
        normalize(item.name) === normalize(source.asset),
      );
      const marketAsset = marketAssets.find(item =>
        normalize(item.symbol) === symbol &&
        (assetGroup === 'all' || matchesGroup(item.categoryName, assetGroup)),
      );
      const base = { source, rowNumber, portfolio: selectedDirectory?.portfolio, asset, marketAsset, type, quantity, price, fee, date };

      if (!source.quantity?.trim() || !source.price?.trim() || type === null || quantity <= 0 || price < 0 || !date) {
        return { ...base, status: 'invalid', reason: 'Type, Quantity, Price hoặc Date không hợp lệ.' };
      }
      if (!selectedDirectory) {
        return { ...base, status: 'invalid', reason: 'Chưa chọn được portfolio đích.' };
      }
      if (source.id && existingIds.has(source.id.toLowerCase())) {
        return { ...base, status: 'duplicate', reason: 'Transaction ID đã tồn tại.' };
      }
      if (asset && !matchesGroup(asset.categoryName, assetGroup)) {
        return { ...base, status: 'out-of-scope', reason: 'Không thuộc phạm vi import đang chọn.' };
      }
      if (!marketAsset) {
        return { ...base, status: 'missing-market', reason: `Chưa có ${source.symbol || source.asset} trong Market Asset.` };
      }
      if (!asset) {
        return { ...base, status: 'missing-asset', reason: 'Market Asset đã có nhưng chưa được thêm vào portfolio.' };
      }
      const rowFingerprint = fingerprint(asset.assetId, type, quantity, price, date, source.notes ?? '');
      if (existingFingerprints.has(rowFingerprint)) {
        return { ...base, status: 'duplicate', reason: 'Giao dịch trùng nội dung đã tồn tại.' };
      }
      existingFingerprints.add(rowFingerprint);
      if (source.id) existingIds.add(source.id.toLowerCase());
      return { ...base, status: 'ready', reason: 'Sẵn sàng import.' };
    });
  }, [assetGroup, directories, existingTransactions, marketAssets, rows, targetPortfolioId]);

  const counts = useMemo(() => previewRows.reduce<Record<PreviewStatus, number>>((result, row) => {
    result[row.status] += 1;
    return result;
  }, { ready: 0, duplicate: 0, invalid: 0, 'missing-market': 0, 'missing-asset': 0, 'out-of-scope': 0 }), [previewRows]);

  const resolveSymbol = async (row: PreviewRow) => {
    if (!row.portfolio || !row.source.symbol) return;
    const symbol = row.source.symbol.trim().toUpperCase();
    try {
      setResolvingSymbol(symbol);
      let marketAssetId = row.marketAsset?.id;
      if (!marketAssetId) {
        if (!isAdmin) return;
        const categories = await categoriesApi.getCategories();
        const inferredGroup: TransactionAssetGroup = assetGroup !== 'all'
          ? assetGroup
          : matchesGroup(row.source.category ?? '', 'fund')
            ? 'fund'
            : matchesGroup(row.source.category ?? '', 'stock')
              ? 'stock'
              : 'crypto';
        const category = categories.find(item => matchesGroup(item.name, inferredGroup));
        if (!category) throw new Error(`Không tìm thấy category ${inferredGroup} phù hợp.`);
        const created = await marketAssetsApi.createMarketAsset({
          categoryId: category.id,
          symbol,
          name: symbol,
          currentPrice: row.price,
          priceSource: 'Manual',
          externalId: null,
        });
        marketAssetId = created.id;
      }
      await createAsset({ portfolioId: row.portfolio.id, marketAssetId });
      await loadDirectory(row.portfolio.id);
      showNotification(`Đã bổ sung ${symbol} vào portfolio.`, 'success');
    } catch (error) {
      showNotification(error instanceof Error ? error.message : `Không thể bổ sung ${symbol}.`, 'error');
    } finally {
      setResolvingSymbol(null);
    }
  };

  const executeImport = async () => {
    const readyRows = previewRows.filter(row => row.status === 'ready' && row.portfolio && row.asset && row.date && row.type !== null);
    if (readyRows.length === 0) return;
    setImporting(true);
    let imported = 0;
    const failures: string[] = [];
    for (const row of readyRows) {
      try {
        await createTransaction({
          portfolioId: row.portfolio!.id,
          assetId: row.asset!.assetId,
          type: row.type!,
          quantity: row.quantity,
          price: row.price,
          fee: row.fee,
          currency: row.source.currency?.trim() || row.asset!.currency,
          notes: row.source.notes?.trim() || '',
          timestamp: row.date!.toISOString(),
        });
        imported += 1;
      } catch (error) {
        failures.push(`${row.source.symbol || row.source.asset}: ${error instanceof Error ? error.message : 'Lỗi không xác định'}`);
      }
    }
    setImporting(false);
    if (failures.length) {
      showNotification(`Đã import ${imported}; ${failures.length} giao dịch lỗi.`, 'error');
      await loadDirectory(targetPortfolioId);
      return;
    }
    showNotification(`Đã import ${imported} giao dịch từ ${fileName}.`, 'success');
    await onImported();
    onClose();
  };

  return (
    <div className="modal-overlay import-preview-overlay" role="dialog" aria-modal="true" aria-labelledby="import-preview-title">
      <div className="import-preview-modal glass-panel">
        <header className="import-preview-header">
          <div>
            <span className="data-actions-kicker">IMPORT REVIEW</span>
            <h2 id="import-preview-title">Kiểm tra trước khi import</h2>
            <p>{fileName}</p>
          </div>
          <button className="close-btn" onClick={onClose} disabled={importing} aria-label="Đóng">&times;</button>
        </header>

        {loading ? (
          <div className="import-preview-state"><div className="spinner" /><span>Đang đối chiếu Market Asset…</span></div>
        ) : loadError ? (
          <div className="import-preview-state error-state">
            <strong>Không thể tạo preview</strong><span>{loadError}</span>
            <button className="btn btn-outline" onClick={() => void loadDirectory()}>Thử lại</button>
          </div>
        ) : (
          <>
            <div className="import-preview-toolbar">
              <label>
                Portfolio đích
                <select value={targetPortfolioId} onChange={event => setTargetPortfolioId(event.target.value)}>
                  <option value="">Chọn portfolio</option>
                  {directories.map(item => <option key={item.portfolio.id} value={item.portfolio.id}>{item.portfolio.name}</option>)}
                </select>
              </label>
              <div className="import-preview-stats">
                <span className="ready">{counts.ready}<small>Sẵn sàng</small></span>
                <span className="warning">{counts['missing-market'] + counts['missing-asset']}<small>Cần bổ sung</small></span>
                <span>{counts.duplicate + counts['out-of-scope']}<small>Bỏ qua</small></span>
                <span className="danger">{counts.invalid}<small>Không hợp lệ</small></span>
              </div>
            </div>

            <div className="import-preview-table-wrap">
              <table className="import-preview-table">
                <thead><tr><th>Dòng</th><th>Asset</th><th>Loại</th><th>Số lượng</th><th>Trạng thái</th><th /></tr></thead>
                <tbody>
                  {previewRows.map(row => (
                    <tr key={`${row.rowNumber}-${row.source.symbol}-${row.source.date}`} className={`preview-${row.status}`}>
                      <td>{row.rowNumber}</td>
                      <td><strong>{row.source.symbol || row.source.asset || '—'}</strong><small>{row.portfolio?.name || 'Chưa chọn portfolio'}</small></td>
                      <td>{row.source.type || '—'}</td>
                      <td>{row.quantity || '—'}</td>
                      <td><span className={`preview-status ${row.status}`}>{row.reason}</span></td>
                      <td>
                        {(row.status === 'missing-market' || row.status === 'missing-asset') && row.portfolio && (
                          <button
                            className="preview-resolve-btn"
                            onClick={() => void resolveSymbol(row)}
                            disabled={Boolean(resolvingSymbol) || (row.status === 'missing-market' && !isAdmin)}
                            title={row.status === 'missing-market' && !isAdmin ? 'Chỉ Admin có thể tạo Market Asset' : undefined}
                          >
                            {resolvingSymbol === row.source.symbol?.toUpperCase()
                              ? 'Đang thêm…'
                              : row.status === 'missing-market'
                                ? isAdmin ? 'Tạo & thêm' : 'Cần Admin'
                                : 'Thêm vào portfolio'}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <footer className="import-preview-footer">
              <p>{counts.ready} / {rows.length} dòng sẽ được ghi. Các dòng còn lại không bị import âm thầm.</p>
              <div>
                <button className="btn btn-outline" onClick={onClose} disabled={importing}>Hủy</button>
                <button className="btn btn-primary" onClick={() => void executeImport()} disabled={importing || counts.ready === 0}>
                  {importing ? 'Đang import…' : `Xác nhận import ${counts.ready} dòng`}
                </button>
              </div>
            </footer>
          </>
        )}
      </div>
    </div>
  );
}
