import { apiClient } from '../../../shared/api/baseClient';

export interface CashAccountDto {
  id: string;
  portfolioId: string;
  currency: string;
  balance: number;
}

export interface AdjustCashBalanceCommand {
  portfolioId: string;
  currency: string;
  amount: number;
  isDeposit: boolean;
  description?: string;
  occurredAt?: string;
}

export const cashAccountsApi = {
  getAccounts: (portfolioId?: string) => 
    apiClient<CashAccountDto[]>(portfolioId ? `/cash-accounts?portfolioId=${portfolioId}` : '/cash-accounts'),
  
  adjustBalance: (command: AdjustCashBalanceCommand) => 
    apiClient<CashAccountDto>('/cash-accounts/adjust-balance', {
      method: 'POST',
      body: JSON.stringify(command)
    }),
};
