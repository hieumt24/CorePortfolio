export type TransactionAmountField = 'quantity' | 'price' | 'total';

export interface TransactionAmounts {
  quantity: number;
  price: number;
  total: number;
}

export const deriveTransactionAmount = (
  values: TransactionAmounts,
  explicitFields: TransactionAmountField[],
): Partial<TransactionAmounts> => {
  if (explicitFields.length !== 2) return {};
  const [firstField, secondField] = explicitFields;
  if (values[firstField] <= 0 || values[secondField] <= 0) return {};

  const derivedField = (['quantity', 'price', 'total'] as TransactionAmountField[])
    .find(field => !explicitFields.includes(field));

  if (derivedField === 'total') return { total: values.quantity * values.price };
  if (derivedField === 'price') return { price: values.total / values.quantity };
  if (derivedField === 'quantity') return { quantity: values.total / values.price };
  return {};
};
