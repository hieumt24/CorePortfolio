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

interface PositionedPdfText {
  text: string;
  x: number;
  y: number;
}

interface OkxTradingRow {
  id: string;
  orderId: string;
  time: string;
  tradeType: string;
  symbol: string;
  action: string;
  amount: string;
  tradingUnit: string;
  price: string;
  fee: string;
  feeUnit: string;
  balanceUnit: string;
}

interface BinanceTradingRow {
  entryId: string;
  time: string;
  pair: string;
  side: string;
  price: string;
  executed: string;
  amount: string;
  fee: string;
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
  const metadata = source.match(/%CP_TX_DATA:([A-Za-z0-9+/=]+)/)?.[1];
  if (metadata) {
    try {
      const binary = atob(metadata);
      const bytes = Uint8Array.from(binary, character => character.charCodeAt(0));
      return JSON.parse(new TextDecoder().decode(bytes)) as string[][];
    } catch {
      throw new Error('Metadata giao dịch trong PDF CorePortfolio không hợp lệ.');
    }
  }

  const values: string[] = [];
  const pattern = /\(((?:\\.|[^)])*)\)\s*Tj/g;
  let match: RegExpExecArray | null;
  while ((match = pattern.exec(source)) !== null) values.push(unescapePdfText(match[1]));
  if (values.length < 2) throw new Error('Chỉ hỗ trợ import PDF được xuất từ CorePortfolio.');

  const rows = values.map(value => value.split('\t'));
  return [rows[0], ...rows.slice(1)];
};

const readPdfCell = (items: PositionedPdfText[], minY: number, maxY: number) =>
  items
    .filter(item => item.y >= minY && item.y < maxY)
    .sort((left, right) => left.x - right.x)
    .map(item => item.text.trim())
    .join('')
    .trim();

const extractOkxTradingRows = async (buffer: ArrayBuffer): Promise<OkxTradingRow[]> => {
  const [{ getDocument, GlobalWorkerOptions }, workerModule] = await Promise.all([
    import('pdfjs-dist/legacy/build/pdf.mjs'),
    import('pdfjs-dist/legacy/build/pdf.worker.min.mjs?url'),
  ]);
  if (typeof Worker !== 'undefined') {
    GlobalWorkerOptions.workerSrc = workerModule.default;
  }

  const document = await getDocument({ data: new Uint8Array(buffer) }).promise;
  const rows: OkxTradingRow[] = [];

  try {
    for (let pageNumber = 1; pageNumber <= document.numPages; pageNumber += 1) {
      const page = await document.getPage(pageNumber);
      const content = await page.getTextContent();
      const items: PositionedPdfText[] = content.items.flatMap(item =>
        'str' in item && item.str.trim()
          ? [{
              text: item.str,
              x: item.transform[4],
              y: item.transform[5],
            }]
          : [],
      );

      const dateItems = items.filter(item =>
        /^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/.test(item.text.trim()),
      );

      dateItems.forEach(dateItem => {
        const rowItems = items.filter(item => Math.abs(item.x - dateItem.x) <= 6.5);
        rows.push({
          id: readPdfCell(rowItems, 0, 75),
          orderId: readPdfCell(rowItems, 75, 125),
          time: readPdfCell(rowItems, 125, 185),
          tradeType: readPdfCell(rowItems, 185, 230),
          symbol: readPdfCell(rowItems, 230, 265),
          action: readPdfCell(rowItems, 265, 300),
          amount: readPdfCell(rowItems, 300, 345),
          tradingUnit: readPdfCell(rowItems, 345, 385),
          price: readPdfCell(rowItems, 385, 430),
          fee: readPdfCell(rowItems, 470, 505),
          feeUnit: readPdfCell(rowItems, 505, 540),
          balanceUnit: readPdfCell(rowItems, 765, Number.POSITIVE_INFINITY),
        });
      });
    }
  } finally {
    await document.destroy();
  }

  return rows;
};

