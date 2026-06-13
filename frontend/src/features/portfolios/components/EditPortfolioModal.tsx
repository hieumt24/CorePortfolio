import React, { useState } from 'react';
import { updatePortfolio } from '../api/portfolioApi';
import './CreatePortfolioModal.css'; // Reuse CSS

interface EditPortfolioModalProps {
  portfolio: {
    portfolioId: string;
    name: string;
  };
  onClose: () => void;
  onSuccess: () => void;
}

export const EditPortfolioModal: React.FC<EditPortfolioModalProps> = ({ portfolio, onClose, onSuccess }) => {
  const [name, setName] = useState(portfolio.name);
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      setError('Name is required');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      await updatePortfolio(portfolio.portfolioId, { name, description });
      onSuccess();
    } catch (err: any) {
      setError(err.message || 'Failed to update portfolio');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content glass-panel" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Edit Portfolio</h2>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>
        
        {error && <div className="error-alert">{error}</div>}

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label htmlFor="name">Portfolio Name</label>
            <input
              id="name"
              type="text"
              value={name}
              onChange={e => setName(e.target.value)}
              placeholder="e.g. Retirement Fund"
              className="glass-input"
              disabled={loading}
              autoFocus
            />
          </div>

          <div className="form-group">
            <label htmlFor="description">Description (Optional)</label>
            <textarea
              id="description"
              value={description}
              onChange={e => setDescription(e.target.value)}
              placeholder="Brief description of your goals"
              className="glass-input"
              disabled={loading}
              rows={3}
            />
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
