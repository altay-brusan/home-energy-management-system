import axios, { AxiosInstance } from 'axios';

export const apiClient: AxiosInstance = axios.create({
  baseURL: 'https://example.com/api',
  timeout: 15000,
});

apiClient.interceptors.request.use((config) => {
  return config;
});

