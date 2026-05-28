export interface UiState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  empty: boolean;
}

export interface ApiUiError {
  message: string;
  status: number;
  traceId?: string;
}