const readPdfColumn = (items: PositionedPdfText[], minX: number, maxX: number) =>
  items
    .filter(item => item.x >= minX && item.x < maxX)
    .sort((left, right) => left.x - right.x)
    .map(item => item.text.trim())
    .join('')
    .trim();

const extractBinanceTradingRows = async (buffer: ArrayBuffer): Promise<BinanceTradingRow[]> => {
  const [{ getDocument, GlobalWorkerOptions }, workerModule] = await Promise.all([
    import('pdfjs-dist/legacy/build/pdf.mjs'),
    import('pdfjs-dist/legacy/build/pdf.worker.min.mjs?url'),
  ]);
  if (typeof Worker !== 'undefined') {
    GlobalWorkerOptions.workerSrc = workerModule.default;
  }

  const document = await getDocument({ data: new Uint8Array(buffer) }).promise;
  const rows: BinanceTradingRow[] = [];

  try {
    for (let pageNumber = 1; pageNumber <= document.numPages; pageNumber += 1) {
      const page = await document.getPage(pageNumber);
      const content = await page.getTextContent();
      const items: PositionedPdfText[] = content.items.flatMap(item =>
        'str' in item && item.str.trim()
          ? [{
              text: item.str,
              x: item.transform[4],
              y: item.transform[5],
            }]
          : [],
      );
      const isBinance = items.some(item => item.text.toLowerCase().includes('binance.com')) &&
        items.some(item => /thời gian|time/i.test(item.text)) &&
        items.some(item => /cặp giao dịch|trading pair/i.test(item.text));
      if (!isBinance) continue;

      const dateItems = items.filter(item =>
        item.x < 120 && /^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/.test(item.text.trim()),
      );
      dateItems.forEach((dateItem, rowIndex) => {
        const rowItems = items.filter(item => Math.abs(item.y - dateItem.y) <= 2);
        rows.push({
          entryId: `${pageNumber}-${rowIndex + 1}`,
          time: readPdfColumn(rowItems, 0, 125),
          pair: readPdfColumn(rowItems, 125, 225),
          side: readPdfColumn(rowItems, 225, 325),
          price: readPdfColumn(rowItems, 325, 425),
          executed: readPdfColumn(rowItems, 425, 525),
          amount: readPdfColumn(rowItems, 525, 625),
          fee: readPdfColumn(rowItems, 625, Number.POSITIVE_INFINITY),
        });
      });
    }
  } finally {
    await document.destroy();
  }

  if (rows.length === 0) {
    throw new Error('Không tìm thấy giao dịch trong PDF Binance Spot.');
  }
  return rows;
};

const splitNumberAndUnit = (value: string) => {
  const match = value.trim().match(/^([+-]?(?:\d+(?:\.\d*)?|\.\d+))([A-Za-z0-9]+)$/);
  return match
    ? { value: Math.abs(parseFlexibleNumber(match[1])), unit: match[2].toUpperCase() }
    : null;
};

