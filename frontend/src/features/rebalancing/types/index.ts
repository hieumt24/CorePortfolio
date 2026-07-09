export const RebalanceExecutionPlanStatus = {
  Simulated: 0,
  Applied: 1,
} as const;

export type RebalanceExecutionPlanStatus =
  typeof RebalanceExecutionPlanStatus[keyof typeof RebalanceExecutionPlanStatus];

export const RebalanceExecutionAction = {
  Buy: 0,
  Sell: 1,
} as const;

export type RebalanceExecutionAction =
  typeof RebalanceExecutionAction[keyof typeof RebalanceExecutionAction];

export interface RebalanceExecutionPlanItem {
  id: string;
  categoryId: string;
  categoryName: string;
  action: RebalanceExecutionAction;
  currentValue: number;
  targetValue: number;
  suggestedAmount: number;
  executableAmount: number;
  isCashLimited: boolean;
  priority: number;
}

export interface RebalanceExecutionPlan {
  id: string;
  currency: string;
  status: RebalanceExecutionPlanStatus;
  availableCash: number;
  createdAt: string;
  appliedAt: string | null;
  notes: string;
  items: RebalanceExecutionPlanItem[];
}
