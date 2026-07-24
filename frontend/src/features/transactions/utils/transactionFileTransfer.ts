import { TransactionType } from '../types';
import type { GlobalTransactionDto, TransactionType as TransactionTypeValue } from '../types';

export const TRANSACTION_FILE_HEADERS = [
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
] as const;

export interface TransactionImportRow {
  id?: string;
  portfolioId?: string;
  portfolio?: string;
  assetId?: string;
  symbol?: string;
  asset?: string;
  category?: string;
  currency?: string;
  type?: string;
  quantity?: string;
  price?: string;
  fee?: string;
  notes?: string;
  date?: string;
}

const transactionTypeNames: Record<TransactionTypeValue, string> = {
  [TransactionType.Buy]: 'Buy',
  [TransactionType.Sell]: 'Sell',
  [TransactionType.Deposit]: 'Deposit',
  [TransactionType.Withdrawal]: 'Withdrawal',
  [TransactionType.Dividend]: 'Dividend',
  [TransactionType.Earn]: 'Earn',
};

const normalizeHeader = (value: string) =>
  value
    .replace(/^\uFEFF/, '')
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/[^a-z0-9]/g, '');

const escapeCsv = (value: string) => {
  const normalized = value.replace(/\r?\n/g, ' ');
  return /[",\r\n]/.test(normalized) ? `"${normalized.replace(/"/g, '""')}"` : normalized;
};

const escapeXml = (value: string) =>
  value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');

const formatType = (type: TransactionTypeValue) => transactionTypeNames[type] ?? String(type);

const formatNumber = (value: number, maximumFractionDigits = 8) =>
  new Intl.NumberFormat('en-US', { maximumFractionDigits }).format(value);

export const transactionToFileRow = (transaction: GlobalTransactionDto): string[] => [
  transaction.id,
  transaction.portfolioId,
  transaction.portfolioName,
  transaction.assetId,
  transaction.symbol,
  transaction.assetName,
  transaction.categoryName,
  transaction.currency,
  formatType(transaction.type),
  String(transaction.quantity),
  String(transaction.price),
  String(transaction.fee),
  transaction.notes ?? '',
  new Date(transaction.date).toISOString(),
  String(transaction.quantity * transaction.price),
];

export const transactionsToCsv = (transactions: GlobalTransactionDto[]) => {
  const rows = [Array.from(TRANSACTION_FILE_HEADERS), ...transactions.map(transactionToFileRow)];
  return `\uFEFF${rows.map(row => row.map(escapeCsv).join(',')).join('\r\n')}`;
};

export const transactionsToSpreadsheetXml = (transactions: GlobalTransactionDto[]) => {
  const rowToXml = (row: string[]) =>
    `<Row>${row.map(value => `<Cell><Data ss:Type="String">${escapeXml(value)}</Data></Cell>`).join('')}</Row>`;
  const rows = [Array.from(TRANSACTION_FILE_HEADERS), ...transactions.map(transactionToFileRow)];

  return `<?xml version="1.0" encoding="UTF-8"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
  xmlns:o="urn:schemas-microsoft-com:office:office"
  xmlns:x="urn:schemas-microsoft-com:office:excel"
  xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
  <Worksheet ss:Name="Transactions">
    <Table>${rows.map(rowToXml).join('')}</Table>
  </Worksheet>
</Workbook>`;
};

export const parseCsvRows = (text: string): string[][] => {
  const source = text.replace(/^\uFEFF/, '');
  const rows: string[][] = [];
  let row: string[] = [];
  let field = '';
  let quoted = false;

  for (let index = 0; index < source.length; index += 1) {
    const character = source[index];
    const next = source[index + 1];

    if (character === '"' && quoted && next === '"') {
      field += '"';
      index += 1;
      continue;
    }
    if (character === '"') {
      quoted = !quoted;
      continue;
    }
    if (character === ',' && !quoted) {
      row.push(field);
      field = '';
      continue;
    }
    if ((character === '\n' || character === '\r') && !quoted) {
      if (character === '\r' && next === '\n') index += 1;
      row.push(field);
      if (row.some(value => value.trim())) rows.push(row);
      row = [];
      field = '';
      continue;
    }
    field += character;
  }

  if (field || row.length) {
    row.push(field);
    if (row.some(value => value.trim())) rows.push(row);
  }
  return rows;
};

export const parseSpreadsheetXmlRows = (text: string): string[][] => {
  const document = new DOMParser().parseFromString(text, 'application/xml');
  if (document.querySelector('parsererror')) throw new Error('File XLS không đúng định dạng Excel XML.');

  const rowNodes = Array.from(document.getElementsByTagNameNS('*', 'Row'));
  if (rowNodes.length === 0) throw new Error('Không tìm thấy bảng dữ liệu trong file XLS.');

  return rowNodes.map(row =>
    Array.from(row.getElementsByTagNameNS('*', 'Data')).map(data => data.textContent?.trim() ?? ''),
  );
};

export const parseExcelRows = async (buffer: ArrayBuffer): Promise<string[][]> => {
  const XLSX = await import('xlsx');
  const workbook = XLSX.read(buffer, { type: 'array', cellDates: false, raw: false });
  const sheetName = workbook.SheetNames[0];
  if (!sheetName) throw new Error('File XLS không có worksheet.');
  const sheet = workbook.Sheets[sheetName];
  const rows = XLSX.utils.sheet_to_json<(string | number | boolean | null)[]>(sheet, {
    header: 1,
    raw: false,
    defval: '',
  });
  return rows.map(row => row.map(value => String(value ?? '').trim()));
};

const unescapePdfText = (value: string) =>
  value
    .replace(/\\([\\()])/g, '$1')
    .replace(/\\n/g, ' ')
    .replace(/\\r/g, ' ')
    .replace(/\\t/g, ' ');

export const parseGeneratedPdfRows = (buffer: ArrayBuffer): string[][] => {
  const source = new TextDecoder().decode(buffer);
  const values: string[] = [];
  const pattern = /\(((?:\\.|[^)])*)\)\s*Tj/g;
  let match: RegExpExecArray | null;
  while ((match = pattern.exec(source)) !== null) values.push(unescapePdfText(match[1]));
  if (values.length < 2) throw new Error('Chỉ hỗ trợ import PDF được xuất từ CorePortfolio.');

  const rows = values.map(value => value.split('\t'));
  return [rows[0], ...rows.slice(1)];
};

const headerAliases: Record<string, keyof TransactionImportRow> = {
  id: 'id',
  transactionid: 'id',
  portfolioid: 'portfolioId',
  portfolioname: 'portfolio',
  portfolio: 'portfolio',
  assetid: 'assetId',
  symbol: 'symbol',
  ticker: 'symbol',
  asset: 'asset',
  assetname: 'asset',
  category: 'category',
  categoryname: 'category',
  currency: 'currency',
  type: 'type',
  transactiontype: 'type',
  quantity: 'quantity',
  qty: 'quantity',
  price: 'price',
  fee: 'fee',
  notes: 'notes',
  note: 'notes',
  date: 'date',
  datetime: 'date',
  timestamp: 'date',
};

const resolveHeaderAlias = (header: string): keyof TransactionImportRow | undefined => {
  const normalized = normalizeHeader(header);
  const direct = headerAliases[normalized];
  if (direct) return direct;
  if (normalized.includes('tenccq') || normalized.includes('ticker')) return 'symbol';
  if (normalized.includes('ngaymua') || normalized.includes('ngaygiaodich')) return 'date';
  if (normalized.includes('soluong') || normalized === 'quantity') return 'quantity';
  if (normalized.includes('giamua') || normalized.includes('giatrimua')) return 'price';
  if (normalized.includes('phigiaodich')) return 'fee';
  return undefined;
};

export const rowsToTransactionImportRows = (rows: string[][]): TransactionImportRow[] => {
  if (rows.length < 2) throw new Error('File không có dòng giao dịch để import.');
  let headerRowIndex = -1;
  let headerIndexes = new Map<number, keyof TransactionImportRow>();

  rows.some((row, rowIndex) => {
    const candidateIndexes = new Map<number, keyof TransactionImportRow>();
    row.forEach((header, index) => {
      const alias = resolveHeaderAlias(header);
      if (alias) candidateIndexes.set(index, alias);
    });
    const keys = Array.from(candidateIndexes.values());
    if (keys.includes('quantity') && keys.includes('price') && (keys.includes('symbol') || keys.includes('assetId') || keys.includes('asset'))) {
      headerRowIndex = rowIndex;
      headerIndexes = candidateIndexes;
      return true;
    }
    return false;
  });
  if (headerRowIndex < 0) throw new Error('Không tìm thấy header giao dịch. Hãy dùng file đã export từ CorePortfolio hoặc báo cáo có các cột tài sản, số lượng, giá và ngày.');
  if (!Array.from(headerIndexes.values()).some(key => key === 'quantity' || key === 'assetId' || key === 'symbol')) {
    throw new Error('Không nhận diện được header giao dịch. Hãy dùng file đã export từ CorePortfolio.');
  }

  return rows.slice(headerRowIndex + 1).map(values => {
    const result: TransactionImportRow = {};
    headerIndexes.forEach((key, index) => {
      result[key] = values[index]?.trim() ?? '';
    });
    return result;
  });
};

export const parseFlexibleNumber = (value: string | undefined, fallback = 0) => {
  if (!value?.trim()) return fallback;
  let normalized = value.trim().replace(/[^\d,.-]/g, '');
  const commaIndex = normalized.lastIndexOf(',');
  const dotIndex = normalized.lastIndexOf('.');
  if (commaIndex >= 0 && dotIndex >= 0) {
    const decimalSeparator = commaIndex > dotIndex ? ',' : '.';
    const thousandsSeparator = decimalSeparator === ',' ? '.' : ',';
    normalized = normalized.replaceAll(thousandsSeparator, '').replace(decimalSeparator, '.');
  } else if (commaIndex >= 0) {
    const commaGroups = normalized.split(',');
    normalized = commaGroups.length > 2 || commaGroups.at(-1)?.length === 3
      ? normalized.replaceAll(',', '')
      : normalized.replace(',', '.');
  } else if ((normalized.match(/\./g) ?? []).length > 1) {
    normalized = normalized.replaceAll('.', '');
  }
  const number = Number(normalized);
  return Number.isFinite(number) ? number : fallback;
};

export const parseTransactionType = (value: string | undefined): TransactionTypeValue | null => {
  const normalized = value?.trim().toLowerCase();
  if (!normalized) return TransactionType.Buy;
  if (normalized === '0' || normalized === 'buy' || normalized === 'mua') return TransactionType.Buy;
  if (normalized === '1' || normalized === 'sell' || normalized === 'bán' || normalized === 'ban') return TransactionType.Sell;
  if (normalized === '2' || normalized === 'deposit' || normalized === 'nạp' || normalized === 'nap') return TransactionType.Deposit;
  if (normalized === '3' || normalized === 'withdrawal' || normalized === 'rút' || normalized === 'rut') return TransactionType.Withdrawal;
  if (normalized === '4' || normalized === 'dividend' || normalized === 'cổ tức' || normalized === 'co tuc') return TransactionType.Dividend;
  if (normalized === '5' || normalized === 'earn' || normalized === 'reward' || normalized === 'thưởng' || normalized === 'thuong') return TransactionType.Earn;
  return null;
};

export const parseTransactionDate = (value: string | undefined): Date | null => {
  if (!value?.trim()) return null;
  const trimmed = value.trim();
  const vietnameseDate = trimmed.match(/^(\d{1,2})[/-](\d{1,2})[/-](\d{4})(?:\s+(\d{1,2}):(\d{2}))?$/);
  if (vietnameseDate) {
    const [, day, month, year, hour = '0', minute = '0'] = vietnameseDate;
    const hasExplicitTime = vietnameseDate[4] !== undefined;
    const date = hasExplicitTime
      ? new Date(Number(year), Number(month) - 1, Number(day), Number(hour), Number(minute))
      : new Date(Date.UTC(Number(year), Number(month) - 1, Number(day)));
    return Number.isNaN(date.getTime()) ? null : date;
  }
  const date = new Date(trimmed);
  return Number.isNaN(date.getTime()) ? null : date;
};

const stripDiacritics = (value: string) =>
  value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D');

const pdfSafe = (value: string) =>
  stripDiacritics(value).replace(/[^\x20-\x7E\t]/g, '?').replace(/\\/g, '\\\\').replace(/\(/g, '\\(').replace(/\)/g, '\\)');

const pdfRows = (transactions: GlobalTransactionDto[]) => [
  'Date\tPortfolio\tSymbol\tType\tQuantity\tPrice\tTotal',
  ...transactions.map(transaction => [
    new Date(transaction.date).toISOString().slice(0, 10),
    transaction.portfolioName,
    transaction.symbol,
    formatType(transaction.type),
    formatNumber(transaction.quantity),
    `${formatNumber(transaction.price, 2)} ${transaction.currency}`,
    `${formatNumber(transaction.quantity * transaction.price, 2)} ${transaction.currency}`,
  ].join('\t')),
];

export const transactionsToPdf = (transactions: GlobalTransactionDto[]) => {
  const linesPerPage = 42;
  const lines = pdfRows(transactions);
  const pages = Array.from({ length: Math.max(1, Math.ceil(lines.length / linesPerPage)) }, (_, index) =>
    lines.slice(index * linesPerPage, (index + 1) * linesPerPage),
  );
  const objects: string[] = [];
  const pageObjectIds = pages.map((_, index) => 4 + index * 2);

  objects.push('<< /Type /Catalog /Pages 2 0 R >>');
  objects.push(`<< /Type /Pages /Kids [${pageObjectIds.map(id => `${id} 0 R`).join(' ')}] /Count ${pages.length} >>`);
  objects.push('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>');

  pages.forEach((page, index) => {
    const content = [
      'BT',
      '/F1 7 Tf',
      '36 780 Td',
      '11 TL',
      ...page.map((line, lineIndex) => `(${pdfSafe(line.slice(0, 132))}) Tj${lineIndex < page.length - 1 ? ' T*' : ''}`),
      'ET',
    ].join('\n');
    objects.push(`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents ${5 + index * 2} 0 R >>`);
    objects.push(`<< /Length ${new TextEncoder().encode(content).length} >>\nstream\n${content}\nendstream`);
  });

  let pdf = '%PDF-1.4\n';
  const offsets = [0];
  objects.forEach((object, index) => {
    offsets.push(new TextEncoder().encode(pdf).length);
    pdf += `${index + 1} 0 obj\n${object}\nendobj\n`;
  });
  pdf += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  pdf += offsets.slice(1).map(offset => `${String(offset).padStart(10, '0')} 00000 n \n`).join('');
  pdf += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${new TextEncoder().encode(pdf).length}\n%%EOF`;
  return new Blob([pdf], { type: 'application/pdf' });
};
