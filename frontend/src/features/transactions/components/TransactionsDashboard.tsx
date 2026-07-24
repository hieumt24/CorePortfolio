import React, { useEffect, useRef, useState } from 'react';
import { deleteAllTransactions, deleteTransaction, getAllTransactions } from '../api/transactionApi';
import type { GlobalTransactionDto, PaginatedResult, TransactionAssetGroup, TransactionDto } from '../types';
import { TransactionType } from '../types';
import { useNotification } from '../../../context/NotificationContext';
import { GlobalCreateTransactionModal } from './GlobalCreateTransactionModal';
import { EditTransactionModal } from './EditTransactionModal';
import { TransactionImportPreviewModal } from './TransactionImportPreviewModal';
import type { AssetSummaryDto } from '../../portfolios/types';
import {
  parseCsvRows,
  parseExcelRows,
  parsePdfRows,
  parseSpreadsheetXmlRows,
  rowsToTransactionImportRows,
  transactionsToCsv,
  transactionsToPdf,
  transactionsToSpreadsheetXml,
} from '../utils/transactionFileTransfer';
import type { TransactionImportRow } from '../utils/transactionFileTransfer';
import './TransactionsDashboard.css';

const assetGroupLabels: Record<TransactionAssetGroup, string> = {
  all: 'Tất cả',
  crypto: 'Crypto',
  stock: 'Cổ phiếu',
  fund: 'CCQ / ETF',
};

const normalizeCategory = (category: string) =>
  category
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd');

const isFundCategory = (value: string) =>
  value.includes('fund') ||
  value.includes('quy') ||
  value.includes('ccq') ||
  value.includes('etf') ||
  value.includes('chung chi quy');

const matchesAssetGroup = (category: string, group: TransactionAssetGroup) => {
  const value = normalizeCategory(category);
  if (group === 'all') return true;
  if (group === 'crypto') return value.includes('crypto') || value.includes('tien ma hoa') || value.includes('tien dien tu');
  if (group === 'stock') {
    return !isFundCategory(value) &&
      (value.includes('stock') || value.includes('equity') || value.includes('co phieu') || value.includes('chung khoan'));
  }
  return isFundCategory(value);
};

