export interface ApiResponse<T = any> {
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
  details?: any;
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

export interface PageRequest {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  search?: string;
  filters?: Record<string, any>;
}

export interface BulkOperationRequest {
  ids: string[];
  operation: 'delete' | 'activate' | 'deactivate' | 'export';
  params?: Record<string, any>;
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
  filters?: Record<string, any>;
}

export interface AuditLog {
  id: string;
  userId: string;
  userName: string;
  action: string;
  entityType: string;
  entityId?: string;
  oldValues?: Record<string, any>;
  newValues?: Record<string, any>;
  ipAddress: string;
  userAgent: string;
  timestamp: Date;
  duration?: number;
  success: boolean;
  errorMessage?: string;
}