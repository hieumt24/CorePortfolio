import React from 'react';
import './Skeleton.css';

interface SkeletonProps {
  type?: 'text' | 'title' | 'circle' | 'rect';
  width?: string | number;
  height?: string | number;
  className?: string;
  style?: React.CSSProperties;
}

export const Skeleton: React.FC<SkeletonProps> = ({ 
  type = 'text', 
  width, 
  height, 
  className = '',
  style 
}) => {
  const combinedStyle: React.CSSProperties = {
    ...style,
    width: width || (type === 'text' || type === 'title' ? '100%' : 'auto'),
    height: height,
  };

  return (
    <div 
      className={`skeleton ${type} ${className}`} 
      style={combinedStyle} 
    />
  );
};

export const DashboardSkeleton: React.FC = () => {
  return (
    <div className="skeleton-container" style={{ paddingBottom: '3rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2rem' }}>
        <div>
          <Skeleton type="title" width="250px" height="32px" />
          <Skeleton type="text" width="400px" />
        </div>
        <Skeleton type="rect" width="100px" height="40px" />
      </div>

      {/* Grid Row 1 */}
      <div className="skeleton-dashboard-grid" style={{ gridTemplateColumns: '1fr 1fr' }}>
        <div className="glass-panel" style={{ padding: '1.5rem', height: '400px', display: 'flex', flexDirection: 'column' }}>
          <Skeleton type="title" width="40%" />
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '2rem' }}>
            <Skeleton type="circle" width="200px" height="200px" />
            <div style={{ flex: 1 }}>
              <Skeleton type="text" />
              <Skeleton type="text" />
              <Skeleton type="text" />
              <Skeleton type="text" />
            </div>
          </div>
        </div>
        <div className="glass-panel" style={{ padding: '1.5rem', height: '400px', display: 'flex', flexDirection: 'column' }}>
          <Skeleton type="title" width="40%" />
          <div style={{ flex: 1, marginTop: '1rem' }}>
            <Skeleton type="rect" width="100%" height="100%" />
          </div>
        </div>
      </div>

      {/* Grid Row 2 */}
      <div className="skeleton-dashboard-grid" style={{ gridTemplateColumns: '1fr' }}>
        <div className="glass-panel" style={{ padding: '1.5rem', height: '300px', display: 'flex', flexDirection: 'column' }}>
          <Skeleton type="title" width="30%" />
          <div style={{ flex: 1, marginTop: '1rem' }}>
            <Skeleton type="rect" width="100%" height="100%" />
          </div>
        </div>
      </div>
    </div>
  );
};

export const GlobalReportSkeleton: React.FC = () => {
  return (
    <div className="skeleton-container">
      {/* Header */}
      <div style={{ marginBottom: '2rem' }}>
        <Skeleton type="title" width="300px" height="32px" />
        <Skeleton type="text" width="200px" />
      </div>
      
      {/* Summary Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
        <div className="glass-panel" style={{ padding: '1.5rem' }}>
          <Skeleton type="title" width="150px" />
          <Skeleton type="rect" width="200px" height="40px" />
        </div>
        <div className="glass-panel" style={{ padding: '1.5rem' }}>
          <Skeleton type="title" width="150px" />
          <Skeleton type="rect" width="200px" height="40px" />
        </div>
      </div>

      {/* Performance Bar */}
      <div style={{ marginBottom: '2rem' }}>
        <Skeleton type="rect" width="100%" height="60px" />
      </div>

      {/* Perf Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '2rem' }}>
        {[1, 2, 3, 4].map(i => (
          <div key={i} className="glass-panel" style={{ padding: '1.5rem' }}>
            <Skeleton type="title" width="100px" />
            <Skeleton type="text" width="120px" height="24px" />
            <Skeleton type="rect" width="80px" height="24px" style={{ marginTop: '0.5rem' }} />
          </div>
        ))}
      </div>

      {/* Pie Charts */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '2rem', marginBottom: '2rem' }}>
        <div className="glass-panel" style={{ padding: '1.5rem', height: '400px' }}>
          <Skeleton type="title" width="200px" />
          <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '300px' }}>
            <Skeleton type="circle" width="220px" height="220px" />
          </div>
        </div>
        <div className="glass-panel" style={{ padding: '1.5rem', height: '400px' }}>
          <Skeleton type="title" width="200px" />
          <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '300px' }}>
            <Skeleton type="circle" width="220px" height="220px" />
          </div>
        </div>
      </div>
    </div>
  );
};
