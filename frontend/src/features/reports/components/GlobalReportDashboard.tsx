import React, { useEffect, useState } from 'react';
import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { getGlobalReport } from '../api/reportsApi';
import { settingsApi } from '../../admin/api/settingsApi';
import type { GlobalReportDto } from '../types';
import { HistoricalPerformanceChart } from './HistoricalPerformanceChart';
import './GlobalReportDashboard.css';

const COLORS = ['#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#ec4899', '#6366f1', '#14b8a6'];

export const GlobalReportDashboard: React.FC = () => {
  const [reportData, setReportData] = useState<GlobalReportDto | null>(null);
  const [usdToVndRate, setUsdToVndRate] = useState<number>(26309);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const [reportRes, rateRes] = await Promise.all([
          getGlobalReport(),
          settingsApi.getSetting('USD_TO_VND')
        ]);
        setReportData(reportRes);
        if (rateRes) {
          setUsdToVndRate(parseFloat(rateRes));
        }
      } catch (error) {
        console.error('Failed to fetch global report data', error);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  if (loading) {
    return <div className="loading-container">Loading report...</div>;
  }

  if (!reportData) {
    return <div className="error-container">Failed to load report data.</div>;
  }

  const convertToVnd = (value: number, currency: string) => {
    if (currency === 'USD') return value * usdToVndRate;
    return value;
  };

  const formatterVnd = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' });

  let totalInvestedVND = 0;
  let totalValueVND = 0;

  const categoryChartData = reportData.allocationsByCategory.map(cat => {
    const valueVnd = convertToVnd(cat.currentValue, cat.currency);
    const investedVnd = convertToVnd(cat.totalInvested, cat.currency);
    totalInvestedVND += investedVnd;
    totalValueVND += valueVnd;
    return {
      name: cat.categoryName,
      value: valueVnd
    };
  }).filter(d => d.value > 0);

  const portfolioChartData = reportData.allocationsByPortfolio.map(port => {
    let portTotalValueVnd = 0;
    port.currencies.forEach(curr => {
      portTotalValueVnd += convertToVnd(curr.currentValue, curr.currency);
    });
    
    return {
      name: port.portfolioName,
      value: portTotalValueVnd
    };
  }).filter(d => d.value > 0);

  const renderCustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      return (
        <div className="custom-tooltip glass-panel" style={{ padding: '10px', fontSize: '14px' }}>
          <p className="label" style={{ margin: 0, fontWeight: 'bold' }}>{`${payload[0].name}`}</p>
          <p className="intro" style={{ margin: 0, color: payload[0].payload.fill }}>
            {formatterVnd.format(payload[0].value)}
          </p>
        </div>
      );
    }
    return null;
  };

  return (
    <div className="report-dashboard">
      <div className="report-header">
        <h1>Global Portfolio Report</h1>
        <p>Tỷ giá hiện tại: 1 USD = {formatterVnd.format(usdToVndRate)}</p>
      </div>
      
      <div className="report-summary-cards">
        <div className="summary-card glass-panel">
          <h3>Total Invested</h3>
          <p className="summary-value">{formatterVnd.format(totalInvestedVND)}</p>
        </div>
        <div className="summary-card glass-panel">
          <h3>Current Value</h3>
          <p className="summary-value">{formatterVnd.format(totalValueVND)}</p>
        </div>
        <div className="summary-card glass-panel">
          <h3>Total P/L</h3>
          <p className={`summary-value ${totalValueVND >= totalInvestedVND ? 'positive' : 'negative'}`}>
            {totalValueVND >= totalInvestedVND ? '+' : ''}
            {formatterVnd.format(totalValueVND - totalInvestedVND)}
          </p>
        </div>
      </div>

      <div className="charts-container">
        <div className="chart-wrapper glass-panel">
          <h2>Allocation by Category</h2>
          <div style={{ width: '100%', height: 350 }}>
            {categoryChartData.length > 0 ? (
              <ResponsiveContainer>
                <PieChart>
                  <Pie
                    data={categoryChartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={70}
                    outerRadius={110}
                    paddingAngle={5}
                    dataKey="value"
                    label={({name, percent}) => `${name} ${((percent || 0) * 100).toFixed(1)}%`}
                    stroke="none"
                  >
                    {categoryChartData.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip content={renderCustomTooltip} />
                  <Legend verticalAlign="bottom" height={36}/>
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="no-data">No data available</div>
            )}
          </div>
        </div>

        <div className="chart-wrapper glass-panel">
          <h2>Allocation by Portfolio</h2>
          <div style={{ width: '100%', height: 350 }}>
            {portfolioChartData.length > 0 ? (
              <ResponsiveContainer>
                <PieChart>
                  <Pie
                    data={portfolioChartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={70}
                    outerRadius={110}
                    paddingAngle={5}
                    dataKey="value"
                    label={({name, percent}) => `${name} ${((percent || 0) * 100).toFixed(1)}%`}
                    stroke="none"
                  >
                    {portfolioChartData.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[(index + 3) % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip content={renderCustomTooltip} />
                  <Legend verticalAlign="bottom" height={36}/>
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="no-data">No data available</div>
            )}
          </div>
        </div>
      </div>
      
      <HistoricalPerformanceChart />
    </div>
  );
};
