import Axios, { AxiosError, AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios';

export const axiosInstance = Axios.create({
  baseURL: (typeof process !== 'undefined' && process.env?.VITE_API_BASE_URL) || 'http://127.0.0.1:5041',
});

// Interceptor to add auth tokens or correlation IDs
axiosInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (config.headers && !config.headers['X-Correlation-ID']) {
    config.headers['X-Correlation-ID'] = crypto.randomUUID();
  }
  return config;
});

export const customInstance = <T>(config: AxiosRequestConfig, options?: AxiosRequestConfig): Promise<T> => {
  const source = Axios.CancelToken.source();
  const promise = axiosInstance({
    ...config,
    ...options,
    cancelToken: source.token,
  }).then(({ data }) => data);

  (promise as any).cancel = () => {
    source.cancel('Query was cancelled');
  };

  return promise;
};

export type ErrorType<Error> = AxiosError<Error>;