export const TransactionsDashboard: React.FC = () => {
  const [data, setData] = useState<PaginatedResult<GlobalTransactionDto> | null>(null);
  const [loading, setLoading] = useState(true);
  
  // Filters
  const [page, setPage] = useState(1);
  const [pageSize] = useState(100);
  const [typeFilter, setTypeFilter] = useState<number | ''>('');
  const [assetGroup, setAssetGroup] = useState<TransactionAssetGroup>('all');
  const [actionScope, setActionScope] = useState<TransactionAssetGroup>('all');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingTransaction, setEditingTransaction] = useState<GlobalTransactionDto | null>(null);
  const [transferBusy, setTransferBusy] = useState<'import' | 'csv' | 'xls' | 'pdf' | null>(null);
  const [deletingAll, setDeletingAll] = useState(false);
  const [importPreview, setImportPreview] = useState<{ fileName: string; rows: TransactionImportRow[] } | null>(null);
  const importInputRef = useRef<HTMLInputElement>(null);
  const { showNotification } = useNotification();

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      const params: any = { page, pageSize };
      if (typeFilter !== '') params.type = typeFilter;
      if (startDate) params.startDate = new Date(startDate).toISOString();
      if (endDate) params.endDate = new Date(endDate).toISOString();

      const result = await getAllTransactions(params);
      setData(result);
    } catch (error) {
      showNotification('Failed to fetch transactions', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTransactions();
  }, [page, pageSize, typeFilter, startDate, endDate]);

  const fetchAllTransactions = async () => {
    const allItems: GlobalTransactionDto[] = [];
    let currentPage = 1;
    let totalCount = Number.POSITIVE_INFINITY;

    while (allItems.length < totalCount) {
      const result = await getAllTransactions({ page: currentPage, pageSize: 500 });
      allItems.push(...result.items);
      totalCount = result.totalCount;
      if (result.items.length === 0) break;
      currentPage += 1;
    }
    return allItems;
  };

  const downloadBlob = (blob: Blob, filename: string) => {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
  };

  const handleExport = async (format: 'csv' | 'xls' | 'pdf') => {
    try {
      setTransferBusy(format);
      const allTransactions = await fetchAllTransactions();
      const transactions = allTransactions.filter(transaction =>
        matchesAssetGroup(transaction.categoryName, actionScope),
      );
      const dateStamp = new Date().toISOString().slice(0, 10);
      const scopeSuffix = actionScope === 'all' ? 'all' : actionScope;
      if (format === 'csv') {
        downloadBlob(new Blob([transactionsToCsv(transactions)], { type: 'text/csv;charset=utf-8' }), `coreportfolio-transactions-${scopeSuffix}-${dateStamp}.csv`);
      } else if (format === 'xls') {
        downloadBlob(new Blob([transactionsToSpreadsheetXml(transactions)], { type: 'application/vnd.ms-excel' }), `coreportfolio-transactions-${scopeSuffix}-${dateStamp}.xls`);
      } else {
        downloadBlob(transactionsToPdf(transactions, assetGroupLabels[actionScope]), `coreportfolio-transactions-${scopeSuffix}-${dateStamp}.pdf`);
      }
      showNotification(`Đã export ${transactions.length} giao dịch ${assetGroupLabels[actionScope]} dạng ${format.toUpperCase()}.`, 'success');
    } catch (error) {
      showNotification(error instanceof Error ? error.message : 'Không thể export giao dịch.', 'error');
    } finally {
      setTransferBusy(null);
    }
  };

  const handleImportFile = async (file: File) => {
    try {
      setTransferBusy('import');
      const extension = file.name.toLowerCase().split('.').pop();
      let rows: string[][];
      if (extension === 'csv') {
        rows = parseCsvRows(await file.text());
      } else if (extension === 'xls' || extension === 'xlsx') {
        const buffer = await file.arrayBuffer();
        const bytes = new Uint8Array(buffer.slice(0, 8));
        const isOleBinary = bytes[0] === 0xD0 && bytes[1] === 0xCF && bytes[2] === 0x11 && bytes[3] === 0xE0;
        rows = isOleBinary ? await parseExcelRows(buffer) : parseSpreadsheetXmlRows(new TextDecoder().decode(buffer));
      } else if (extension === 'xml') {
        rows = parseSpreadsheetXmlRows(await file.text());
      } else if (extension === 'pdf') {
        rows = await parsePdfRows(await file.arrayBuffer());
      } else {
        throw new Error('Chỉ hỗ trợ file CSV, XLS hoặc PDF.');
      }

      setImportPreview({ fileName: file.name, rows: rowsToTransactionImportRows(rows) });
    } catch (error) {
      showNotification(error instanceof Error ? error.message : 'Không thể import giao dịch.', 'error');
    } finally {
      setTransferBusy(null);
    }
  };

  const visibleItems = data?.items.filter(item => matchesAssetGroup(item.categoryName, assetGroup)) ?? [];
  const countFor = (group: Exclude<TransactionAssetGroup, 'all'>) =>
    data?.items.filter(item => matchesAssetGroup(item.categoryName, group)).length ?? 0;

  const handleDelete = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this transaction?')) {
      try {
        await deleteTransaction(id);
        showNotification('Transaction deleted successfully', 'success');
        fetchTransactions();
      } catch (error) {
        showNotification('Failed to delete transaction', 'error');
      }
    }
  };

  const handleDeleteAll = async () => {
    const scopeLabel = assetGroupLabels[actionScope];
    const confirmed = window.confirm(
      `Bạn sắp xóa toàn bộ giao dịch thuộc phạm vi "${scopeLabel}", bao gồm các dòng tiền liên quan. Portfolio và asset vẫn được giữ lại. Hành động này không thể hoàn tác. Tiếp tục?`,
    );

    if (!confirmed) return;

    try {
      setDeletingAll(true);
      const result = await deleteAllTransactions(actionScope);
      setPage(1);
      await fetchTransactions();

      if (result.deletedCount === 0) {
        showNotification('Không có giao dịch nào để xóa.', 'info');
      } else {
        showNotification(`Đã xóa ${result.deletedCount} giao dịch ${scopeLabel}.`, 'success');
      }
    } catch (error) {
      showNotification(error instanceof Error ? error.message : 'Không thể xóa toàn bộ giao dịch.', 'error');
    } finally {
      setDeletingAll(false);
    }
  };

  const formatCurrency = (value: number, currency: string) => {
    try {
      const isVND = currency === 'VND';
      return new Intl.NumberFormat(isVND ? 'vi-VN' : 'en-US', {
        style: 'currency',
        currency: currency || 'USD',
        minimumFractionDigits: isVND ? 0 : 2,
      }).format(value);
    } catch {
      return value.toString();
    }
  };

  const getTypeName = (type: number) => {
    switch (type) {
      case TransactionType.Buy: return 'Buy';
      case TransactionType.Sell: return 'Sell';
      case TransactionType.Deposit: return 'Deposit';
      case TransactionType.Withdrawal: return 'Withdrawal';
      case TransactionType.Dividend: return 'Dividend';
      case TransactionType.Earn: return 'Earn';
      default: return 'Unknown';
    }
  };

  const toEditTransaction = (item: GlobalTransactionDto): TransactionDto => ({
    id: item.id, type: item.type, quantity: item.quantity, price: item.price,
    fee: item.fee, notes: item.notes, timestamp: item.date,
  });

  const toEditAsset = (item: GlobalTransactionDto): AssetSummaryDto => ({
    assetId: item.assetId, marketAssetId: item.assetId, symbol: item.symbol, name: item.assetName,
    categoryName: item.categoryName, currency: item.currency, currentPrice: item.price,
    totalQuantity: item.quantity, totalCost: item.quantity * item.price, currentValue: item.quantity * item.price,
    totalBought: item.quantity, averageCost: item.price, realizedPnl: 0, unrealizedPnl: 0, fees: item.fee, priceUpdatedAt: item.date,
  });

  return (
    <div className="container dashboard-layout">
      {/* Decorative blurred blobs */}
      <div className="mesh-blob blob-1"></div>
      <div className="mesh-blob blob-2" style={{ left: '60%', top: '40%' }}></div>

      <header className="dashboard-header">
        <div className="header-titles">
          <h1 className="gradient-text">Transactions Ledger</h1>
          <p className="subtitle">Theo dõi crypto, cổ phiếu và CCQ trong một dòng thời gian rõ ràng</p>
        </div>
        <div className="header-actions">
          <button className="btn btn-primary" onClick={() => setIsCreateModalOpen(true)} disabled={transferBusy !== null || deletingAll}>
            + Thêm giao dịch
          </button>
        </div>
      </header>

      <section className="data-actions-panel glass-panel" aria-label="Thao tác dữ liệu giao dịch">
        <input
          ref={importInputRef}
          type="file"
          accept=".csv,.xls,.xlsx,.xml,.pdf"
          className="sr-only"
          onChange={event => {
            const file = event.target.files?.[0];
            if (file) void handleImportFile(file);
            event.currentTarget.value = '';
          }}
        />
        <div className="data-actions-copy">
          <span className="data-actions-kicker">DATA CONTROL</span>
          <strong>Chọn phạm vi thao tác</strong>
          <small>Áp dụng chung cho Import, Export và Xóa</small>
        </div>
        <div className="scope-selector" role="group" aria-label="Phạm vi tài sản">
          {(Object.keys(assetGroupLabels) as TransactionAssetGroup[]).map(group => (
            <button
              key={group}
              className={`scope-option ${actionScope === group ? 'active' : ''}`}
              onClick={() => setActionScope(group)}
              disabled={transferBusy !== null || deletingAll}
              aria-pressed={actionScope === group}
            >
              {assetGroupLabels[group]}
            </button>
          ))}
        </div>
        <div className="transfer-actions" aria-label="Import and export transactions">
          <button
            className="btn btn-transfer"
            onClick={() => importInputRef.current?.click()}
            disabled={transferBusy !== null || deletingAll}
          >
            {transferBusy === 'import' ? 'Đang import…' : '⇧ Import'}
          </button>
          <div className="export-actions">
            <span className="export-label">Export</span>
            {(['csv', 'xls', 'pdf'] as const).map(format => (
              <button
                key={format}
                className="export-format-btn"
                onClick={() => void handleExport(format)}
                disabled={transferBusy !== null || deletingAll}
                title={`Export ${assetGroupLabels[actionScope]} dạng ${format.toUpperCase()}`}
              >
                {transferBusy === format ? '…' : format.toUpperCase()}
              </button>
            ))}
          </div>
          <button
            className="btn btn-delete-all"
            onClick={() => void handleDeleteAll()}
            disabled={transferBusy !== null || deletingAll || loading}
            title={`Xóa toàn bộ giao dịch ${assetGroupLabels[actionScope]}`}
          >
            {deletingAll ? 'Đang xóa…' : `Xóa ${assetGroupLabels[actionScope]}`}
          </button>
        </div>
      </section>

      <section className="transaction-groups" aria-label="Asset groups">
        {(['all', 'crypto', 'stock', 'fund'] as const).map(group => (
          <button key={group} className={`group-card ${group} ${assetGroup === group ? 'active' : ''}`} onClick={() => { setAssetGroup(group); setPage(1); }}>
            <span className="group-icon">{group === 'crypto' ? '₿' : group === 'stock' ? '↗' : group === 'fund' ? '◈' : '◎'}</span>
            <span><strong>{group === 'all' ? 'Tất cả' : group === 'crypto' ? 'Crypto' : group === 'stock' ? 'Cổ phiếu' : 'CCQ / ETF'}</strong><small>{group === 'all' ? data?.totalCount ?? 0 : countFor(group)} giao dịch</small></span>
          </button>
        ))}
      </section>

      <div className="filters-toolbar glass-panel">
        <div className="toolbar-group">
          <label htmlFor="typeFilter" className="sr-only">Type</label>
          <select 
            id="typeFilter"
            className="filter-select"
            value={typeFilter} 
            onChange={(e) => {
              setTypeFilter(e.target.value === '' ? '' : Number(e.target.value));
              setPage(1);
            }}
          >
            <option value="">All Types</option>
            <option value={TransactionType.Buy}>Buy</option>
            <option value={TransactionType.Sell}>Sell</option>
            <option value={TransactionType.Deposit}>Deposit</option>
            <option value={TransactionType.Withdrawal}>Withdrawal</option>
            <option value={TransactionType.Dividend}>Dividend</option>
            <option value={TransactionType.Earn}>Earn / Reward</option>
          </select>
        </div>

        <div className="toolbar-group">
          <label htmlFor="startDate" className="sr-only">Start Date</label>
          <input 
            id="startDate"
            type="date" 
            className="filter-input"
            value={startDate}
            onChange={(e) => { setStartDate(e.target.value); setPage(1); }}
            title="Start Date"
          />
          <span className="toolbar-sep">to</span>
          <label htmlFor="endDate" className="sr-only">End Date</label>
          <input 
            id="endDate"
            type="date" 
            className="filter-input"
            value={endDate}
            onChange={(e) => { setEndDate(e.target.value); setPage(1); }}
            title="End Date"
          />
        </div>
      </div>

      <div className="table-container glass-panel">
        {loading ? (
          <div className="state-panel">
            <div className="spinner"></div>
          </div>
        ) : visibleItems.length === 0 ? (
          <div className="state-panel empty-state">
            <p>No transactions match your filters.</p>
          </div>
        ) : (
          <div className="table-scroll">
            <table className="ledger-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Portfolio</th>
                  <th>Asset</th>
                  <th>Type</th>
                  <th className="num-col">Quantity</th>
                  <th className="num-col">Price</th>
                  <th className="num-col">Total</th>
                  <th className="action-col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {visibleItems.map(t => (
                  <tr key={t.id}>
                    <td className="date-cell">{new Date(t.date).toLocaleDateString()}</td>
                    <td>{t.portfolioName}</td>
                    <td className="asset-cell">
                      <span className="asset-name">{t.assetName}</span>
                      <span className="asset-sym">{t.symbol}</span>
                    </td>
                    <td>
                      <span className={`badge ${getTypeName(t.type).toLowerCase()}`}>
                        {getTypeName(t.type)}
                      </span>
                    </td>
                    <td className="num-col">{t.quantity.toLocaleString(undefined, { maximumFractionDigits: 8 })}</td>
                    <td className="num-col">{formatCurrency(t.price, t.currency)}</td>
                    <td className="num-col strong">{formatCurrency(t.quantity * t.price, t.currency)}</td>
                    <td className="action-col">
                      <button className="btn-icon edit-action" onClick={() => setEditingTransaction(t)} aria-label={`Edit ${t.symbol} transaction`} title="Edit transaction">
                        <span aria-hidden="true">✎</span>
                      </button>
                      <button className="btn-icon" onClick={() => handleDelete(t.id)} aria-label="Delete transaction">
                        <span aria-hidden="true">×</span>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {data && data.totalCount > 0 && (
        <div className="pagination">
          <button 
            className="btn btn-outline"
            disabled={page === 1}
            onClick={() => setPage(p => p - 1)}
          >
            ← Prev
          </button>
          <span className="page-info">
            {page} / {Math.ceil(data.totalCount / data.pageSize)} <span className="page-total">({data.totalCount} total)</span>
          </span>
          <button 
            className="btn btn-outline"
            disabled={page >= Math.ceil(data.totalCount / data.pageSize)}
            onClick={() => setPage(p => p + 1)}
          >
            Next →
          </button>
        </div>
      )}

      {isCreateModalOpen && (
        <GlobalCreateTransactionModal 
          initialCategory={assetGroup === 'all' ? undefined : assetGroup === 'stock' ? 'stock' : assetGroup === 'fund' ? 'fund' : 'crypto'}
          onClose={() => setIsCreateModalOpen(false)}
          onSuccess={() => {
            setIsCreateModalOpen(false);
            showNotification('Transaction recorded successfully', 'success');
            fetchTransactions();
          }}
        />
      )}
      {editingTransaction && (
        <EditTransactionModal
          transaction={toEditTransaction(editingTransaction)}
          asset={toEditAsset(editingTransaction)}
          onClose={() => setEditingTransaction(null)}
          onSuccess={() => {
            setEditingTransaction(null);
            showNotification('Transaction updated successfully', 'success');
            fetchTransactions();
          }}
        />
      )}
      {importPreview && (
        <TransactionImportPreviewModal
          fileName={importPreview.fileName}
          rows={importPreview.rows}
          assetGroup={actionScope}
          onClose={() => setImportPreview(null)}
          onImported={fetchTransactions}
        />
      )}
    </div>
  );
};
