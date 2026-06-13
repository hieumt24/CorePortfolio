import React, { createContext, useContext, useState, useCallback } from 'react';
import type { ReactNode } from 'react';
import './Notification.css';

export type NotificationType = 'success' | 'error' | 'info';

interface NotificationItem {
  id: string;
  message: string;
  type: NotificationType;
}

interface NotificationContextType {
  showNotification: (message: string, type: NotificationType) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const useNotification = () => {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }
  return context;
};

export const NotificationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);

  const showNotification = useCallback((message: string, type: NotificationType) => {
    const id = Math.random().toString(36).substr(2, 9);
    setNotifications((prev) => [...prev, { id, message, type }]);

    setTimeout(() => {
      setNotifications((prev) => prev.filter((n) => n.id !== id));
    }, 4000);
  }, []);

  return (
    <NotificationContext.Provider value={{ showNotification }}>
      {children}
      <div className="notification-container">
        {notifications.map((n) => (
          <div key={n.id} className={`notification-toast notification-${n.type}`}>
            <div className="notification-icon">
              {n.type === 'success' && '✓'}
              {n.type === 'error' && '✕'}
              {n.type === 'info' && 'ℹ'}
            </div>
            <div className="notification-message">{n.message}</div>
            <button className="notification-close" onClick={() => setNotifications((prev) => prev.filter(item => item.id !== n.id))}>×</button>
          </div>
        ))}
      </div>
    </NotificationContext.Provider>
  );
};
