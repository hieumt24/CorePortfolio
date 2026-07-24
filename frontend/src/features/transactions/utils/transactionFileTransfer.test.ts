import { describe, expect, it } from 'vitest';
import {
  binanceRowsToImportRows,
  parseCsvRows,
  parseFlexibleNumber,
  parseGeneratedPdfRows,
  parseSpreadsheetXmlRows,
  parseTransactionDate,
  parseTransactionType,
  rowsToTransactionImportRows,
  transactionsToCsv,
  transactionsToPdf,
  transactionsToSpreadsheetXml,
} from './transactionFileTransfer';
import type { GlobalTransactionDto } from '../types';
import { TransactionType } from '../types';

const transaction: GlobalTransactionDto = {
  id: 'tx-1',
  portfolioId: 'portfolio-1',
  portfolioName: 'Growth, 2026',
  assetId: 'asset-1',
  symbol: 'VND',
  assetName: 'Vietnam Dong',
  categoryName: 'Fiat',
  currency: 'VND',
  type: TransactionType.Buy,
  quantity: 2,
  price: 125000,
  fee: 500,
  notes: 'Imported, verified',
  date: '2026-07-24T09:30:00.000Z',
};

describe('transaction file transfer', () => {
  it('round-trips CSV values containing commas', () => {
    const rows = parseCsvRows(transactionsToCsv([transaction]));
    const imported = rowsToTransactionImportRows(rows);

    expect(imported).toHaveLength(1);
    expect(imported[0]).toMatchObject({
      id: 'tx-1',
      portfolioId: 'portfolio-1',
      portfolio: 'Growth, 2026',
      notes: 'Imported, verified',
      quantity: '2',
      price: '125000',
    });
  });

  it('round-trips SpreadsheetML XLS values', () => {
    const xml = transactionsToSpreadsheetXml([transaction]);
    const rows = parseSpreadsheetXmlRows(xml);

    expect(rows[0]).toContain('PortfolioId');
    expect(rows[1]).toContain('Growth, 2026');
    expect(rows[1]).toContain('Imported, verified');
  });

  it('exports and imports the generated PDF table', async () => {
    const blob = transactionsToPdf([transaction]);
    const rows = parseGeneratedPdfRows(await blob.arrayBuffer());
    const imported = rowsToTransactionImportRows(rows);

    expect(rows[0]).toEqual([
      'Id',
      'PortfolioId',
      'Portfolio',
      'AssetId',
      'Symbol',
      'Asset',
      'Category',
      'Currency',
      'Type',
      'Quantity',
      'Price',
      'Fee',
      'Notes',
      'Date',
      'Total',
    ]);
    expect(imported[0]).toMatchObject({
      id: 'tx-1',
      portfolioId: 'portfolio-1',
      portfolio: 'Growth, 2026',
      symbol: 'VND',
      fee: '500',
      notes: 'Imported, verified',
    });
  });

  it('finds and maps a fund report header after cover rows', () => {
    const rows = rowsToTransactionImportRows([
      ['', 'BÁO CÁO TÀI SẢN'],
      ['', 'Tên CCQ', 'Tên chương trình', 'Ngày mua', '', 'Số lượng', 'Giá mua\n(VND)'],
      ['', 'DCDS', 'Linh hoạt', '23/07/2026', '', '11.03', '90,617.66'],
      ['', 'TỔNG TÀI SẢN'],
    ]);

    expect(rows[0]).toMatchObject({ symbol: 'DCDS', quantity: '11.03', price: '90,617.66' });
    expect(parseTransactionType(rows[0].type)).toBe(TransactionType.Buy);
    expect(parseFlexibleNumber(rows[0].price)).toBe(90617.66);
    expect(parseTransactionDate(rows[0].date)?.toISOString()).toBe('2026-07-23T00:00:00.000Z');
  });

  it('maps Binance Spot fills and converts base or BNB fees into quote currency', () => {
    const rows = binanceRowsToImportRows([
      {
        entryId: '1-1',
        time: '2026-01-10 10:00:00',
        pair: 'BNBUSDT',
        side: 'BUY',
        price: '900',
        executed: '0.1BNB',
        amount: '90USDT',
        fee: '0.0001BNB',
      },
      {
        entryId: '1-2',
        time: '2026-01-10 10:01:00',
        pair: 'SOLUSDT',
        side: 'BUY',
        price: '135',
        executed: '0.2SOL',
        amount: '27USDT',
        fee: '0.00001BNB',
      },
      {
        entryId: '1-3',
        time: '2026-01-10 10:02:00',
        pair: 'SOLUSDT',
        side: 'SELL',
        price: '136',
        executed: '0.1SOL',
        amount: '13.6USDT',
        fee: '0.0136USDT',
      },
    ]);
    const imported = rowsToTransactionImportRows(rows);

    expect(imported).toHaveLength(3);
    expect(imported[0]).toMatchObject({
      symbol: 'BNB',
      type: 'BUY',
      quantity: '0.1',
      price: '900',
      fee: '0.09',
      currency: 'USD',
      date: '2026-01-10T10:00:00+07:00',
    });
    expect(imported[1]).toMatchObject({ symbol: 'SOL', fee: '0.009' });
    expect(imported[2]).toMatchObject({ symbol: 'SOL', type: 'SELL', fee: '0.0136' });
    expect(imported.every(row => row.notes?.includes('Binance Spot'))).toBe(true);
  });
});
