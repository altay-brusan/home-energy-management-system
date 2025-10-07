import { useState } from 'react';

export type Notification = {
  id: string;
  title: string;
  message: string;
  description: string;
  createdAt: string;
  read: boolean;
  severity: 'normal' | 'critical';
};

export type TrashNotification = Notification & { deletedAt: string };

export function useNotifications(initial: Notification[] = []) {
  const [items, setItems] = useState<Notification[]>(initial);
  const [trash, setTrash] = useState<TrashNotification[]>([]);

  const markRead = (id: string) => {
    setItems(prev =>
      prev.map(n => (n.id === id ? { ...n, read: true } : n))
    );
  };

  const markAllRead = () => {
    setItems(prev => prev.map(n => ({ ...n, read: true })));
  };

  const deleteNotification = (id: string) => {
    setItems(prev => {
      const target = prev.find(n => n.id === id);
      if (!target) return prev;
      setTrash(t => [
        ...t,
        { ...target, deletedAt: new Date().toISOString() },
      ]);
      return prev.filter(n => n.id !== id);
    });
  };

  const restoreNotification = (id: string) => {
    setTrash(prev => {
      const target = prev.find(n => n.id === id);
      if (!target) return prev;
      setItems(i => [...i, target]); // restore
      return prev.filter(n => n.id !== id);
    });
  };

  const deleteForever = (id: string) => {
    setTrash(prev => prev.filter(n => n.id !== id));
  };

  return {
    items,
    setItems,
    trash,
    setTrash,
    markRead,
    markAllRead,
    deleteNotification,
    restoreNotification,
    deleteForever,
  };
}
