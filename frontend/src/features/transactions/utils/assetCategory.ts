export const isCryptoCategory = (categoryName: string) => {
  const normalized = categoryName.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  return normalized.includes('crypto') || normalized.includes('tien ma hoa') || normalized.includes('tien dien tu');
};
