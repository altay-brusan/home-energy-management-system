import { useCallback, useEffect, useState } from 'react';
import { deviceNotificationsService } from '../api/services/deviceNotificationsService';
import { sitesService } from '../api/services/sitesService';
import { useSitesStore } from '../store/sitesStore';
import {
  DeviceNotificationDto,
  PaginatedListOfDeviceNotificationDto
} from '../api/types/deviceNotifications.types';

export type NotificationSeverityLevel = 'ERROR' | 'WARNING' | 'ALARM' | 'INFO';

export type DeviceNotification = {
  id: string;
  title: string;
  message: string;
  description: string;
  createdAt: string;
  read: boolean;
  severity: 'normal' | 'critical';
  severityLevel: NotificationSeverityLevel;
  deviceId: string;
  siteId: string;
  isDeleted: boolean;
  deviceNotificationType: {
    id: string;
    code: string;
    message: string;
    status: string;
    description: string;
    category: string;
  };
  deviceManufacturer: string;
  siteName: string;
  displayMessage?: string;
  resolvedCode?: string;
  sourceDevice?: string;
};

const PAGE_SIZE = 20;

export interface DeviceNotificationFilters {
  isToday?: boolean | null;
  isProcessed?: boolean | null;
  category?: string | null; // Device category (inverter, battery, etc.)
  severity?: string | null; // Severity level (WARNING, ERROR, ALARM)
  includeDeleted?: boolean | null;
}

