import React from 'react';
import './TransactionPnlCell.css';

interface TransactionPnlCellProps {
  remainingQuantity?: number | null;
  unrealizedPnl?: number | null;
  isClosed?: boolean | null;
  currency: string;
  formatCurrency: (value: number, currency: string) => string;
}

export const TransactionPnlCell: React.FC<TransactionPnlCellProps> = ({
  remainingQuantity,
  unrealizedPnl,
  isClosed,
  currency,
  formatCurrency,
}) => {
  if (isClosed === undefined || isClosed === null) {
    return <span className="transaction-pnl-na" title="Không áp dụng cho loại giao dịch này">—</span>;
  }

  if (isClosed) {
    return <span className="transaction-position-status closed">Đã chốt</span>;
  }

  const pnl = unrealizedPnl ?? 0;
  return (
    <span className={`transaction-pnl ${pnl >= 0 ? 'text-success' : 'text-danger'}`}>
      <strong>{pnl > 0 ? '+' : ''}{formatCurrency(pnl, currency)}</strong>
      <small>Còn {Number(remainingQuantity ?? 0).toLocaleString(undefined, { maximumFractionDigits: 8 })}</small>
    </span>
  );
};
