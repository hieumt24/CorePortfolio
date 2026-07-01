import React, { useState, useEffect } from 'react';
import { getBudgetsProgress, setBudget } from '../api/budgetApi';
import type { BudgetProgress } from '../api/budgetApi';
import { apiClient } from '../../../shared/api/baseClient';

export const BudgetsPage: React.FC = () => {
  const [budgets, setBudgets] = useState<BudgetProgress[]>([]);
  const [loading, setLoading] = useState(true);
  const [categories, setCategories] = useState<any[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);

  // Form state
  const [selectedCategory, setSelectedCategory] = useState('');
  const [monthlyLimit, setMonthlyLimit] = useState<number | ''>('');

  const fetchData = async () => {
    try {
      setLoading(true);
      const [budgetsData, categoriesData] = await Promise.all([
        getBudgetsProgress(),
        apiClient<any[]>('/cashflows/categories')
      ]);
      setBudgets(budgetsData);
      setCategories(categoriesData.filter((c: any) => c.type === 'Expense'));
    } catch (error) {
      console.error('Failed to fetch data', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleSaveBudget = async () => {
    if (!selectedCategory || !monthlyLimit) return;
    try {
      await setBudget({ categoryId: selectedCategory, monthlyLimit: Number(monthlyLimit) });
      setIsModalOpen(false);
      fetchData();
    } catch (error) {
      console.error('Failed to save budget', error);
    }
  };

  const getProgressColor = (percentage: number) => {
    if (percentage < 50) return 'bg-green-500';
    if (percentage < 80) return 'bg-yellow-500';
    return 'bg-red-500';
  };

  return (
    <div className="container mx-auto p-4 max-w-4xl">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold">Ngân sách hàng tháng</h1>
        <button 
          onClick={() => setIsModalOpen(true)}
          className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg font-medium"
        >
          + Thêm/Sửa Ngân sách
        </button>
      </div>

      {loading ? (
        <div className="text-center py-10">Đang tải dữ liệu...</div>
      ) : (
        <div className="grid gap-4">
          {budgets.length === 0 ? (
            <div className="text-center py-10 text-gray-500 bg-gray-50 dark:bg-gray-800 rounded-lg">
              Bạn chưa thiết lập ngân sách nào.
            </div>
          ) : (
            budgets.map(budget => (
              <div key={budget.id} className="bg-white dark:bg-gray-800 p-4 rounded-lg shadow-sm border border-gray-100 dark:border-gray-700">
                <div className="flex justify-between items-center mb-2">
                  <div className="flex items-center gap-2">
                    <span className="text-2xl">{budget.categoryIcon}</span>
                    <span className="font-medium text-gray-800 dark:text-gray-100">{budget.categoryName}</span>
                  </div>
                  <div className="text-sm">
                    <span className="font-semibold text-gray-800 dark:text-gray-100">
                      {budget.spentAmount.toLocaleString()} đ
                    </span>
                    <span className="text-gray-500 dark:text-gray-400 mx-1">/</span>
                    <span className="text-gray-500 dark:text-gray-400">
                      {budget.monthlyLimit.toLocaleString()} đ
                    </span>
                  </div>
                </div>
                
                <div className="w-full bg-gray-200 rounded-full h-2.5 dark:bg-gray-700 mt-2">
                  <div 
                    className={`h-2.5 rounded-full ${getProgressColor(budget.progressPercentage)}`} 
                    style={{ width: `${budget.progressPercentage}%` }}
                  ></div>
                </div>
                
                <div className="text-xs text-right mt-1 text-gray-500">
                  {budget.progressPercentage.toFixed(1)}%
                </div>
              </div>
            ))
          )}
        </div>
      )}

      {isModalOpen && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-lg max-w-md w-full p-6 shadow-xl">
            <h2 className="text-xl font-bold mb-4">Thiết lập Ngân sách</h2>
            
            <div className="mb-4">
              <label className="block text-sm font-medium mb-1">Danh mục chi tiêu</label>
              <select 
                value={selectedCategory} 
                onChange={(e) => setSelectedCategory(e.target.value)}
                className="w-full border rounded p-2 dark:bg-gray-700 dark:border-gray-600"
              >
                <option value="">-- Chọn danh mục --</option>
                {categories.map(c => (
                  <option key={c.id} value={c.id}>
                    {c.icon} {c.name}
                  </option>
                ))}
              </select>
            </div>
            
            <div className="mb-6">
              <label className="block text-sm font-medium mb-1">Hạn mức / tháng (VND)</label>
              <input 
                type="number" 
                value={monthlyLimit} 
                onChange={(e) => setMonthlyLimit(e.target.value ? Number(e.target.value) : '')}
                className="w-full border rounded p-2 dark:bg-gray-700 dark:border-gray-600"
                placeholder="VD: 5000000"
              />
            </div>
            
            <div className="flex justify-end gap-2">
              <button 
                onClick={() => setIsModalOpen(false)}
                className="px-4 py-2 border rounded text-gray-600 hover:bg-gray-50 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
              >
                Hủy
              </button>
              <button 
                onClick={handleSaveBudget}
                disabled={!selectedCategory || !monthlyLimit}
                className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
              >
                Lưu
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
