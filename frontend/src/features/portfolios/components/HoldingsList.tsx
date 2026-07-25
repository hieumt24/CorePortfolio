import React, { useMemo, useState } from 'react';
import type { AssetSummaryDto } from '../types';

type PositionFilter = 'open' | 'closed' | 'all';
type SortKey = 'weight' | 'value' | 'pnl' | 'pnlPercent' | 'symbol';

interface HoldingRow {
  asset: AssetSummaryDto;
  valueVnd: number;
  weight: number;
  totalPnl: number;
  pnlPercentage: number | null;
  isClosed: boolean;
}

interface HoldingsListProps {
  assets: AssetSummaryDto[];
  portfolioValueVnd: number;
  usdToVndRate: number;
  formatCurrency: (value: number | undefined | null, currency: string | null | undefined) => string;
  onSelectAsset: (asset: AssetSummaryDto) => void;
}

const isEffectivelyZero = (value: number) => Math.abs(value) < 0.00000001;

export const HoldingsList: React.FC<HoldingsListProps> = ({
  assets,
  portfolioValueVnd,
  usdToVndRate,
  formatCurrency,
  onSelectAsset,
}) => {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('all');
  const [currency, setCurrency] = useState('all');
  const [position, setPosition] = useState<PositionFilter>('open');
  const [sortBy, setSortBy] = useState<SortKey>('weight');

  const rows = useMemo<HoldingRow[]>(() => assets.map(asset => {
    const valueVnd = asset.currency === 'USD'
      ? asset.currentValue * usdToVndRate
      : asset.currentValue;
    const totalPnl = (asset.realizedPnl || 0) + (asset.unrealizedPnl || 0);

    return {
      asset,
      valueVnd,
      weight: portfolioValueVnd > 0 ? (valueVnd / portfolioValueVnd) * 100 : 0,
      totalPnl,
      pnlPercentage: asset.totalBought > 0 ? (totalPnl / asset.totalBought) * 100 : null,
      isClosed: isEffectivelyZero(asset.totalQuantity || 0),
    };
  }), [assets, portfolioValueVnd, usdToVndRate]);

  const categories = useMemo(
    () => [...new Set(assets.map(asset => asset.categoryName || 'Uncategorized'))].sort(),
    [assets],
  );
  const currencies = useMemo(
    () => [...new Set(assets.map(asset => asset.currency || 'VND'))].sort(),
    [assets],
  );

  const visibleRows = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase();
    return rows
      .filter(({ asset, isClosed }) => {
        const matchesSearch = !normalizedSearch
          || asset.symbol.toLocaleLowerCase().includes(normalizedSearch)
          || asset.name.toLocaleLowerCase().includes(normalizedSearch);
        const matchesCategory = category === 'all' || asset.categoryName === category;
        const matchesCurrency = currency === 'all' || asset.currency === currency;
        const matchesPosition = position === 'all'
          || (position === 'closed' ? isClosed : !isClosed);
        return matchesSearch && matchesCategory && matchesCurrency && matchesPosition;
      })
      .sort((a, b) => {
        if (sortBy === 'symbol') return a.asset.symbol.localeCompare(b.asset.symbol);
        if (sortBy === 'value') return b.valueVnd - a.valueVnd;
        if (sortBy === 'pnl') return b.totalPnl - a.totalPnl;
        if (sortBy === 'pnlPercent') return (b.pnlPercentage ?? -Infinity) - (a.pnlPercentage ?? -Infinity);
        return b.weight - a.weight;
      });
  }, [category, currency, position, rows, search, sortBy]);

  const hasFilters = Boolean(search) || category !== 'all' || currency !== 'all' || position !== 'open';
  const resetFilters = () => {
    setSearch('');
    setCategory('all');
    setCurrency('all');
    setPosition('open');
    setSortBy('weight');
  };

  return (
    <section className="holdings-panel glass-panel" aria-labelledby="holdings-heading">
      <div className="holdings-heading-row">
        <div>
          <h2 id="holdings-heading">Holdings</h2>
          <p>{visibleRows.length} of {assets.length} assets</p>
        </div>
        {hasFilters && (
          <button type="button" className="clear-filters" onClick={resetFilters}>
            Clear filters
          </button>
        )}
      </div>

      <div className="holdings-toolbar" aria-label="Filter and sort holdings">
        <label className="holding-search">
          <span className="sr-only">Search holdings</span>
          <input
            type="search"
            value={search}
            onChange={event => setSearch(event.target.value)}
            placeholder="Search symbol or asset"
          />
        </label>
        <label>
          <span className="sr-only">Filter by category</span>
          <select value={category} onChange={event => setCategory(event.target.value)}>
            <option value="all">All categories</option>
            {categories.map(item => <option key={item} value={item}>{item}</option>)}
          </select>
        </label>
        <label>
          <span className="sr-only">Filter by currency</span>
          <select value={currency} onChange={event => setCurrency(event.target.value)}>
            <option value="all">All currencies</option>
            {currencies.map(item => <option key={item} value={item}>{item}</option>)}
          </select>
        </label>
        <label>
          <span className="sr-only">Filter by position status</span>
          <select value={position} onChange={event => setPosition(event.target.value as PositionFilter)}>
            <option value="open">Open positions</option>
            <option value="closed">Closed positions</option>
            <option value="all">All positions</option>
          </select>
        </label>
        <label>
          <span className="sr-only">Sort holdings</span>
          <select value={sortBy} onChange={event => setSortBy(event.target.value as SortKey)}>
            <option value="weight">Weight: high to low</option>
            <option value="value">Value: high to low</option>
            <option value="pnl">PnL: high to low</option>
            <option value="pnlPercent">PnL %: high to low</option>
            <option value="symbol">Symbol: A to Z</option>
          </select>
        </label>
      </div>

      {visibleRows.length === 0 ? (
        <div className="holdings-empty">
          <strong>No holdings match these filters</strong>
          <p>Try another keyword or clear the active filters.</p>
          <button type="button" className="btn btn-outline" onClick={resetFilters}>Reset filters</button>
        </div>
      ) : (
        <div className="holdings-table" role="table" aria-label="Portfolio holdings">
          <div className="holdings-table-head" role="row">
            <span role="columnheader">Asset</span>
            <span role="columnheader">Quantity</span>
            <span role="columnheader">Average cost</span>
            <span role="columnheader">Current price</span>
            <span role="columnheader">Value</span>
            <span role="columnheader">Weight</span>
            <span role="columnheader">Total PnL</span>
          </div>
          <div className="holdings-table-body" role="rowgroup">
            {visibleRows.map(({ asset, weight, totalPnl, pnlPercentage, isClosed }) => (
              <button
                type="button"
                role="row"
                className="holding-row"
                key={asset.assetId}
                onClick={() => onSelectAsset(asset)}
                aria-label={`View ${asset.symbol} details`}
              >
                <span className="holding-identity" role="cell">
                  <span className="holding-symbol-line">
                    <strong>{asset.symbol}</strong>
                    {isClosed && <small>Closed</small>}
                  </span>
                  <span>{asset.name}</span>
                  <small>{asset.categoryName} · {asset.currency}</small>
                </span>
                <span className="holding-number" role="cell" data-label="Quantity">
                  {asset.totalQuantity.toLocaleString(undefined, { maximumFractionDigits: 8 })}
                </span>
                <span className="holding-number" role="cell" data-label="Average cost">
                  {formatCurrency(asset.averageCost, asset.currency)}
                </span>
                <span className="holding-number" role="cell" data-label="Current price">
                  {formatCurrency(asset.currentPrice, asset.currency)}
                </span>
                <span className="holding-number holding-value" role="cell" data-label="Value">
                  {formatCurrency(asset.currentValue, asset.currency)}
                </span>
                <span className="holding-weight" role="cell" data-label="Weight">
                  <span>{weight.toFixed(1)}%</span>
                  <span className="weight-track" aria-hidden="true">
                    <span style={{ transform: `scaleX(${Math.min(weight, 100) / 100})` }} />
                  </span>
                </span>
                <span
                  className={`holding-pnl ${totalPnl >= 0 ? 'text-success' : 'text-danger'}`}
                  role="cell"
                  data-label="Total PnL"
                >
                  <strong>{totalPnl > 0 ? '+' : ''}{formatCurrency(totalPnl, asset.currency)}</strong>
                  <small>
                    {pnlPercentage === null
                      ? '—'
                      : `${pnlPercentage > 0 ? '+' : ''}${pnlPercentage.toFixed(2)}%`}
                  </small>
                </span>
              </button>
            ))}
          </div>
        </div>
      )}
    </section>
  );
};