export const binanceRowsToImportRows = (rows: BinanceTradingRow[]): string[][] => {
  const normalized = rows.flatMap(row => {
    const executed = splitNumberAndUnit(row.executed);
    const amount = splitNumberAndUnit(row.amount);
    const fee = splitNumberAndUnit(row.fee);
    const price = parseFlexibleNumber(row.price);
    const side = row.side.toUpperCase();
    if (!executed || !amount || !fee || price <= 0 || !/^(BUY|SELL)$/.test(side)) return [];
    return [{ row, executed, amount, fee, price, side }];
  });

  const bnbPrices = normalized
    .filter(item => item.executed.unit === 'BNB' && item.amount.unit === 'USDT')
    .map(item => ({
      timestamp: Date.parse(`${item.row.time.replace(' ', 'T')}+07:00`),
      price: item.price,
    }))
    .filter(item => Number.isFinite(item.timestamp));

  const convertedRows = normalized.map(item => {
    let feeInQuote = 0;
    let feeNote = `${item.fee.value} ${item.fee.unit}`;
    if (item.fee.unit === item.amount.unit) {
      feeInQuote = item.fee.value;
    } else if (item.fee.unit === item.executed.unit) {
      feeInQuote = item.fee.value * item.price;
    } else if (item.fee.unit === 'BNB' && bnbPrices.length > 0) {
      const timestamp = Date.parse(`${item.row.time.replace(' ', 'T')}+07:00`);
      const nearest = bnbPrices.reduce((best, candidate) =>
        Math.abs(candidate.timestamp - timestamp) < Math.abs(best.timestamp - timestamp) ? candidate : best,
      );
      feeInQuote = item.fee.value * nearest.price;
      feeNote += ` @ ${nearest.price} ${item.amount.unit}`;
    }

    const currency = item.amount.unit === 'USDT' || item.amount.unit === 'USDC'
      ? 'USD'
      : item.amount.unit;
    return [
      item.executed.unit,
      item.side,
      String(item.executed.value),
      String(item.price),
      String(Number(feeInQuote.toPrecision(15))),
      currency,
      `Binance Spot | Entry ${item.row.entryId} | Fee ${feeNote} | UTC+7`,
      `${item.row.time.replace(' ', 'T')}+07:00`,
    ];
  });

  if (convertedRows.length === 0) {
    throw new Error('Không tìm thấy giao dịch Buy/Sell hợp lệ trong PDF Binance Spot.');
  }

  return [
    ['Symbol', 'Type', 'Quantity', 'Price', 'Fee', 'Currency', 'Notes', 'Date'],
    ...convertedRows,
  ];
};

const parseBinancePdfRows = async (buffer: ArrayBuffer) =>
  binanceRowsToImportRows(await extractBinanceTradingRows(buffer));

const okxRowsToImportRows = (rows: OkxTradingRow[]): string[][] => {
  const spotRows = rows.filter(row =>
    row.tradeType.toLowerCase() === 'spot' &&
    /^(buy|sell)$/i.test(row.action),
  );

  const convertedRows = spotRows.flatMap(row => {
    const [baseSymbol, quoteSymbol] = row.symbol.toUpperCase().split('-');
    if (!baseSymbol || !quoteSymbol || row.balanceUnit.toUpperCase() !== baseSymbol) return [];

    const quantity = Math.abs(parseFlexibleNumber(row.amount));
    const price = parseFlexibleNumber(row.price);
    if (quantity <= 0 || price <= 0) return [];

    const matchingLeg = spotRows.find(candidate => {
      if (
        candidate === row ||
        candidate.orderId !== row.orderId ||
        candidate.time !== row.time ||
        candidate.price !== row.price ||
        candidate.balanceUnit.toUpperCase() !== quoteSymbol
      ) return false;

      const quoteAmount = Math.abs(parseFlexibleNumber(candidate.amount));
      const expectedQuoteAmount = quantity * price;
      const tolerance = Math.max(0.00000001, expectedQuoteAmount * 0.000001);
      return Math.abs(quoteAmount - expectedQuoteAmount) <= tolerance;
    });

    const feeLeg = [row, matchingLeg]
      .filter((candidate): candidate is OkxTradingRow => Boolean(candidate))
      .find(candidate => Math.abs(parseFlexibleNumber(candidate.fee)) > 0);
    const rawFee = Math.abs(parseFlexibleNumber(feeLeg?.fee));
    const fee = feeLeg?.feeUnit.toUpperCase() === baseSymbol ? rawFee * price : rawFee;
    const normalizedCurrency = quoteSymbol === 'USDT' || quoteSymbol === 'USDC'
      ? 'USD'
      : quoteSymbol;

    return [[
      baseSymbol,
      row.action,
      String(quantity),
      String(price),
      String(fee),
      normalizedCurrency,
      `OKX Spot | Order ${row.orderId} | Entry ${row.id} | UTC+8`,
      `${row.time.replace(' ', 'T')}+08:00`,
    ]];
  });

  if (convertedRows.length === 0) {
    throw new Error('Không tìm thấy giao dịch Spot hợp lệ trong PDF Trading History của OKX.');
  }

  return [
    ['Symbol', 'Type', 'Quantity', 'Price', 'Fee', 'Currency', 'Notes', 'Date'],
    ...convertedRows,
  ];
};