export function useDeviceNotifications (filters?: DeviceNotificationFilters) {
  const { selectedSite } = useSitesStore();

  const [notifications, setNotifications] = useState<DeviceNotification[]>([]);
  const [deletedNotifications, setDeletedNotifications] = useState<DeviceNotification[]>([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const mapApiNotificationToNotification = (apiNotification: DeviceNotificationDto): DeviceNotification => {
    let parsedAdditionalData = '';
    let severityLevel: NotificationSeverityLevel = 'INFO';
    let displayMessage: string | undefined;
    let resolvedCode: string | undefined;
    let sourceDevice: string | undefined;

    try {
      if (apiNotification.additionalData) {
        const parsed = JSON.parse(apiNotification.additionalData);

        // Extract severity from additionalData
        if (parsed.severity) {
          const sev = parsed.severity.toUpperCase();
          if (sev === 'ERROR' || sev === 'WARNING' || sev === 'ALARM') {
            severityLevel = sev as NotificationSeverityLevel;
          }
        }

        // Extract displayMessage
        if (parsed.displayMessage) {
          displayMessage = parsed.displayMessage;
        }

        // Extract resolvedCode
        if (parsed.resolvedCode) {
          resolvedCode = parsed.resolvedCode;
        }

        // Extract sourceDevice
        if (parsed.sourceDevice) {
          sourceDevice = parsed.sourceDevice;
        }

        const parts: string[] = [];

        if (parsed.DeviceName) {
          parts.push(`Device: ${parsed.DeviceName}`);
        }
        if (parsed.RandomValue) {
          parts.push(`Value: ${parsed.RandomValue}`);
        }

        Object.keys(parsed).forEach(key => {
          if (key !== 'DeviceName' && key !== 'RandomValue') {
            parts.push(`${key}: ${parsed[key]}`);
          }
        });

        parsedAdditionalData = parts.join('\n');
      }
    } catch (error) {
      parsedAdditionalData = apiNotification.additionalData || '';
    }

    const mapped = {
      id: apiNotification.id,
      title: apiNotification.deviceNotificationType?.message || 'Notification',
      message: apiNotification.deviceNotificationType?.description || 'No description available',
      description: parsedAdditionalData,
      createdAt: apiNotification.created,
      read: apiNotification.isProcessed,
      severity: (apiNotification.deviceNotificationType?.category === 'critical' ? 'critical' : 'normal') as 'normal' | 'critical',
      severityLevel,
      deviceId: apiNotification.deviceId,
      siteId: apiNotification.siteId,
      isDeleted: apiNotification.isDeleted,
      deviceNotificationType: {
        id: apiNotification.deviceNotificationType.id,
        code: apiNotification.deviceNotificationType.code,
        message: apiNotification.deviceNotificationType.message,
        status: apiNotification.deviceNotificationType.status,
        description: apiNotification.deviceNotificationType.description,
        category: apiNotification.deviceNotificationType.category,
      },
      deviceManufacturer: apiNotification.deviceManufacturer,
      siteName: apiNotification.siteName,
      displayMessage,
      resolvedCode,
      sourceDevice,
    };

    return mapped;
  };

  const fetchNotifications = useCallback(async (pageNumber: number = 1, isRefresh: boolean = false) => {
    try {
      if (pageNumber === 1) {
        setError(null);
        if (isRefresh) {
          setRefreshing(true);
        } else {
          setLoading(true);
        }
      } else {
        setLoadingMore(true);
      }

      const siteId = selectedSite?.id;

      if (!siteId) {
        const sites = await sitesService.getUserSites();
        const firstSiteId = sites?.siteList?.[0]?.id;
        if (!firstSiteId) {
          throw new Error('No site found for user');
        }
        const response: PaginatedListOfDeviceNotificationDto = await deviceNotificationsService.getDeviceNotifications({
          SiteId: firstSiteId,
          PageNumber: pageNumber,
          PageSize: PAGE_SIZE,
          IsToday: filters?.isToday,
          IsProcessed: filters?.isProcessed,
          Category: filters?.category,
          Severity: filters?.severity,
          IncludeDeleted: filters?.includeDeleted,
        });

        const mappedNotifications = response.items.map(mapApiNotificationToNotification);
        if (pageNumber === 1) {
          setNotifications(mappedNotifications);
        } else {
          setNotifications(prev => [...prev, ...mappedNotifications]);
        }
        setHasNextPage(response.hasNextPage);
        setCurrentPage(pageNumber);
        setTotalCount(response.totalCount);
        return;
      }

      const response: PaginatedListOfDeviceNotificationDto = await deviceNotificationsService.getDeviceNotifications({
        SiteId: siteId,
        PageNumber: pageNumber,
        PageSize: PAGE_SIZE,
        IsToday: filters?.isToday,
        IsProcessed: filters?.isProcessed,
        Category: filters?.category,
        Severity: filters?.severity,
        IncludeDeleted: filters?.includeDeleted,
      });

      const mappedNotifications = response.items.map(mapApiNotificationToNotification);

      if (pageNumber === 1) {
        setNotifications(mappedNotifications);
      } else {
        setNotifications(prev => [...prev, ...mappedNotifications]);
      }

      setHasNextPage(response.hasNextPage);
      setCurrentPage(pageNumber);
      setTotalCount(response.totalCount);

    } catch (err: any) {
      if (err?.response?.status === 401) {
        setError(null);
      } else {
        const errorMessage = err instanceof Error ? err.message : 'Failed to fetch notifications';
        setError(errorMessage);
      }
    } finally {
      setLoading(false);
      setRefreshing(false);
      setLoadingMore(false);
    }
  }, [filters?.includeDeleted, selectedSite?.id]);

  const fetchDeletedNotifications = useCallback(async (pageNumber: number = 1, isRefresh: boolean = false) => {
    try {
      if (pageNumber === 1) {
        setError(null);
        if (isRefresh) {
          setRefreshing(true);
        } else {
          setLoading(true);
        }
      } else {
        setLoadingMore(true);
      }

      const siteId = selectedSite?.id;

      if (!siteId) {
        const sites = await sitesService.getUserSites();
        const firstSiteId = sites?.siteList?.[0]?.id;
        if (!firstSiteId) {
          throw new Error('No site found for user');
        }

        const response: PaginatedListOfDeviceNotificationDto = await deviceNotificationsService.getDeletedDeviceNotifications({
          SiteId: firstSiteId,
          PageNumber: pageNumber,
          PageSize: PAGE_SIZE,
          IsToday: filters?.isToday,
          Category: filters?.category,
          Severity: filters?.severity,
        });

        const mappedNotifications = response.items.map(mapApiNotificationToNotification);
        if (pageNumber === 1) {
          setDeletedNotifications(mappedNotifications);
        } else {
          setDeletedNotifications(prev => [...prev, ...mappedNotifications]);
        }
        setHasNextPage(response.hasNextPage);
        setCurrentPage(pageNumber);
        setTotalCount(response.totalCount);
        return;
      }

      const response: PaginatedListOfDeviceNotificationDto = await deviceNotificationsService.getDeletedDeviceNotifications({
        SiteId: siteId,
        PageNumber: pageNumber,
        PageSize: PAGE_SIZE,
        IsToday: filters?.isToday,
        Category: filters?.category,
        Severity: filters?.severity,
      });

      const mappedNotifications = response.items.map(mapApiNotificationToNotification);

      if (pageNumber === 1) {
        setDeletedNotifications(mappedNotifications);
      } else {
        setDeletedNotifications(prev => [...prev, ...mappedNotifications]);
      }

      setHasNextPage(response.hasNextPage);
      setCurrentPage(pageNumber);
      setTotalCount(response.totalCount);

    } catch (err: any) {
      if (err?.response?.status === 401) {
        setError(null);
      } else {
        const errorMessage = err instanceof Error ? err.message : 'Failed to fetch deleted notifications';
        setError(errorMessage);
      }
    } finally {
      setLoading(false);
      setRefreshing(false);
      setLoadingMore(false);
    }
  }, [filters?.isToday, filters?.category, selectedSite?.id]);

  const refreshNotifications = useCallback(async () => {
    setCurrentPage(1);
    if (filters?.includeDeleted) {
      await fetchDeletedNotifications(1, true);
    } else {
      await fetchNotifications(1, true);
    }
  }, [fetchNotifications, fetchDeletedNotifications, filters?.includeDeleted]);

  const loadMoreNotifications = useCallback(() => {
    if (!loadingMore && hasNextPage) {
      if (filters?.includeDeleted) {
        fetchDeletedNotifications(currentPage + 1);
      } else {
        fetchNotifications(currentPage + 1);
      }
    }
  }, [fetchNotifications, fetchDeletedNotifications, loadingMore, hasNextPage, currentPage, filters?.includeDeleted]);

  const markAsRead = useCallback(async (id: string) => {
    const notification = notifications.find(n => n.id === id);
    if (notification?.read) {
      return;
    }

    setNotifications(prev => {
      const updated = prev.map(notification =>
        notification.id === id ? { ...notification, read: true } : notification
      );
      return updated;
    });

    try {
      await deviceNotificationsService.markDeviceNotificationAsRead({ id });

      if (filters?.includeDeleted) {
        await fetchDeletedNotifications(1, true);
      } else {
        await fetchNotifications(1, true);
      }
    } catch (error) {
      // Hata durumunda geri al
      setNotifications(prev =>
        prev.map(notification =>
          notification.id === id ? { ...notification, read: false } : notification
        )
      );
      throw error; // Error'ı tekrar throw et ki toast gösterilebilsin
    }
  }, [notifications, filters?.includeDeleted, fetchNotifications, fetchDeletedNotifications]);

  const markAllAsRead = useCallback(async () => {
    try {
      const siteId = selectedSite?.id;

      if (!siteId) {
        const sites = await sitesService.getUserSites();
        const firstSiteId = sites?.siteList?.[0]?.id;
        if (!firstSiteId) {
          throw new Error('No site found for user');
        }

        await deviceNotificationsService.markAllNotificationsAsRead(firstSiteId);

        if (filters?.includeDeleted) {
          await fetchDeletedNotifications(1, true);
        } else {
          await fetchNotifications(1, true);
        }
        return;
      }

      await deviceNotificationsService.markAllNotificationsAsRead(siteId);

      if (filters?.includeDeleted) {
        await fetchDeletedNotifications(1, true);
      } else {
        await fetchNotifications(1, true);
      }
    } catch (error) {
      throw error;
    }
  }, [selectedSite?.id, filters?.includeDeleted, fetchNotifications, fetchDeletedNotifications]);

  const deleteNotification = useCallback(async (id: string) => {
    try {
      await deviceNotificationsService.deleteDeviceNotification({ id });
      setNotifications(prev => prev.filter(notification => notification.id !== id));
    } catch (error) {
      throw error;
    }
  }, []);

  const restoreNotification = useCallback(async (id: string) => {
    try {
      await deviceNotificationsService.restoreDeviceNotification({ id });
      setDeletedNotifications(prev => {
        const notificationToRestore = prev.find(n => n.id === id);
        if (notificationToRestore) {
          setNotifications(current => [...current, { ...notificationToRestore, isDeleted: false }]);
        }
        return prev.filter(notification => notification.id !== id);
      });
    } catch (error) {
      throw error;
    }
  }, []);

  const permanentDeleteNotification = useCallback(async (id: string) => {
    try {
      await deviceNotificationsService.purgeDeviceNotification({ id });
      setDeletedNotifications(prev => prev.filter(notification => notification.id !== id));
    } catch (error) {
      throw error;
    }
  }, []);

  useEffect(() => {
    // selectedSite değiştiğinde kısa bir delay ile fetch yap
    // Bu, token yenileme işlemlerinin tamamlanması için zaman tanır
    const timer = setTimeout(() => {
      if (filters?.includeDeleted) {
        fetchDeletedNotifications(1);
      } else {
        fetchNotifications(1);
      }
    }, 100);

    return () => clearTimeout(timer);
  }, [fetchNotifications, fetchDeletedNotifications, filters?.includeDeleted, selectedSite?.id]);

  useEffect(() => {
    setCurrentPage(1);

    // selectedSite değiştiğinde de delay ile fetch yap
    const timer = setTimeout(() => {
      if (filters?.includeDeleted) {
        fetchDeletedNotifications(1, false);
      } else {
        fetchNotifications(1, false);
      }
    }, 100);

    return () => clearTimeout(timer);
  }, [filters?.isToday, filters?.isProcessed, filters?.category, fetchNotifications, fetchDeletedNotifications, filters?.includeDeleted, selectedSite?.id]);

  return {
    notifications,
    deletedNotifications,
    loading,
    refreshing,
    loadingMore,
    error,
    hasNextPage,
    totalCount,
    refreshNotifications,
    loadMoreNotifications,
    markAsRead,
    markAllAsRead,
    deleteNotification,
    restoreNotification,
    permanentDeleteNotification,
  };
}
