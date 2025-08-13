import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UsersService } from '../services/users.service';
import { UserProfileDto, UserSettingsDto } from '../models/user.models';
import { ToastrService } from 'ngx-toastr';
import { UserAdminService, UserSummaryDto, RoleDto, CategoryDto } from '../services/user-admin.service';

@Component({
  standalone: true,
  selector: 'app-user-detail',
  templateUrl: './user-detail.component.html',
  styleUrls: ['./user-detail.component.scss'],
  imports: [CommonModule, RouterModule, FormsModule]
})
export class UserDetailComponent {
  private admin = inject(UserAdminService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private usersService = inject(UsersService);

  user = signal<UserSummaryDto | null>(null);
  roles = signal<RoleDto[]>([]);
  categories = signal<CategoryDto[]>([]);
  // user-category assignments (with metadata)
  userCategoryAssignments = signal<Array<{ categoryId: string; categoryName: string; assignedAt?: string; expiresAt?: string; isActive: boolean; notes?: string }>>([]);
  assignmentPage = signal(1);
  assignmentPageSize = signal(10);
  assignmentsHasNext = signal(false);
  assignmentQuery = signal('');
  assignmentActiveFilter = signal<'all' | 'true' | 'false'>('all');
  assignmentExpiringFilter = signal<'all' | 'expired' | '7' | '30'>('all');
  assignmentSortKey = signal<'categoryName' | 'assignedAt' | 'expiresAt' | 'isActive' | 'notes'>('assignedAt');
  assignmentSortDirection = signal<'asc' | 'desc'>('asc');
  loading = signal(true);
  error = signal<string | null>(null);
  selectedRoleId = signal<string | null>(null);
  selectedCategoryId = signal<string | null>(null);
  // category edit modal state
  editingCategoryId = signal<string | null>(null);
  categoryEdit = signal<{ expiresAt?: string; isActive: boolean; notes?: string }>({ isActive: true });
  assignCategoryExpiresAt = signal<string | null>(null);
  assignCategoryNotes = signal<string | null>(null);
  permissionQuery = signal<string>('');
  // profile/settings quick view
  showProfile = signal(false);
  showSettings = signal(false);
  userProfile = signal<UserProfileDto | null>(null);
  userSettings = signal<UserSettingsDto | null>(null);
  showProfileEdit = signal(false);
  showSettingsEdit = signal(false);
  profileEdit = signal<Partial<UserProfileDto>>({});
  settingsEdit = signal<Partial<UserSettingsDto>>({});

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    this.admin.getUser(id).subscribe({
      next: (res) => {
        const data = (res as any).data ?? res;
        this.user.set(data);
      },
      error: (err) => this.error.set(err?.message ?? 'Detay alınamadı'),
      complete: () => this.loading.set(false)
    });
    this.admin.listRoles({ page: 1, pageSize: 100 }).subscribe(r => {
      const data = (r as any).data ?? r;
      this.roles.set(data);
    });
    this.admin.listCategories({ page: 1, pageSize: 100 }).subscribe(r => {
      const data = (r as any).data ?? r;
      this.categories.set(data);
    });
    this.loadAssignments();
  }

  loadAssignments() {
    const uid = this.route.snapshot.paramMap.get('id')!;
    const page = this.assignmentPage();
    const pageSize = this.assignmentPageSize();
    const isActiveParam = this.assignmentActiveFilter() === 'all' ? undefined : this.assignmentActiveFilter() === 'true';
    this.admin.listUserCategories({ userId: uid, isActive: isActiveParam, page, pageSize }).subscribe(a => {
      const data = (a as any).data ?? (a as any);
      if (Array.isArray(data)) {
        this.userCategoryAssignments.set(data.map((x: any) => ({
          categoryId: x.categoryId,
          categoryName: x.categoryName,
          assignedAt: x.assignedAt,
          expiresAt: x.expiresAt,
          isActive: !!x.isActive,
          notes: x.notes
        })));
        this.assignmentsHasNext.set(data.length === pageSize);
      }
    });
  }

