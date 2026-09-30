export interface ValidationErrorDetail {
  error: string;
  detail: string;
}

/** The backend's ApiResponseWithData<T> envelope. HTTP errors remain HttpErrorResponse. */
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  errors: ValidationErrorDetail[];
  data: T | null;
}

/** Matches WebApi/Common/PaginatedResponse<T>, including totalCount. */
export interface PagedResponse<T> extends ApiResponse<T[]> {
  currentPage: number;
  totalPages: number;
  totalCount: number;
}
