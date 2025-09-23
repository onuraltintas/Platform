import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { ApiResponse, PagedResult, PageRequest } from '../models/api.models';

@Injectable({
  providedIn: 'root'
})
export class BaseApiService {
  protected readonly http = inject(HttpClient);
  protected readonly apiUrl = environment.apiGateway;

  protected get<T>(
    endpoint: string,
    params?: HttpParams | { [param: string]: string | string[] }
  ): Observable<T> {
    return this.http.get<ApiResponse<T>>(`${this.apiUrl}${endpoint}`, { params }).pipe(
      map(response => this.extractData(response))
    );
  }

  protected post<T>(
    endpoint: string,
    body: unknown,
    options?: { headers?: HttpHeaders }
  ): Observable<T> {
    return this.http.post<ApiResponse<T>>(`${this.apiUrl}${endpoint}`, body, options).pipe(
      map(response => this.extractData(response))
    );
  }

  protected put<T>(
    endpoint: string,
    body: unknown,
    options?: { headers?: HttpHeaders }
  ): Observable<T> {
    return this.http.put<ApiResponse<T>>(`${this.apiUrl}${endpoint}`, body, options).pipe(
      map(response => this.extractData(response))
    );
  }

  protected patch<T>(
    endpoint: string,
    body: unknown,
    options?: { headers?: HttpHeaders }
  ): Observable<T> {
    return this.http.patch<ApiResponse<T>>(`${this.apiUrl}${endpoint}`, body, options).pipe(
      map(response => this.extractData(response))
    );
  }

  protected delete<T>(
    endpoint: string,
    options?: { headers?: HttpHeaders }
  ): Observable<T> {
    return this.http.delete<ApiResponse<T>>(`${this.apiUrl}${endpoint}`, options).pipe(
      map(response => this.extractData(response))
    );
  }

  protected getPagedData<T>(
    endpoint: string,
    pageRequest?: PageRequest
  ): Observable<PagedResult<T>> {
    const params = this.buildPageParams(pageRequest);
    return this.get<PagedResult<T>>(endpoint, params);
  }

  protected postFormData<T>(
    endpoint: string,
    formData: FormData
  ): Observable<T> {
    return this.http.post<ApiResponse<T>>(`${this.apiUrl}${endpoint}`, formData).pipe(
      map(response => this.extractData(response))
    );
  }

  protected downloadFile(
    endpoint: string,
    filename?: string
  ): Observable<void> {
    return new Observable(observer => {
      this.http.get(`${this.apiUrl}${endpoint}`, {
        responseType: 'blob',
        observe: 'response'
      }).subscribe({
        next: (response) => {
          const blob = response.body!;
          const contentDisposition = response.headers.get('content-disposition');
          const fileName = filename || this.extractFilename(contentDisposition) || 'download';

          // Create download link
          const link = document.createElement('a');
          link.href = window.URL.createObjectURL(blob);
          link.download = fileName;
          link.click();

          // Clean up
          window.URL.revokeObjectURL(link.href);

          observer.next();
          observer.complete();
        },
        error: (error) => observer.error(error)
      });
    });
  }

  private extractData<T>(response: ApiResponse<T>): T {
    if (response.success && response.data !== undefined) {
      return response.data;
    }

    // If no data but success, return empty object/array based on context
    if (response.success) {
      return {} as T;
    }

    throw new Error(response.message || 'API request failed');
  }

  private buildPageParams(pageRequest?: PageRequest): HttpParams {
    let params = new HttpParams();

    if (!pageRequest) return params;

    if (pageRequest.pageNumber !== undefined) {
      params = params.set('pageNumber', pageRequest.pageNumber.toString());
    }
    if (pageRequest.pageSize !== undefined) {
      params = params.set('pageSize', pageRequest.pageSize.toString());
    }
    if (pageRequest.sortBy) {
      params = params.set('sortBy', pageRequest.sortBy);
    }
    if (pageRequest.sortDirection) {
      params = params.set('sortDirection', pageRequest.sortDirection);
    }
    if (pageRequest.search) {
      params = params.set('search', pageRequest.search);
    }
    if (pageRequest.filters) {
      Object.keys(pageRequest.filters).forEach(key => {
        const value = pageRequest.filters![key];
        if (value !== null && value !== undefined) {
          params = params.set(key, value.toString());
        }
      });
    }

    return params;
  }

  private extractFilename(contentDisposition: string | null): string | null {
    if (!contentDisposition) return null;

    const matches = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(contentDisposition);
    if (matches && matches[1]) {
      return matches[1].replace(/['"]/g, '');
    }

    return null;
  }

  protected handleError(error: unknown): Observable<never> {
    console.error('API Error:', error);
    throw error;
  }
}