  nextAssignmentsPage() { if (!this.assignmentsHasNext()) return; this.assignmentPage.set(this.assignmentPage() + 1); this.loadAssignments(); }
  prevAssignmentsPage() { if (this.assignmentPage() <= 1) return; this.assignmentPage.set(this.assignmentPage() - 1); this.loadAssignments(); }

  onAssignmentFiltersChange() { this.assignmentPage.set(1); this.loadAssignments(); }

  filteredAssignments() {
    const q = (this.assignmentQuery() || '').toLowerCase();
    const exp = this.assignmentExpiringFilter();
    const now = new Date();
    const withinDays = (iso?: string) => {
      if (exp === 'all') return true;
      if (!iso) return exp !== 'expired';
      const date = new Date(iso);
      if (isNaN(date.getTime())) return false;
      if (exp === 'expired') return date.getTime() < now.getTime();
      const diffDays = (date.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);
      const limit = exp === '7' ? 7 : 30;
      return diffDays >= 0 && diffDays <= limit;
    };
    const filtered = this.userCategoryAssignments()
      .filter(a => (!q || a.categoryName.toLowerCase().includes(q) || (a.notes || '').toLowerCase().includes(q)))
      .filter(a => withinDays(a.expiresAt as string | undefined));

    const key = this.assignmentSortKey();
    const dir = this.assignmentSortDirection();
    const factor = dir === 'asc' ? 1 : -1;
    return [...filtered].sort((a, b) => {
      const va = (a as any)[key];
      const vb = (b as any)[key];
      if (va == null && vb == null) return 0;
      if (va == null) return -1 * factor;
      if (vb == null) return 1 * factor;
      if (key === 'assignedAt' || key === 'expiresAt') {
        const da = new Date(va).getTime();
        const db = new Date(vb).getTime();
        return (da - db) * factor;
      }
      if (key === 'isActive') {
        return ((va ? 1 : 0) - (vb ? 1 : 0)) * factor;
      }
      return String(va).localeCompare(String(vb)) * factor;
    });
  }

