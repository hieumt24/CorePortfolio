import { describe, expect, it } from 'vitest';
import { getTargetAllocationDraftState } from './targetAllocationValidation';

describe('getTargetAllocationDraftState', () => {
  it('allows a complete 100 percent plan', () => {
    const result = getTargetAllocationDraftState([
      { targetPercentage: 60 },
      { targetPercentage: 40 },
    ]);

    expect(result.canSave).toBe(true);
    expect(result.isComplete).toBe(true);
  });

  it('allows clearing the target plan with zero percent', () => {
    const result = getTargetAllocationDraftState([
      { targetPercentage: 0 },
      { targetPercentage: 0 },
    ]);

    expect(result.canSave).toBe(true);
    expect(result.isCleared).toBe(true);
  });

  it('rejects an incomplete plan', () => {
    const result = getTargetAllocationDraftState([
      { targetPercentage: 75 },
    ]);

    expect(result.canSave).toBe(false);
    expect(result.total).toBe(75);
  });

  it('rejects out of range values even when the total is 100', () => {
    const result = getTargetAllocationDraftState([
      { targetPercentage: 110 },
      { targetPercentage: -10 },
    ]);

    expect(result.canSave).toBe(false);
    expect(result.hasOutOfRangeValue).toBe(true);
  });
});
