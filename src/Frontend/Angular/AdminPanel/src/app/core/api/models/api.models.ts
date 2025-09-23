export interface ApiResponse<T = unknown> {
  data?: T;
  success: boolean;
  message?: string;
  errors?: ApiError[];
  timestamp: Date;
  path?: string;
  traceId?: string;
}

export interface ApiError {
  code: string;
  message: string;
  field?: string;
  details?: Record<string, unknown>;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface PagedResponse<T> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface PageRequest {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  search?: string;
  filters?: Record<string, unknown>;
}

export interface BulkOperationRequest {
  ids: string[];
  operation: 'delete' | 'activate' | 'deactivate' | 'export';
  params?: Record<string, unknown>;
}

export interface BulkOperationResult {
  succeeded: string[];
  failed: BulkOperationFailure[];
  totalProcessed: number;
}

export interface BulkOperationFailure {
  id: string;
  reason: string;
}

export interface FileUploadResponse {
  fileId: string;
  fileName: string;
  fileUrl: string;
  fileSize: number;
  contentType: string;
}

export interface ExportRequest {
  format: 'excel' | 'csv' | 'pdf';
  columns?: string[];
  filters?: Record<string, unknown>;
}

export interface AuditLog {
  id: string;
  userId: string;
  userName: string;
  action: string;
  entityType: string;
  entityId?: string;
  oldValues?: Record<string, unknown>;
  newValues?: Record<string, unknown>;
  ipAddress: string;
  userAgent: string;
  timestamp: Date;
  duration?: number;
  success: boolean;
  errorMessage?: string;
}