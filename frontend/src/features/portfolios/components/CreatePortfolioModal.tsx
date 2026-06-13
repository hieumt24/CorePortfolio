import React, { useState } from 'react';
import { createPortfolio } from '../api/portfolioApi';
import './CreatePortfolioModal.css';

interface Props {
  onClose: () => void;
  onSuccess: () => void;
}

export const CreatePortfolioModal: React.FC<Props> = ({ onClose, onSuccess }) => {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      setError('Portfolio name is required');
      return;
    }
    
    try {
      setLoading(true);
      setError('');
      await createPortfolio({ name, description });
      onSuccess();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content glass-panel" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Create New Portfolio</h2>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>
        <form onSubmit={handleSubmit} className="modal-form">
          {error && <div className="modal-error">{error}</div>}
          <div className="form-group">
            <label htmlFor="name">Name</label>
            <input 
              id="name"
              type="text" 
              value={name} 
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Long-term Crypto"
              className="glass-input"
              autoFocus
            />
          </div>
          <div className="form-group">
            <label htmlFor="description">Description</label>
            <textarea 
              id="description"
              value={description} 
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Optional description"
              className="glass-input"
              rows={3}
            />
          </div>
          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Creating...' : 'Create Portfolio'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
