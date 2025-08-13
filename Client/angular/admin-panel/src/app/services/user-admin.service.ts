import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface PagedUsersResponse {
  users: Array<UserSummaryDto>;
  totalCount: number;
  currentPage: number;
  pageSize: number;
}

export interface UserSummaryDto {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phoneNumber: string | null;
  isEmailConfirmed: boolean;
  isActive: boolean;
  lastLoginAt?: string | null;
  roles: string[];
  categories: string[];
  permissions: string[];
}

export interface CreateUserBody {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  phoneNumber: string | null;
  roleIds?: string[];
  categoryIds?: string[];
  isActive?: boolean;
  isEmailConfirmed?: boolean;
}

export interface UpdateUserBody {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
  isActive: boolean;
  isEmailConfirmed: boolean;
  roleIds?: string[];
  categoryIds?: string[];
}

export interface RoleDto { id: string; name: string; description?: string; isActive: boolean; }
export interface PermissionDto { id: string; name: string; description?: string; group?: string; isActive: boolean; }
export interface CreatePermissionBody { name: string; description?: string; group?: string; isActive?: boolean; }
export interface UpdatePermissionBody { name: string; description?: string; group?: string; isActive: boolean; }
export interface CategoryDto { id: string; name: string; description?: string; type?: string; isActive: boolean; }
export interface CreateCategoryBody { name: string; description?: string; type?: string; isActive?: boolean; }
export interface UpdateCategoryBody { name: string; description?: string; type?: string; isActive: boolean; }

export interface CreateRoleBody { name: string; description?: string; isActive?: boolean; permissionIds?: string[]; }
export interface UpdateRoleBody { name: string; description?: string; isActive: boolean; }

@Injectable({ providedIn: 'root' })
export class UserAdminService {
  private base = `${environment.apiUrl}/v1/admin`;
  private users = `${this.base}/users`;
  private roles = `${this.base}/roles`;
  private permissions = `${this.base}/permissions`;
  private categories = `${this.base}/categories`;
  private userRoles = `${this.base}/user-roles`;
  private userCategories = `${this.base}/user-categories`;
  private rolePermissions = `${this.base}/roles`;

  constructor(private http: HttpClient) {}

  listUsers(params: { page?: number; pageSize?: number; search?: string; roleId?: string; categoryId?: string; isActive?: boolean; isEmailConfirmed?: boolean; }): Observable<PagedUsersResponse> {
    const query: Record<string, any> = {};
    if (params.page !== undefined) query['page'] = params.page;
    if (params.pageSize !== undefined) query['pageSize'] = params.pageSize;
    if (params.search) query['search'] = params.search;
    if (params.roleId) query['roleId'] = params.roleId;
    if (params.categoryId) query['categoryId'] = params.categoryId;
    if (params.isActive !== undefined) query['isActive'] = params.isActive;
    if (params.isEmailConfirmed !== undefined) query['isEmailConfirmed'] = params.isEmailConfirmed;
    return this.http.get<PagedUsersResponse>(this.users, { params: query });
  }

  getUser(userId: string): Observable<UserSummaryDto> {
    return this.http.get<any>(`${this.users}/${userId}`).pipe(map(res => res?.data ?? res));
  }

  createUser(body: CreateUserBody): Observable<UserSummaryDto> {
    return this.http.post<any>(this.users, body).pipe(map(res => res?.data ?? res));
  }

  updateUser(userId: string, body: UpdateUserBody): Observable<UserSummaryDto> {
    return this.http.put<any>(`${this.users}/${userId}`, body).pipe(map(res => res?.data ?? res));
  }

  deleteUser(userId: string): Observable<any> {
    return this.http.delete(`${this.users}/${userId}`);
  }

  deactivateUser(userId: string): Observable<any> {
    return this.http.patch(`${this.users}/${userId}/deactivate`, {});
  }

  activateUser(userId: string): Observable<any> {
    return this.http.patch(`${this.users}/${userId}/activate`, {});
  }

  // Roles CRUD
  getRole(roleId: string): Observable<RoleDto> {
    return this.http.get<any>(`${this.roles}/${roleId}`).pipe(map(res => res?.data ?? res));
  }

  createRole(body: CreateRoleBody): Observable<RoleDto> {
    return this.http.post<any>(this.roles, body).pipe(map(res => res?.data ?? res));
  }

  updateRole(roleId: string, body: UpdateRoleBody): Observable<RoleDto> {
    return this.http.put<any>(`${this.roles}/${roleId}`, body).pipe(map(res => res?.data ?? res));
  }

  deleteRole(roleId: string): Observable<any> {
    return this.http.delete(`${this.roles}/${roleId}`);
  }

  // Lookups
  listRoles(params?: { search?: string; isActive?: boolean; page?: number; pageSize?: number; }): Observable<{ data: RoleDto[] } | RoleDto[]> {
    const query: Record<string, any> = {};
    if (params?.search) query['search'] = params.search;
    if (params?.isActive !== undefined) query['isActive'] = params.isActive;
    if (params?.page !== undefined) query['page'] = params.page;
    if (params?.pageSize !== undefined) query['pageSize'] = params.pageSize;
    return this.http.get<any>(this.roles, { params: query });
  }