  sortAssignmentsBy(key: 'categoryName' | 'assignedAt' | 'expiresAt' | 'isActive' | 'notes') {
    if (this.assignmentSortKey() === key) {
      this.assignmentSortDirection.set(this.assignmentSortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.assignmentSortKey.set(key);
      this.assignmentSortDirection.set('asc');
    }
    // trigger change detection by re-setting the signal to a new array (no-op content-wise)
    this.userCategoryAssignments.set([...this.userCategoryAssignments()]);
  }

  exportAssignmentsCsv() {
    const rows = this.userCategoryAssignments().map(a => ([
      a.categoryName,
      a.assignedAt ?? '',
      a.expiresAt ?? '',
      a.isActive ? 'Aktif' : 'Pasif',
      a.notes ?? ''
    ]));
    const header = ['Kategori', 'Atanma', 'Bitiş', 'Durum', 'Not'];
    const csv = [header, ...rows].map(r => r.map(v => '"' + String(v).replace(/"/g, '""') + '"').join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'kategori-atamalari.csv'; a.click();
    URL.revokeObjectURL(url);
  }

  exportAssignmentsExcel() {
    // Basit bir CSV'yi .xls uzantısıyla indiriyoruz (hızlı çözüm)
    const rows = this.userCategoryAssignments().map(a => ([
      a.categoryName,
      a.assignedAt ?? '',
      a.expiresAt ?? '',
      a.isActive ? 'Aktif' : 'Pasif',
      a.notes ?? ''
    ]));
    const header = ['Kategori', 'Atanma', 'Bitiş', 'Durum', 'Not'];
    const csv = [header, ...rows].map(r => r.join('\t')).join('\n');
    const blob = new Blob([csv], { type: 'application/vnd.ms-excel' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'kategori-atamalari.xls'; a.click();
    URL.revokeObjectURL(url);
  }

  toggleActive() {
    const u = this.user();
    if (!u) return;
    const action = u.isActive ? this.admin.deactivateUser(u.id) : this.admin.activateUser(u.id);
    action.subscribe(() => {
      this.user.set({ ...u, isActive: !u.isActive });
    });
  }

  goEdit() {
    const id = this.user()?.id; if (id) this.router.navigate(['/users', id, 'edit']);
  }

  addRole() {
    const u = this.user();
    const roleId = this.selectedRoleId();
    if (!u || !roleId) return;
    this.admin.assignRoleToUser({ userId: u.id, roleId }).subscribe({
      next: () => {
        const role = this.roles().find(r => r.id === roleId);
        if (role && !u.roles.includes(role.name)) {
          this.user.set({ ...u, roles: [...u.roles, role.name] });
        }
        this.selectedRoleId.set(null);
      },
      error: (err) => this.error.set(err?.message ?? 'Rol atama hatası')
    });
  }

  removeRole(roleName: string) {
    const u = this.user();
    if (!u) return;
    const role = this.roles().find(r => r.name === roleName);
    if (!role) return;
    this.admin.removeRoleFromUser(u.id, role.id).subscribe({
      next: () => {
        this.user.set({ ...u, roles: u.roles.filter(r => r !== roleName) });
      },
      error: (err) => this.error.set(err?.message ?? 'Rol kaldırma hatası')
    });
  }

  addCategory() {
    const u = this.user();
    const categoryId = this.selectedCategoryId();
    if (!u || !categoryId) return;
    this.admin.assignCategoryToUser({ userId: u.id, categoryId, expiresAt: this.assignCategoryExpiresAt() || undefined, notes: this.assignCategoryNotes() || undefined }).subscribe({
      next: () => {
        const cat = this.categories().find(c => c.id === categoryId);
        if (cat && !u.categories.includes(cat.name)) {
          this.user.set({ ...u, categories: [...u.categories, cat.name] });
        }
        this.selectedCategoryId.set(null);
        this.assignCategoryExpiresAt.set(null);
        this.assignCategoryNotes.set(null);
      },
      error: (err) => this.error.set(err?.message ?? 'Kategori atama hatası')
    });
  }

  removeCategory(categoryName: string) {
    const u = this.user();
    if (!u) return;
    const cat = this.categories().find(c => c.name === categoryName);
    if (!cat) return;
    this.admin.removeCategoryFromUser(u.id, cat.id).subscribe({
      next: () => {
        this.user.set({ ...u, categories: u.categories.filter(c => c !== categoryName) });
      },
      error: (err) => this.error.set(err?.message ?? 'Kategori kaldırma hatası')
    });
  }

  openEditCategory(categoryName: string) {
    const u = this.user(); if (!u) return;
    const cat = this.categories().find(c => c.name === categoryName);
    if (!cat) return;
    this.editingCategoryId.set(cat.id);
    const assignment = this.userCategoryAssignments().find(a => a.categoryId === cat.id);
    this.categoryEdit.set({
      isActive: assignment?.isActive ?? true,
      notes: assignment?.notes,
      expiresAt: this.isoToLocalInput(assignment?.expiresAt)
    });
  }

  saveCategoryEdit() {
    const u = this.user();
    const catId = this.editingCategoryId();
    if (!u || !catId) return;
    const body = { ...this.categoryEdit(), expiresAt: this.localInputToIso(this.categoryEdit().expiresAt) } as any;
    this.admin.updateUserCategory(u.id, catId, body).subscribe({
      next: () => {
        const list = this.userCategoryAssignments().map(x => x.categoryId === catId ? {
          ...x,
          isActive: !!body.isActive,
          notes: body.notes,
          expiresAt: body.expiresAt
        } : x);
        this.userCategoryAssignments.set(list);
        this.editingCategoryId.set(null);
      },
      error: (e) => this.error.set(e?.message ?? 'Kategori güncellenemedi')
    });
  }

  cancelCategoryEdit() { this.editingCategoryId.set(null); }

  // Permissions filter
  filteredPermissions(): string[] {
    const list = this.user()?.permissions || [];
    const q = (this.permissionQuery() || '').toLowerCase();
    if (!q) return list;
    return list.filter(p => p.toLowerCase().includes(q));
  }

  // Quick navigation: load profile/settings into view
  loadProfile() {
    const id = this.user()?.id;
    if (!id) return;
    this.usersService.getProfile(id).subscribe({
      next: (p) => { this.userProfile.set(p); this.showProfile.set(true); },
      error: (e) => this.error.set(e?.message ?? 'Profil getirilemedi')
    });
  }

  loadSettings() {
    const id = this.user()?.id;
    if (!id) return;
    this.usersService.getSettings(id).subscribe({
      next: (s) => { this.userSettings.set(s); this.showSettings.set(true); },
      error: (e) => this.error.set(e?.message ?? 'Ayarlar getirilemedi')
    });
  }

  editProfile() {
    const p = this.userProfile(); if (!p) return;
    this.profileEdit.set({ ...p });
    this.showProfileEdit.set(true);
  }

  cancelProfileEdit() {
    this.showProfileEdit.set(false);
  }

  saveProfile() {
    const id = this.user()?.id; if (!id) return;
    const payload: any = { ...this.profileEdit() };
    this.usersService.updateProfile(id, payload).subscribe({
      next: (updated) => { this.userProfile.set(updated); this.showProfileEdit.set(false); },
      error: (e) => this.error.set(e?.message ?? 'Profil güncellenemedi')
    });
  }

  editSettings() {
    const s = this.userSettings(); if (!s) return;
    this.settingsEdit.set({ ...s });
    this.showSettingsEdit.set(true);
  }

  cancelSettingsEdit() {
    this.showSettingsEdit.set(false);
  }

  saveSettings() {
    const id = this.user()?.id; if (!id) return;
    const payload: any = { ...this.settingsEdit() };
    this.usersService.updateSettings(id, payload).subscribe({
      next: (updated) => { this.userSettings.set(updated); this.showSettingsEdit.set(false); },
      error: (e) => this.error.set(e?.message ?? 'Ayarlar güncellenemedi')
    });
  }

  // Template change handlers (avoid unsupported spread in Angular template expressions)
  onProfileChange<K extends keyof UserProfileDto>(key: K, value: UserProfileDto[K]) {
    const current = this.profileEdit();
    this.profileEdit.set({ ...(current as any), [key]: value } as Partial<UserProfileDto>);
  }

  onSettingsChange<K extends keyof UserSettingsDto>(key: K, value: UserSettingsDto[K]) {
    const current = this.settingsEdit();
    this.settingsEdit.set({ ...(current as any), [key]: value } as Partial<UserSettingsDto>);
  }

  onCategoryEditChange(key: 'expiresAt' | 'isActive' | 'notes', value: any) {
    const current = this.categoryEdit();
    const updated: { expiresAt?: string; isActive: boolean; notes?: string } = { ...current };
    (updated as any)[key] = value;
    this.categoryEdit.set(updated);
  }

  // Locale helpers
  formatDateTime(iso?: string | null): string {
    if (!iso) return '-';
    try {
      const d = new Date(iso);
      return new Intl.DateTimeFormat(navigator.language, { dateStyle: 'medium', timeStyle: 'short' }).format(d);
    } catch { return iso as string; }
  }

  isoToLocalInput(iso?: string | null): string | undefined {
    if (!iso) return undefined;
    const d = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, '0');
    const y = d.getFullYear();
    const m = pad(d.getMonth() + 1);
    const day = pad(d.getDate());
    const h = pad(d.getHours());
    const min = pad(d.getMinutes());
    return `${y}-${m}-${day}T${h}:${min}`;
  }

  localInputToIso(local?: string | null): string | undefined {
    if (!local) return undefined;
    const d = new Date(local);
    return isNaN(d.getTime()) ? undefined : d.toISOString();
  }
}

