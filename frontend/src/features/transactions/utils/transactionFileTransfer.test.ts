import { describe, expect, it } from 'vitest';
import {
  parseCsvRows,
  parseGeneratedPdfRows,
  parseSpreadsheetXmlRows,
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

    expect(rows[0]).toEqual(['Date', 'Portfolio', 'Symbol', 'Type', 'Quantity', 'Price', 'Total']);
    expect(rows[1]).toContain('Growth, 2026');
    expect(rows[1]).toContain('VND');
  });
});
