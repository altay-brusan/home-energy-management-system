import axios, {
  AxiosError,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from "axios";
import { mapToAppError } from "./error";

const baseURL: string = "https://example.com/api";

export const apiClient = axios.create({
  baseURL,
  timeout: 15000,
  headers: { "Content-Type": "application/json" },
});

apiClient.interceptors.response.use(
  (res: AxiosResponse): AxiosResponse => res,
  (error: AxiosError): Promise<never> => {
    const appError = mapToAppError(error);
    return Promise.reject(appError);
  }
);
apiClient.interceptors.response.use(
  (res: AxiosResponse) => res,
  (error: AxiosError) => {
    const appError = mapToAppError(error);
    return Promise.reject(appError);
  }
);
