import { useEffect, useMemo, useState } from 'react';
import { analyticsApi } from '../api/analyticsApi';
import type {
  AnalyticsOverviewDto,
  AnalyticsScenarioDto,
} from '../types';

interface ScenarioLabProps {
  data: AnalyticsOverviewDto;
}

const confidenceLabels: Record<string, string> = {
  High: 'Tin cậy cao',
  Medium: 'Tin cậy vừa',
  Low: 'Tin cậy thấp',
};

const parseNumericInput = (value: string) => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
};

export const ScenarioLab = ({ data }: ScenarioLabProps) => {
  const [horizonMonths, setHorizonMonths] = useState(12);
  const [monthlyIncomeChange, setMonthlyIncomeChange] = useState(0);
  const [monthlyExpenseChange, setMonthlyExpenseChange] = useState(0);
  const [shocks, setShocks] = useState<Record<string, number>>({});
  const [result, setResult] = useState<AnalyticsScenarioDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const scopeKey = `${data.scope.portfolioId ?? 'all'}:${data.scope.from}:${data.scope.to}:${data.scope.currency}`;
  const money = useMemo(
    () => new Intl.NumberFormat(data.scope.currency === 'VND' ? 'vi-VN' : 'en-US', {
      style: 'currency',
      currency: data.scope.currency,
      maximumFractionDigits: data.scope.currency === 'VND' ? 0 : 2,
    }),
    [data.scope.currency],
  );

  useEffect(() => {
    setShocks(Object.fromEntries(
      data.allocation.map((item) => [item.categoryName, 0]),
    ));
    setResult(null);
    setError(null);
  }, [data.allocation, scopeKey]);

  const applyUniformShock = (changePercentage: number) => {
    setShocks(Object.fromEntries(
      data.allocation.map((item) => [item.categoryName, changePercentage]),
    ));
    setResult(null);
  };

  const runScenario = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await analyticsApi.evaluateScenario({
        portfolioId: data.scope.portfolioId ?? undefined,
        from: data.scope.from.slice(0, 10),
        to: data.scope.to.slice(0, 10),
        currency: data.scope.currency,
        horizonMonths,
        monthlyIncomeChange,
        monthlyExpenseChange,
        shocks: data.allocation.map((item) => ({
          categoryName: item.categoryName,
          changePercentage: shocks[item.categoryName] ?? 0,
        })),
      });
      setResult(response);
    } catch (reason) {
      setError(reason instanceof Error && reason.message
        ? reason.message
        : 'Không thể chạy mô phỏng. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="analytics-scenario-lab" aria-labelledby="scenario-lab-title">
      <div className="analytics-panel-heading">
        <div>
          <span className="analytics-eyebrow">Scenario Lab</span>
          <h2 id="scenario-lab-title">Kiểm tra giả định trước khi hành động</h2>
          <p>
            Thử cú sốc giá và thay đổi thu–chi. Kết quả chỉ so sánh giả định, không dự báo thị trường.
          </p>
        </div>
        <span className="scenario-scope-chip">{data.scope.portfolioName}</span>
      </div>

      <div className="scenario-input-grid">
        <section className="scenario-control-panel" aria-labelledby="scenario-market-title">
          <div className="scenario-section-heading">
            <div>
              <span className="analytics-eyebrow">Biến số 01</span>
              <h3 id="scenario-market-title">Cú sốc theo nhóm tài sản</h3>
            </div>
            <div className="scenario-presets" aria-label="Kịch bản nhanh">
              <button type="button" onClick={() => applyUniformShock(0)}>Đặt lại</button>
              <button type="button" onClick={() => applyUniformShock(-10)}>Giảm 10%</button>
              <button type="button" onClick={() => applyUniformShock(-25)}>Stress −25%</button>
            </div>
          </div>

          {data.allocation.length === 0 ? (
            <div className="analytics-empty-state">
              <strong>Chưa có giá trị tài sản để stress test</strong>
              <p>Bạn vẫn có thể mô phỏng phần dòng tiền bên dưới.</p>
            </div>
          ) : (
            <div className="scenario-shock-list">
              {data.allocation.map((item) => {
                const value = shocks[item.categoryName] ?? 0;
                return (
                  <label key={item.categoryName} className="scenario-shock-row">
                    <span>
                      <strong>{item.categoryName}</strong>
                      <small>{money.format(item.totalValue)} · {item.percentage.toFixed(1)}%</small>
                    </span>
                    <input
                      type="range"
                      min="-50"
                      max="50"
                      step="1"
                      value={value}
                      aria-label={`Thay đổi giá ${item.categoryName}`}
                      onChange={(event) => {
                        setShocks((current) => ({
                          ...current,
                          [item.categoryName]: Number(event.target.value),
                        }));
                        setResult(null);
                      }}
                    />
                    <output className={value < 0 ? 'is-negative' : value > 0 ? 'is-positive' : ''}>
                      {value > 0 ? '+' : ''}{value}%
                    </output>
                  </label>
                );
              })}
            </div>
          )}
        </section>

        <section className="scenario-control-panel" aria-labelledby="scenario-cashflow-title">
          <span className="analytics-eyebrow">Biến số 02</span>
          <h3 id="scenario-cashflow-title">Dòng tiền trong kỳ</h3>
          <div className="scenario-field-grid">
            <label>
              <span>Thời hạn</span>
              <select
                value={horizonMonths}
                onChange={(event) => {
                  setHorizonMonths(Number(event.target.value));
                  setResult(null);
                }}
              >
                {[3, 6, 12, 24].map((months) => (
                  <option key={months} value={months}>{months} tháng</option>
                ))}
              </select>
            </label>
            <label>
              <span>Thay đổi thu mỗi tháng</span>
              <input
                type="number"
                value={monthlyIncomeChange}
                step={data.scope.currency === 'VND' ? 100000 : 10}
                onChange={(event) => {
                  setMonthlyIncomeChange(parseNumericInput(event.target.value));
                  setResult(null);
                }}
              />
            </label>
            <label>
              <span>Thay đổi chi mỗi tháng</span>
              <input
                type="number"
                value={monthlyExpenseChange}
                step={data.scope.currency === 'VND' ? 100000 : 10}
                onChange={(event) => {
                  setMonthlyExpenseChange(parseNumericInput(event.target.value));
                  setResult(null);
                }}
              />
            </label>
          </div>
          <div className="scenario-baseline-note">
            <span>Dòng tiền nền bình quân</span>
            <strong>
              {money.format(
                data.cashflow.length === 0
                  ? 0
                  : data.cashflow.reduce((sum, item) => sum + item.netFlow, 0) / data.cashflow.length,
              )}/tháng
            </strong>
            <small>Từ {data.cashflow.length} tháng trong phạm vi hiện tại.</small>
          </div>
        </section>
      </div>

      <div className="scenario-run-bar">
        <p>
          Kết quả mới sẽ thay thế kết quả trước đó và không được lưu vào tài khoản.
        </p>
        <button
          type="button"
          className="analytics-primary-button"
          disabled={loading}
          onClick={() => void runScenario()}
        >
          {loading ? 'Đang tính mô phỏng…' : 'Chạy mô phỏng'}
        </button>
      </div>

      {error && (
        <div className="scenario-error" role="alert">
          <strong>Không thể hoàn tất mô phỏng</strong>
          <span>{error}</span>
        </div>
      )}

      {result && (
        <div className="scenario-results" aria-live="polite">
          <div className="scenario-result-header">
            <div>
              <span className="analytics-eyebrow">Kết quả so sánh</span>
              <h3>Điều gì thay đổi sau {result.horizonMonths} tháng?</h3>
            </div>
            <span className={`scenario-confidence is-${result.confidence.toLowerCase()}`}>
              {confidenceLabels[result.confidence] ?? result.confidence}
            </span>
          </div>

          <div className="scenario-result-grid">
            <article>
              <span>Giá trị tài sản sau cú sốc</span>
              <strong>{money.format(result.outcome.stressedPortfolioValue)}</strong>
              <small className={result.outcome.portfolioValueChange < 0 ? 'is-negative' : 'is-positive'}>
                {result.outcome.portfolioValueChange >= 0 ? '+' : ''}
                {money.format(result.outcome.portfolioValueChange)}
                {' · '}
                {result.outcome.portfolioValueChangePercentage.toFixed(1)}%
              </small>
            </article>
            <article>
              <span>Dòng tiền tháng trong kịch bản</span>
              <strong>{money.format(result.outcome.scenarioMonthlyNetFlow)}</strong>
              <small>
                Nền: {money.format(result.baseline.averageMonthlyNetFlow)}
              </small>
            </article>
            <article>
              <span>Chênh lệch dòng tiền lũy kế</span>
              <strong>{money.format(result.outcome.cumulativeNetFlowDifference)}</strong>
              <small>So với việc giữ nguyên thu–chi.</small>
            </article>
            <article>
              <span>Chênh lệch kế hoạch tổng hợp</span>
              <strong>{money.format(result.outcome.combinedPlanningDelta)}</strong>
              <small>Tác động giá cộng chênh lệch dòng tiền, không phải NAV dự báo.</small>
            </article>
          </div>

          {result.outcome.breakEvenMonthlyImprovement > 0 && (
            <div className="scenario-break-even">
              <span>Cần cải thiện tối thiểu</span>
              <strong>{money.format(result.outcome.breakEvenMonthlyImprovement)}/tháng</strong>
              <p>để dòng tiền trong kịch bản trở về mức hòa vốn.</p>
            </div>
          )}

          {result.allocations.length > 0 && (
            <div className="analytics-table-wrap scenario-allocation-table">
              <table>
                <thead>
                  <tr>
                    <th>Nhóm tài sản</th>
                    <th>Giả định</th>
                    <th>Hiện tại</th>
                    <th>Sau cú sốc</th>
                    <th>Tỷ trọng mới</th>
                  </tr>
                </thead>
                <tbody>
                  {result.allocations.map((item) => (
                    <tr key={item.categoryName}>
                      <td>{item.categoryName}</td>
                      <td className={item.shockPercentage < 0 ? 'is-negative' : 'is-positive'}>
                        {item.shockPercentage > 0 ? '+' : ''}{item.shockPercentage.toFixed(0)}%
                      </td>
                      <td>{money.format(item.currentValue)}</td>
                      <td>{money.format(item.stressedValue)}</td>
                      <td>{item.stressedPercentage.toFixed(1)}%</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <details className="scenario-methodology">
            <summary>Giả định và giới hạn phương pháp</summary>
            <ul>
              {result.assumptions.map((assumption) => <li key={assumption}>{assumption}</li>)}
            </ul>
            <p>{result.disclaimer}</p>
            <small>Phiên bản: {result.methodologyVersion}</small>
          </details>
        </div>
      )}
    </section>
  );
};
