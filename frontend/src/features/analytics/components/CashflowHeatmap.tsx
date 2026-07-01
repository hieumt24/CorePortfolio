import React, { useState, useEffect } from 'react';
import { analyticsApi } from '../api/analyticsApi';

export const CashflowHeatmap: React.FC = () => {
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const heatmapData = await analyticsApi.getCashflowHeatmap();
        setData(heatmapData);
      } catch (error) {
        console.error('Failed to fetch heatmap', error);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  if (loading) return <div>Đang tải biểu đồ...</div>;

  // Generate last 365 days
  const days = [];
  const today = new Date();
  for (let i = 364; i >= 0; i--) {
    const d = new Date(today);
    d.setDate(today.getDate() - i);
    const dateStr = d.toISOString().split('T')[0];
    
    const dayData = data.find(x => x.date === dateStr);
    days.push({
      date: dateStr,
      count: dayData ? dayData.count : 0,
      amount: dayData ? dayData.totalAmount : 0
    });
  }

  const getColor = (count: number) => {
    if (count === 0) return 'bg-gray-100 dark:bg-gray-800';
    if (count === 1) return 'bg-green-200 dark:bg-green-900';
    if (count <= 3) return 'bg-green-400 dark:bg-green-700';
    if (count <= 5) return 'bg-green-600 dark:bg-green-500';
    return 'bg-green-800 dark:bg-green-400';
  };

  return (
    <div className="w-full overflow-x-auto">
      <div className="flex flex-wrap gap-1 w-full" style={{ maxWidth: '800px' }}>
        {days.map((day) => (
          <div 
            key={day.date} 
            className={`w-3 h-3 rounded-sm ${getColor(day.count)}`}
            title={`${day.date}: ${day.count} giao dịch, ${day.amount.toLocaleString()} đ`}
          ></div>
        ))}
      </div>
      <div className="flex items-center gap-2 mt-4 text-xs text-gray-500">
        <span>Ít</span>
        <div className="w-3 h-3 rounded-sm bg-gray-100 dark:bg-gray-800"></div>
        <div className="w-3 h-3 rounded-sm bg-green-200 dark:bg-green-900"></div>
        <div className="w-3 h-3 rounded-sm bg-green-400 dark:bg-green-700"></div>
        <div className="w-3 h-3 rounded-sm bg-green-600 dark:bg-green-500"></div>
        <div className="w-3 h-3 rounded-sm bg-green-800 dark:bg-green-400"></div>
        <span>Nhiều</span>
      </div>
    </div>
  );
};
