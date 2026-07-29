import type { TargetAllocationDto } from '../types';

const percentageTolerance = 0.01;

export interface TargetAllocationDraftState {
  total: number;
  isComplete: boolean;
  isCleared: boolean;
  hasOutOfRangeValue: boolean;
  canSave: boolean;
}

export const getTargetAllocationDraftState = (
  allocations: Pick<TargetAllocationDto, 'targetPercentage'>[]
): TargetAllocationDraftState => {
  const total = allocations.reduce(
    (sum, allocation) => sum + allocation.targetPercentage,
    0
  );
  const isComplete = Math.abs(total - 100) <= percentageTolerance;
  const isCleared = Math.abs(total) <= percentageTolerance;
  const hasOutOfRangeValue = allocations.some(
    allocation => allocation.targetPercentage < 0 || allocation.targetPercentage > 100
  );

  return {
    total,
    isComplete,
    isCleared,
    hasOutOfRangeValue,
    canSave: !hasOutOfRangeValue && (isComplete || isCleared),
  };
};