  listPermissions(params?: { search?: string; group?: string; isActive?: boolean; page?: number; pageSize?: number; }): Observable<{ data: PermissionDto[] } | PermissionDto[]> {
    const query: Record<string, any> = {};
    if (params?.search) query['search'] = params.search;
    if (params?.group) query['group'] = params.group;
    if (params?.isActive !== undefined) query['isActive'] = params.isActive;
    if (params?.page !== undefined) query['page'] = params.page;
    if (params?.pageSize !== undefined) query['pageSize'] = params.pageSize;
    return this.http.get<any>(this.permissions, { params: query });
  }

  // Permissions CRUD
  getPermission(permissionId: string): Observable<PermissionDto> {
    return this.http.get<any>(`${this.permissions}/${permissionId}`).pipe(map(res => res?.data ?? res));
    }

  createPermission(body: CreatePermissionBody): Observable<PermissionDto> {
    return this.http.post<PermissionDto>(this.permissions, body);
  }

  updatePermission(permissionId: string, body: UpdatePermissionBody): Observable<PermissionDto> {
    return this.http.put<PermissionDto>(`${this.permissions}/${permissionId}`, body);
  }

  deletePermission(permissionId: string): Observable<any> {
    return this.http.delete(`${this.permissions}/${permissionId}`);
  }

  // Role-permission
  getRolePermissions(roleId: string): Observable<{ data: string[] } | string[]> {
    return this.http.get<any>(`${this.rolePermissions}/${roleId}/permissions`);
  }

  assignPermissionToRole(roleId: string, permissionId: string): Observable<any> {
    return this.http.post(`${this.rolePermissions}/${roleId}/permissions/${permissionId}`, {});
  }

  removePermissionFromRole(roleId: string, permissionId: string): Observable<any> {
    return this.http.delete(`${this.rolePermissions}/${roleId}/permissions/${permissionId}`);
  }

  updateRolePermissions(roleId: string, permissionIds: string[]): Observable<any> {
    return this.http.put(`${this.rolePermissions}/${roleId}/permissions`, permissionIds);
  }

  listCategories(params?: { search?: string; type?: string; isActive?: boolean; page?: number; pageSize?: number; }): Observable<{ data: CategoryDto[] } | CategoryDto[]> {
    const query: Record<string, any> = {};
    if (params?.search) query['search'] = params.search;
    if (params?.type) query['type'] = params.type;
    if (params?.isActive !== undefined) query['isActive'] = params.isActive;
    if (params?.page !== undefined) query['page'] = params.page;
    if (params?.pageSize !== undefined) query['pageSize'] = params.pageSize;
    return this.http.get<any>(this.categories, { params: query });
  }

  getCategory(categoryId: string): Observable<CategoryDto> {
    return this.http.get<any>(`${this.categories}/${categoryId}`).pipe(map(res => res?.data ?? res));
  }

  createCategory(body: CreateCategoryBody): Observable<CategoryDto> {
    return this.http.post<CategoryDto>(this.categories, body);
  }

  updateCategory(categoryId: string, body: UpdateCategoryBody): Observable<CategoryDto> {
    return this.http.put<CategoryDto>(`${this.categories}/${categoryId}`, body);
  }

  deleteCategory(categoryId: string): Observable<any> {
    return this.http.delete(`${this.categories}/${categoryId}`);
  }

  // Assignments
  assignRoleToUser(body: { userId: string; roleId: string; expiresAt?: string; notes?: string; }): Observable<any> {
    return this.http.post(this.userRoles, body);
  }

  removeRoleFromUser(userId: string, roleId: string): Observable<any> {
    return this.http.delete(`${this.userRoles}/${userId}/${roleId}`);
  }

  updateUserRole(userId: string, roleId: string, body: { expiresAt?: string; isActive: boolean; notes?: string; }): Observable<any> {
    return this.http.put(`${this.userRoles}/${userId}/${roleId}`, body);
  }

  assignCategoryToUser(body: { userId: string; categoryId: string; expiresAt?: string; notes?: string; }): Observable<any> {
    return this.http.post(this.userCategories, body);
  }

  removeCategoryFromUser(userId: string, categoryId: string): Observable<any> {
    return this.http.delete(`${this.userCategories}/${userId}/${categoryId}`);
  }

  updateUserCategory(userId: string, categoryId: string, body: { expiresAt?: string; isActive: boolean; notes?: string; }): Observable<any> {
    return this.http.put(`${this.userCategories}/${userId}/${categoryId}`, body);
  }

  listUserCategories(params?: { userId?: string; categoryId?: string; isActive?: boolean; page?: number; pageSize?: number; }): Observable<any> {
    const query: Record<string, any> = {};
    if (params?.userId) query['userId'] = params.userId;
    if (params?.categoryId) query['categoryId'] = params.categoryId;
    if (params?.isActive !== undefined) query['isActive'] = params.isActive;
    if (params?.page !== undefined) query['page'] = params.page;
    if (params?.pageSize !== undefined) query['pageSize'] = params.pageSize;
    return this.http.get(this.userCategories, { params: query });
  }
}

