export type AppError = {
  code: string;
  status?: number;
  message: string;
  details?: any;
  retriable?: boolean;
};

type BackendErrorShape = {
  message?: string;
  code?: string;
  errors?: Record<string, string[]> | string[];
};

function extractBackendError(data: unknown): BackendErrorShape | null {
  if (!data || typeof data !== 'object') return null;
  if ('error' in data && typeof (data as any).error === 'object') return (data as any).error as BackendErrorShape;
  return data as BackendErrorShape;
}

export function mapToAppError(err: unknown): AppError {
  const error = err as any;
  const isAxios = !!error?.isAxiosError || !!error?.config || !!error?.response;

  if (isAxios) {
    const status: number | undefined = error?.response?.status;
    const data = error?.response?.data;
    const backend = extractBackendError(data);

    if (error?.code === 'ECONNABORTED' || error?.message?.includes('Network Error')) {
      return { code: 'NETWORK', status, message: 'Bağlantı sorunu oluştu. Lütfen bağlantınızı kontrol edin.', retriable: true };
    }

    if (status === 401) {
      return { code: 'UNAUTHORIZED', status, message: 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.' };
    }

    if (status === 403) {
      return { code: 'FORBIDDEN', status, message: 'Bu işlem için yetkiniz yok.' };
    }

    if (status === 404) {
      return { code: 'NOT_FOUND', status, message: 'İstenen kayıt bulunamadı.' };
    }

    if (status === 422 || status === 400) {
      const msg =
        backend?.message ||
        (Array.isArray(backend?.errors) ? backend?.errors.join(', ') : undefined) ||
        'Form alanlarını kontrol edin.';
      return { code: 'VALIDATION', status, message: msg || 'Geçersiz istek.' };
    }

    if (status && status >= 500) {
      return { code: 'SERVER', status, message: 'Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.', retriable: true };
    }

    const fallback =
      backend?.message ||
      error?.response?.statusText ||
      error?.message ||
      'Bilinmeyen bir hata oluştu.';
    return { code: 'UNKNOWN', status, message: fallback };
  }

  if ((error as Error)?.message) {
    return { code: 'UNKNOWN', message: (error as Error).message };
  }

  return { code: 'UNKNOWN', message: 'Bilinmeyen bir hata oluştu.' };
}
