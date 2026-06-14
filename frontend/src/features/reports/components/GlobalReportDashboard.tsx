import React, { useEffect, useState } from 'react';
import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { getGlobalReport, getGlobalHistory } from '../api/reportsApi';
import { settingsApi } from '../../admin/api/settingsApi';
import type { GlobalReportDto, SnapshotDto } from '../types';
import { HistoricalPerformanceChart } from './HistoricalPerformanceChart';
import { InvestedCapitalChart } from './InvestedCapitalChart';
import './GlobalReportDashboard.css';

const COLORS = ['#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#ec4899', '#6366f1', '#14b8a6'];

export const GlobalReportDashboard: React.FC = () => {
  const [reportData, setReportData] = useState<GlobalReportDto | null>(null);
  const [historyData, setHistoryData] = useState<SnapshotDto[]>([]);
  const [usdToVndRate, setUsdToVndRate] = useState<number>(26309);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const [reportRes, rateRes, historyRes] = await Promise.all([
          getGlobalReport(),
          settingsApi.getSetting('USD_TO_VND'),
          getGlobalHistory()
        ]);
        setReportData(reportRes);
        setHistoryData(historyRes);
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

  const calculatePerformance = (daysAgo: number) => {
    if (historyData.length === 0) return { profit: 0, percentage: 0 };
    
    const targetDate = new Date();
    targetDate.setDate(targetDate.getDate() - daysAgo);
    
    // Find the closest snapshot before or equal to targetDate
    let snapshot = historyData.filter(h => new Date(h.date) <= targetDate).pop();
    
    if (!snapshot) {
      // If no snapshot exists that far back, take the oldest one
      snapshot = historyData[0];
    }
    
    const currentProfit = totalValueVND - totalInvestedVND;
    const historicalProfit = snapshot.totalValue - snapshot.totalInvested;
    const profitChange = currentProfit - historicalProfit;
    
    // Return on Investment = Profit Change / Historical Total Value
    const percentage = snapshot.totalValue > 0 ? (profitChange / snapshot.totalValue) * 100 : 0;
    
    return { profit: profitChange, percentage };
  };

  const perf1W = calculatePerformance(7);
  const perf1M = calculatePerformance(30);
  const perf1Y = calculatePerformance(365);
  const perfAll = {
    profit: totalValueVND - totalInvestedVND,
    percentage: totalInvestedVND > 0 ? ((totalValueVND - totalInvestedVND) / totalInvestedVND) * 100 : 0
  };

  const renderPerfCard = (title: string, data: { profit: number, percentage: number }) => {
    const isPositive = data.profit >= 0;
    return (
      <div className="summary-card glass-panel">
        <h3>{title}</h3>
        <p className={`summary-value ${isPositive ? 'positive' : 'negative'}`}>
          {isPositive ? '+' : ''}{formatterVnd.format(data.profit)}
        </p>
        <p className={`perf-badge ${isPositive ? 'positive-bg' : 'negative-bg'}`} style={{ 
          display: 'inline-block', 
          padding: '4px 10px', 
          borderRadius: '12px', 
          fontSize: '0.85rem',
          fontWeight: 'bold',
          marginTop: '0.5rem',
          background: isPositive ? 'rgba(16, 185, 129, 0.15)' : 'rgba(239, 68, 68, 0.15)',
          color: isPositive ? '#10b981' : '#ef4444'
        }}>
          {isPositive ? '▲' : '▼'} {Math.abs(data.percentage).toFixed(2)}%
        </p>
      </div>
    );
  };

  return (
    <div className="report-dashboard">
      <div className="report-header">
        <h1>Global Portfolio Report</h1>
        <p>Tỷ giá hiện tại: 1 USD = {formatterVnd.format(usdToVndRate)}</p>
      </div>
      
      <div className="report-summary-cards" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '1rem' }}>
        <div className="summary-card glass-panel" style={{ gridColumn: 'span 2' }}>
          <h3>Total Invested</h3>
          <p className="summary-value" style={{ fontSize: '2rem' }}>{formatterVnd.format(totalInvestedVND)}</p>
        </div>
        <div className="summary-card glass-panel" style={{ gridColumn: 'span 2' }}>
          <h3>Current Value</h3>
          <p className="summary-value" style={{ fontSize: '2rem' }}>{formatterVnd.format(totalValueVND)}</p>
        </div>
      </div>
      
      <div style={{ marginBottom: '2rem' }}>
        <InvestedCapitalChart totalInvested={totalInvestedVND} currentValue={totalValueVND} />
      </div>
      
      <div className="report-summary-cards" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '2rem' }}>
        {renderPerfCard('1 Week PnL', perf1W)}
        {renderPerfCard('1 Month PnL', perf1M)}
        {renderPerfCard('1 Year PnL', perf1Y)}
        {renderPerfCard('All Time PnL', perfAll)}
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