export const parsePdfRows = async (buffer: ArrayBuffer): Promise<string[][]> => {
  try {
    return parseGeneratedPdfRows(buffer);
  } catch {
    try {
      return await parseBinancePdfRows(buffer);
    } catch {
      const okxRows = await extractOkxTradingRows(buffer);
      return okxRowsToImportRows(okxRows);
    }
  }
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

const encodePdfMetadata = (rows: string[][]) => {
  const bytes = new TextEncoder().encode(JSON.stringify(rows));
  let binary = '';
  bytes.forEach(byte => {
    binary += String.fromCharCode(byte);
  });
  return btoa(binary);
};

const pdfColor = ([red, green, blue]: [number, number, number]) =>
  `${red.toFixed(3)} ${green.toFixed(3)} ${blue.toFixed(3)}`;

const pdfText = (
  text: string,
  x: number,
  y: number,
  size: number,
  color: [number, number, number] = [0.12, 0.16, 0.25],
  font = 'F1',
) => `${pdfColor(color)} rg BT /${font} ${size} Tf ${x} ${y} Td (${pdfSafe(text)}) Tj ET`;

const pdfRect = (
  x: number,
  y: number,
  width: number,
  height: number,
  color: [number, number, number],
) => `${pdfColor(color)} rg ${x} ${y} ${width} ${height} re f`;

const truncatePdfText = (value: string, maxLength: number) =>
  value.length > maxLength ? `${value.slice(0, Math.max(1, maxLength - 1))}~` : value;

const formatPdfNumber = (value: number) => {
  const absolute = Math.abs(value);
  const maximumFractionDigits = absolute >= 1000 ? 2 : absolute >= 1 ? 4 : 8;
  return formatNumber(value, maximumFractionDigits);
};

export const transactionsToPdf = (
  transactions: GlobalTransactionDto[],
  scopeLabel = 'Tất cả',
) => {
  const rowsPerPage = 22;
  const pages = Array.from(
    { length: Math.max(1, Math.ceil(transactions.length / rowsPerPage)) },
    (_, index) => transactions.slice(index * rowsPerPage, (index + 1) * rowsPerPage),
  );
  const objects: string[] = [];
  const pageObjectIds = pages.map((_, index) => 5 + index * 2);
  const metadataRows = [
    Array.from(TRANSACTION_FILE_HEADERS),
    ...transactions.map(transactionToFileRow),
  ];

  objects.push('<< /Type /Catalog /Pages 2 0 R >>');
  objects.push(`<< /Type /Pages /Kids [${pageObjectIds.map(id => `${id} 0 R`).join(' ')}] /Count ${pages.length} >>`);
  objects.push('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>');
  objects.push('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>');

  pages.forEach((page, index) => {
    const pageNumber = index + 1;
    const buyCount = transactions.filter(transaction => transaction.type === TransactionType.Buy).length;
    const sellCount = transactions.filter(transaction => transaction.type === TransactionType.Sell).length;
    const content = [
      pdfRect(0, 520, 842, 75, [0.035, 0.055, 0.13]),
      pdfRect(0, 520, 9, 75, [0.39, 0.4, 0.95]),
      pdfText('COREPORTFOLIO', 34, 566, 10, [0.65, 0.68, 1], 'F2'),
      pdfText('TRANSACTION REPORT', 34, 540, 22, [1, 1, 1], 'F2'),
      pdfText(`Scope: ${scopeLabel}`, 640, 560, 9, [0.8, 0.82, 0.94], 'F2'),
      pdfText(`Generated: ${new Date().toISOString().slice(0, 10)}`, 640, 542, 8, [0.62, 0.67, 0.8]),
      pdfRect(34, 483, 154, 27, [0.94, 0.95, 1]),
      pdfRect(198, 483, 112, 27, [0.92, 0.99, 0.96]),
      pdfRect(320, 483, 112, 27, [1, 0.95, 0.95]),
      pdfText('TOTAL', 44, 500, 7, [0.39, 0.4, 0.65], 'F2'),
      pdfText(`${transactions.length} transactions`, 44, 489, 10, [0.13, 0.16, 0.3], 'F2'),
      pdfText('BUY', 208, 500, 7, [0.08, 0.55, 0.35], 'F2'),
      pdfText(`${buyCount}`, 208, 489, 10, [0.05, 0.38, 0.25], 'F2'),
      pdfText('SELL', 330, 500, 7, [0.78, 0.22, 0.25], 'F2'),
      pdfText(`${sellCount}`, 330, 489, 10, [0.62, 0.12, 0.18], 'F2'),
      pdfRect(34, 450, 774, 24, [0.12, 0.14, 0.28]),
      ...[
        ['DATE', 40],
        ['PORTFOLIO', 122],
        ['ASSET', 238],
        ['TYPE', 332],
        ['QUANTITY', 398],
        ['PRICE', 493],
        ['FEE', 605],
        ['TOTAL', 690],
      ].map(([label, x]) => pdfText(String(label), Number(x), 459, 7, [0.9, 0.92, 1], 'F2')),
      ...page.flatMap((transaction, rowIndex) => {
        const rowTop = 432 - rowIndex * 18;
        const typeName = formatType(transaction.type);
        const isPositive = transaction.type === TransactionType.Buy ||
          transaction.type === TransactionType.Deposit ||
          transaction.type === TransactionType.Earn;
        return [
          pdfRect(34, rowTop - 5, 774, 18, rowIndex % 2 === 0 ? [0.98, 0.985, 1] : [0.945, 0.955, 0.985]),
          pdfText(new Date(transaction.date).toISOString().slice(0, 10), 40, rowTop, 7),
          pdfText(truncatePdfText(transaction.portfolioName, 18), 122, rowTop, 7),
          pdfText(truncatePdfText(transaction.symbol, 12), 238, rowTop, 7, [0.22, 0.25, 0.45], 'F2'),
          pdfRect(332, rowTop - 3, 48, 12, isPositive ? [0.1, 0.65, 0.43] : [0.9, 0.25, 0.3]),
          pdfText(truncatePdfText(typeName.toUpperCase(), 10), 337, rowTop, 6, [1, 1, 1], 'F2'),
          pdfText(truncatePdfText(formatPdfNumber(transaction.quantity), 14), 398, rowTop, 7),
          pdfText(truncatePdfText(`${formatPdfNumber(transaction.price)} ${transaction.currency}`, 19), 493, rowTop, 7),
          pdfText(truncatePdfText(formatPdfNumber(transaction.fee), 12), 605, rowTop, 7),
          pdfText(
            truncatePdfText(`${formatPdfNumber(transaction.quantity * transaction.price)} ${transaction.currency}`, 19),
            690,
            rowTop,
            7,
            [0.08, 0.1, 0.2],
            'F2',
          ),
        ];
      }),
      pdfRect(34, 28, 774, 1, [0.82, 0.84, 0.91]),
      pdfText('CorePortfolio - Personal investment ledger', 34, 15, 7, [0.45, 0.49, 0.6]),
      pdfText(`Page ${pageNumber} / ${pages.length}`, 744, 15, 7, [0.45, 0.49, 0.6], 'F2'),
    ].join('\n');
    objects.push(`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents ${6 + index * 2} 0 R >>`);
    objects.push(`<< /Length ${new TextEncoder().encode(content).length} >>\nstream\n${content}\nendstream`);
  });

  let pdf = `%PDF-1.4\n%CP_TX_DATA:${encodePdfMetadata(metadataRows)}\n`;
  const offsets = [0];
  objects.forEach((object, index) => {
    offsets.push(new TextEncoder().encode(pdf).length);
    pdf += `${index + 1} 0 obj\n${object}\nendobj\n`;
  });
  const xrefOffset = new TextEncoder().encode(pdf).length;
  pdf += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  pdf += offsets.slice(1).map(offset => `${String(offset).padStart(10, '0')} 00000 n \n`).join('');
  pdf += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xrefOffset}\n%%EOF`;
  return new Blob([pdf], { type: 'application/pdf' });
};
