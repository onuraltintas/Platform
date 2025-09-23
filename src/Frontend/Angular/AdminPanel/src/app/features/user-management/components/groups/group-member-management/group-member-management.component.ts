import { Component, OnInit, Input, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import {
  Users,
  UserPlus,
  UserMinus,
  Search,
  Filter,
  MoreVertical,
  Edit2,
  Trash2,
  Crown,
  Shield,
  User,
  Mail,
  Calendar,
  ArrowUpDown,
  Download,
  Upload,
  CheckCircle,
  AlertCircle,
  Clock,
  X,
  Plus,
  Settings,
  Eye,
  Copy,
  RefreshCw,
  ChevronDown,
  Grid,
  List,
  LucideAngularModule
} from 'lucide-angular';

import { GroupService } from '../../../services/group.service';
import { UserService } from '../../../services/user.service';
import { ConfirmationService } from '../../../../../shared/services/confirmation.service';
import { ErrorHandlerService } from '../../../../../core/services/error-handler.service';

import {
  GroupDto,
  GroupMemberDto,
  GroupMemberRequest,
  GroupMemberRole
} from '../../../models/group.models';
import { UserDto } from '../../../models/user.models';

interface MemberFilter {
  search: string;
  role: GroupMemberRole | 'all';
  status: 'all' | 'active' | 'inactive';
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

interface BulkMemberOperation {
  type: 'remove' | 'changeRole' | 'activate' | 'deactivate' | 'export';
  label: string;
  icon: any;
  confirmMessage: string;
  requiresInput?: boolean;
  destructive?: boolean;
}

interface MemberStatistics {
  totalMembers: number;
  membersByRole: Record<GroupMemberRole, number>;
  activeMembers: number;
  inactiveMembers: number;
  recentJoins: number;
}

interface AddMemberCandidate {
  user: UserDto;
  role: GroupMemberRole;
  selected: boolean;
}

@Component({
  selector: 'app-group-member-management',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    LucideAngularModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="member-management-container">
      <!-- Header Section -->
      <div class="page-header">
        <div class="header-content">
          <div class="title-section">
            <div class="breadcrumb-nav">
              <a [routerLink]="['/user-management/groups']" class="breadcrumb-link">Gruplar</a>
              <span class="breadcrumb-separator">/</span>
              <span class="breadcrumb-current">{{ group()?.name || 'Grup' }}</span>
              <span class="breadcrumb-separator">/</span>
              <span class="breadcrumb-current">Üye Yönetimi</span>
            </div>

            <div class="title-with-icon">
              <lucide-angular [img]="UsersIcon" size="24" class="title-icon"></lucide-angular>
              <h1 class="page-title">{{ group()?.name }} - Üye Yönetimi</h1>
            </div>
            <p class="page-subtitle">Grup üyelerini yönetin, roller atayın ve izinleri kontrol edin</p>
          </div>

          <div class="header-actions">
            <button
              type="button"
              class="btn btn-outline-secondary"
              (click)="refreshData()"
              [disabled]="loading()">
              <lucide-angular [img]="RefreshCwIcon" size="16"></lucide-angular>
              Yenile
            </button>

            <div class="dropdown">
              <button
                type="button"
                class="btn btn-outline-secondary dropdown-toggle"
                data-bs-toggle="dropdown">
                <lucide-angular [img]="DownloadIcon" size="16"></lucide-angular>
                Dışa Aktar
              </button>
              <ul class="dropdown-menu">
                <li><a class="dropdown-item" (click)="exportMembers('excel')">Excel</a></li>
                <li><a class="dropdown-item" (click)="exportMembers('csv')">CSV</a></li>
                <li><a class="dropdown-item" (click)="exportMembers('pdf')">PDF</a></li>
              </ul>
            </div>

            <button
              type="button"
              class="btn btn-outline-primary"
              (click)="openBulkImportModal()"
              [disabled]="loading()">
              <lucide-angular [img]="UploadIcon" size="16"></lucide-angular>
              Toplu İçe Aktar
            </button>

            <button
              type="button"
              class="btn btn-primary"
              (click)="openAddMemberModal()"
              [disabled]="loading()">
              <lucide-angular [img]="UserPlusIcon" size="16"></lucide-angular>
              Üye Ekle
            </button>
          </div>
        </div>

        <!-- Group Info Card -->
        <div class="group-info-card" *ngIf="group()">
          <div class="group-basic-info">
            <div class="group-name-section">
              <h4 class="group-name">{{ group()!.name }}</h4>
              <span class="system-badge" *ngIf="group()!.isSystemGroup">Sistem Grubu</span>
            </div>
            <p class="group-description">{{ group()!.description || 'Açıklama bulunmamaktadır.' }}</p>
          </div>

          <div class="group-stats-overview">
            <div class="stat-item">
              <lucide-angular [img]="UsersIcon" size="16" class="stat-icon"></lucide-angular>
              <span class="stat-value">{{ statistics()?.totalMembers || 0 }}</span>
              <span class="stat-label">Toplam Üye</span>
            </div>
            <div class="stat-item">
              <lucide-angular [img]="CheckCircleIcon" size="16" class="stat-icon active"></lucide-angular>
              <span class="stat-value">{{ statistics()?.activeMembers || 0 }}</span>
              <span class="stat-label">Aktif</span>
            </div>
            <div class="stat-item">
              <lucide-angular [img]="ClockIcon" size="16" class="stat-icon pending"></lucide-angular>
              <span class="stat-value">{{ statistics()?.inactiveMembers || 0 }}</span>
              <span class="stat-label">Pasif</span>
            </div>
          </div>
        </div>

        <!-- Role Distribution Cards -->
        <div class="role-distribution" *ngIf="statistics()">
          <div class="role-card" *ngFor="let role of roleCards()">
            <div class="role-card-header">
              <lucide-angular [img]="role.icon" size="20" [class]="'role-icon ' + role.class"></lucide-angular>
              <span class="role-name">{{ role.name }}</span>
            </div>
            <div class="role-count">{{ role.count }}</div>
            <div class="role-percentage">{{ role.percentage }}%</div>
          </div>
        </div>
      </div>

      <!-- Filters and Controls -->
      <div class="controls-section">
        <div class="filters-row">
          <!-- Search -->
          <div class="search-input-group">
            <lucide-angular [img]="SearchIcon" size="16" class="search-icon"></lucide-angular>
            <input
              type="text"
              class="form-control search-input"
              placeholder="Üye ara..."
              [(ngModel)]="filters().search"
              (input)="onSearchChange($event)"
              [disabled]="loading()">
            <button
              *ngIf="filters().search"
              type="button"
              class="btn-clear-search"
              (click)="clearSearch()">
              <lucide-angular [img]="XIcon" size="14"></lucide-angular>
            </button>
          </div>

          <!-- Role Filter -->
          <div class="filter-group">
            <select
              class="form-select"
              [(ngModel)]="filters().role"
              (change)="applyFilters()">
              <option value="all">Tüm Roller</option>
              <option value="Owner">Sahip</option>
              <option value="Admin">Yönetici</option>
              <option value="Member">Üye</option>
            </select>
          </div>

          <!-- Status Filter -->
          <div class="filter-group">
            <select
              class="form-select"
              [(ngModel)]="filters().status"
              (change)="applyFilters()">
              <option value="all">Tüm Durumlar</option>
              <option value="active">Aktif</option>
              <option value="inactive">Pasif</option>
            </select>
          </div>

          <!-- Sort Controls -->
          <div class="sort-controls">
            <select
              class="form-select sort-select"
              [(ngModel)]="filters().sortBy"
              (change)="onSortChange()">
              <option value="userName">Ada göre</option>
              <option value="role">Role göre</option>
              <option value="joinedAt">Katılma tarihine göre</option>
              <option value="email">E-postaya göre</option>
            </select>
            <button
              type="button"
              class="btn btn-outline-secondary sort-direction"
              (click)="toggleSortDirection()"
              [title]="filters().sortDirection === 'asc' ? 'Artan sıra' : 'Azalan sıra'">
              <lucide-angular [img]="ArrowUpDownIcon" size="14"></lucide-angular>
            </button>
          </div>

          <!-- View Mode Toggle -->
          <div class="view-toggle">
            <div class="btn-group" role="group">
              <button
                type="button"
                class="btn btn-outline-secondary"
                [class.active]="viewMode() === 'table'"
                (click)="setViewMode('table')">
                <lucide-angular [img]="ListIcon" size="16"></lucide-angular>
              </button>
              <button
                type="button"
                class="btn btn-outline-secondary"
                [class.active]="viewMode() === 'grid'"
                (click)="setViewMode('grid')">
                <lucide-angular [img]="GridIcon" size="16"></lucide-angular>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Bulk Operations Bar -->
      <div class="bulk-operations-bar" *ngIf="selectedMembers().length > 0">
        <div class="bulk-info">
          <span class="selected-count">{{ selectedMembers().length }} üye seçildi</span>
          <button type="button" class="btn-link" (click)="clearSelection()">
            Seçimi temizle
          </button>
        </div>

        <div class="bulk-actions">
          <button
            *ngFor="let operation of bulkOperations"
            type="button"
            class="btn btn-outline-secondary btn-sm"
            [class.btn-outline-danger]="operation.destructive"
            (click)="executeBulkOperation(operation)"
            [disabled]="loading()">
            <lucide-angular [img]="operation.icon" size="14"></lucide-angular>
            {{ operation.label }}
          </button>
        </div>
      </div>

      <!-- Members List - Table View -->
      <div class="members-content" *ngIf="viewMode() === 'table'">
        <div class="table-container">
          <table class="table table-hover members-table">
            <thead>
              <tr>
                <th class="select-column">
                  <input
                    type="checkbox"
                    class="form-check-input"
                    [checked]="isAllSelected()"
                    [indeterminate]="isPartiallySelected()"
                    (change)="toggleSelectAll()">
                </th>
                <th>Üye</th>
                <th>E-posta</th>
                <th>Rol</th>
                <th>Durum</th>
                <th>Katılma Tarihi</th>
                <th class="actions-column">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let member of filteredMembers(); trackBy: trackByMemberId"
                  [class.selected]="isMemberSelected(member)">
                <td>
                  <input
                    type="checkbox"
                    class="form-check-input"
                    [checked]="isMemberSelected(member)"
                    (change)="toggleMemberSelection(member)">
                </td>
                <td>
                  <div class="member-info">
                    <div class="member-avatar" *ngIf="member.profilePictureUrl; else defaultAvatar">
                      <img [src]="member.profilePictureUrl" [alt]="member.userName" class="avatar-img">
                    </div>
                    <ng-template #defaultAvatar>
                      <div class="default-avatar">
                        {{ getInitials(member.firstName, member.lastName) }}
                      </div>
                    </ng-template>
                    <div class="member-details">
                      <span class="member-name">{{ member.firstName }} {{ member.lastName }}</span>
                      <span class="member-username">&#64;{{ member.userName }}</span>
                    </div>
                  </div>
                </td>
                <td>
                  <span class="member-email">{{ member.email }}</span>
                </td>
                <td>
                  <span class="role-badge" [class]="getRoleClass(member.role)">
                    <lucide-angular [img]="getRoleIcon(member.role)" size="14" class="role-icon"></lucide-angular>
                    {{ getRoleDisplayName(member.role) }}
                  </span>
                </td>
                <td>
                  <span
                    class="status-badge"
                    [class.status-active]="member.isActive"
                    [class.status-inactive]="!member.isActive">
                    <lucide-angular
                      [img]="member.isActive ? CheckCircleIcon : AlertCircleIcon"
                      size="14"
                      class="status-icon">
                    </lucide-angular>
                    {{ member.isActive ? 'Aktif' : 'Pasif' }}
                  </span>
                </td>
                <td>
                  <span class="join-date">{{ formatDate(member.joinedAt) }}</span>
                </td>
                <td>
                  <div class="action-buttons">
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-primary"
                      (click)="viewMemberProfile(member)"
                      title="Profili Görüntüle">
                      <lucide-angular [img]="EyeIcon" size="14"></lucide-angular>
                    </button>
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-warning"
                      (click)="changeMemberRole(member)"
                      title="Rol Değiştir"
                      [disabled]="!canChangeMemberRole(member)">
                      <lucide-angular [img]="SettingsIcon" size="14"></lucide-angular>
                    </button>

                    <div class="dropdown">
                      <button
                        type="button"
                        class="btn btn-sm btn-outline-secondary dropdown-toggle"
                        data-bs-toggle="dropdown">
                        <lucide-angular [img]="MoreVerticalIcon" size="14"></lucide-angular>
                      </button>
                      <ul class="dropdown-menu">
                        <li>
                          <a class="dropdown-item" (click)="sendMessageToMember(member)">
                            <lucide-angular [img]="MailIcon" size="14"></lucide-angular>
                            Mesaj Gönder
                          </a>
                        </li>
                        <li>
                          <a class="dropdown-item" (click)="viewMemberActivity(member)">
                            <lucide-angular [img]="CalendarIcon" size="14"></lucide-angular>
                            Aktiviteyi Görüntüle
                          </a>
                        </li>
                        <li><hr class="dropdown-divider"></li>
                        <li>
                          <a
                            class="dropdown-item"
                            [class.text-success]="!member.isActive"
                            [class.text-warning]="member.isActive"
                            (click)="toggleMemberStatus(member)">
                            <lucide-angular
                              [img]="member.isActive ? AlertCircleIcon : CheckCircleIcon"
                              size="14">
                            </lucide-angular>
                            {{ member.isActive ? 'Pasifleştir' : 'Aktifleştir' }}
                          </a>
                        </li>
                        <li>
                          <a
                            class="dropdown-item text-danger"
                            (click)="removeMemberFromGroup(member)"
                            [class.disabled]="!canRemoveMember(member)">
                            <lucide-angular [img]="UserMinusIcon" size="14"></lucide-angular>
                            Gruptan Çıkar
                          </a>
                        </li>
                      </ul>
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Members List - Grid View -->
      <div class="members-grid" *ngIf="viewMode() === 'grid'">
        <div class="member-card-container">
          <div
            *ngFor="let member of filteredMembers(); trackBy: trackByMemberId"
            class="member-card"
            [class.selected]="isMemberSelected(member)">

            <div class="card-header">
              <div class="card-selection">
                <input
                  type="checkbox"
                  class="form-check-input"
                  [checked]="isMemberSelected(member)"
                  (change)="toggleMemberSelection(member)">
              </div>

              <div class="member-avatar-section">
                <div class="member-avatar" *ngIf="member.profilePictureUrl; else gridDefaultAvatar">
                  <img [src]="member.profilePictureUrl" [alt]="member.userName" class="avatar-img">
                </div>
                <ng-template #gridDefaultAvatar>
                  <div class="default-avatar">
                    {{ getInitials(member.firstName, member.lastName) }}
                  </div>
                </ng-template>
                <span
                  class="status-indicator"
                  [class.status-active]="member.isActive"
                  [class.status-inactive]="!member.isActive">
                </span>
              </div>

              <div class="card-actions">
                <div class="dropdown">
                  <button
                    type="button"
                    class="btn btn-sm btn-ghost dropdown-toggle"
                    data-bs-toggle="dropdown">
                    <lucide-angular [img]="MoreVerticalIcon" size="16"></lucide-angular>
                  </button>
                  <ul class="dropdown-menu">
                    <li><a class="dropdown-item" (click)="changeMemberRole(member)">Rol Değiştir</a></li>
                    <li><a class="dropdown-item" (click)="toggleMemberStatus(member)">
                      {{ member.isActive ? 'Pasifleştir' : 'Aktifleştir' }}
                    </a></li>
                    <li><a class="dropdown-item text-danger" (click)="removeMemberFromGroup(member)">Gruptan Çıkar</a></li>
                  </ul>
                </div>
              </div>
            </div>

            <div class="card-body">
              <div class="member-name">{{ member.firstName }} {{ member.lastName }}</div>
              <div class="member-username">&#64;{{ member.userName }}</div>
              <div class="member-email">{{ member.email }}</div>

              <div class="member-role-section">
                <span class="role-badge" [class]="getRoleClass(member.role)">
                  <lucide-angular [img]="getRoleIcon(member.role)" size="14" class="role-icon"></lucide-angular>
                  {{ getRoleDisplayName(member.role) }}
                </span>
              </div>

              <div class="member-meta">
                <div class="join-info">
                  <lucide-angular [img]="CalendarIcon" size="14" class="meta-icon"></lucide-angular>
                  <span class="join-date">{{ formatDate(member.joinedAt) }}</span>
                </div>
                <span
                  class="status-badge"
                  [class.status-active]="member.isActive"
                  [class.status-inactive]="!member.isActive">
                  {{ member.isActive ? 'Aktif' : 'Pasif' }}
                </span>
              </div>
            </div>

            <div class="card-footer">
              <button
                type="button"
                class="btn btn-sm btn-outline-primary"
                (click)="viewMemberProfile(member)">
                <lucide-angular [img]="EyeIcon" size="14"></lucide-angular>
                Profil
              </button>
              <button
                type="button"
                class="btn btn-sm btn-outline-secondary"
                (click)="sendMessageToMember(member)">
                <lucide-angular [img]="MailIcon" size="14"></lucide-angular>
                Mesaj
              </button>
              <button
                type="button"
                class="btn btn-sm btn-outline-warning"
                (click)="changeMemberRole(member)"
                [disabled]="!canChangeMemberRole(member)">
                <lucide-angular [img]="SettingsIcon" size="14"></lucide-angular>
                Rol
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="filteredMembers().length === 0 && !loading()">
        <div class="empty-state-content">
          <lucide-angular [img]="UsersIcon" size="48" class="empty-icon"></lucide-angular>
          <h3 class="empty-title">Üye bulunamadı</h3>
          <p class="empty-description">
            {{ hasActiveFilters() ? 'Filtrelere uygun üye bulunamadı.' : 'Bu grupta henüz üye bulunmamaktadır.' }}
          </p>
          <button
            *ngIf="!hasActiveFilters()"
            type="button"
            class="btn btn-primary"
            (click)="openAddMemberModal()">
            <lucide-angular [img]="UserPlusIcon" size="16"></lucide-angular>
            İlk Üyeyi Ekle
          </button>
          <button
            *ngIf="hasActiveFilters()"
            type="button"
            class="btn btn-outline-secondary"
            (click)="clearAllFilters()">
            Filtreleri Temizle
          </button>
        </div>
      </div>

      <!-- Loading Overlay -->
      <div class="loading-overlay" *ngIf="loading()">
        <div class="loading-spinner">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Yükleniyor...</span>
          </div>
          <p class="loading-text">Üyeler yükleniyor...</p>
        </div>
      </div>
    </div>

    <!-- Add Member Modal -->
    <div class="modal fade" id="addMemberModal" tabindex="-1">
      <div class="modal-dialog modal-xl">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Gruba Üye Ekle</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>

          <div class="modal-body">
            <!-- Member Search -->
            <div class="member-search-section">
              <div class="search-input-group">
                <lucide-angular [img]="SearchIcon" size="16" class="search-icon"></lucide-angular>
                <input
                  type="text"
                  class="form-control search-input"
                  placeholder="Kullanıcı ara..."
                  [(ngModel)]="memberSearchTerm"
                  (input)="searchAvailableUsers()">
              </div>
            </div>

            <!-- Available Users -->
            <div class="available-users-section">
              <h6 class="section-title">Eklenebilir Kullanıcılar</h6>
              <div class="users-list">
                <div
                  *ngFor="let candidate of availableUsers(); trackBy: trackByCandidateId"
                  class="user-candidate"
                  [class.selected]="candidate.selected">

                  <div class="candidate-selection">
                    <input
                      type="checkbox"
                      class="form-check-input"
                      [(ngModel)]="candidate.selected">
                  </div>

                  <div class="candidate-info">
                    <div class="candidate-avatar" *ngIf="candidate.user.profilePictureUrl; else candidateDefaultAvatar">
                      <img [src]="candidate.user.profilePictureUrl" [alt]="candidate.user.userName" class="avatar-img">
                    </div>
                    <ng-template #candidateDefaultAvatar>
                      <div class="default-avatar">
                        {{ getInitials(candidate.user.firstName, candidate.user.lastName) }}
                      </div>
                    </ng-template>
                    <div class="candidate-details">
                      <span class="candidate-name">{{ candidate.user.firstName }} {{ candidate.user.lastName }}</span>
                      <span class="candidate-username">&#64;{{ candidate.user.userName }}</span>
                      <span class="candidate-email">{{ candidate.user.email }}</span>
                    </div>
                  </div>

                  <div class="candidate-role">
                    <select class="form-select form-select-sm" [(ngModel)]="candidate.role">
                      <option value="Member">Üye</option>
                      <option value="Admin">Yönetici</option>
                      <option value="Owner">Sahip</option>
                    </select>
                  </div>
                </div>
              </div>
            </div>

            <!-- Selected Members Summary -->
            <div class="selected-members-summary" *ngIf="selectedCandidates().length > 0">
              <h6 class="section-title">
                Seçili Üyeler ({{ selectedCandidates().length }})
              </h6>
              <div class="selected-list">
                <span
                  *ngFor="let candidate of selectedCandidates()"
                  class="selected-member-tag">
                  {{ candidate.user.firstName }} {{ candidate.user.lastName }}
                  <span class="role-in-tag">{{ getRoleDisplayName(candidate.role) }}</span>
                  <button
                    type="button"
                    class="btn-remove-selection"
                    (click)="candidate.selected = false">
                    <lucide-angular [img]="XIcon" size="12"></lucide-angular>
                  </button>
                </span>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              İptal
            </button>
            <button
              type="button"
              class="btn btn-primary"
              (click)="addSelectedMembers()"
              [disabled]="selectedCandidates().length === 0 || saving()">
              <span class="spinner-border spinner-border-sm me-2" *ngIf="saving()"></span>
              {{ selectedCandidates().length }} Üyeyi Ekle
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Change Role Modal -->
    <div class="modal fade" id="changeRoleModal" tabindex="-1">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Rol Değiştir</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>

          <form [formGroup]="changeRoleForm" (ngSubmit)="saveRoleChange()">
            <div class="modal-body">
              <div class="member-info-summary" *ngIf="selectedMemberForRoleChange()">
                <div class="member-summary">
                  <div class="member-avatar" *ngIf="selectedMemberForRoleChange()!.profilePictureUrl; else summaryDefaultAvatar">
                    <img [src]="selectedMemberForRoleChange()!.profilePictureUrl" [alt]="selectedMemberForRoleChange()!.userName" class="avatar-img">
                  </div>
                  <ng-template #summaryDefaultAvatar>
                    <div class="default-avatar">
                      {{ getInitials(selectedMemberForRoleChange()!.firstName, selectedMemberForRoleChange()!.lastName) }}
                    </div>
                  </ng-template>
                  <div class="member-details">
                    <span class="member-name">{{ selectedMemberForRoleChange()!.firstName }} {{ selectedMemberForRoleChange()!.lastName }}</span>
                    <span class="member-username">&#64;{{ selectedMemberForRoleChange()!.userName }}</span>
                    <span class="current-role">Mevcut Rol: {{ getRoleDisplayName(selectedMemberForRoleChange()!.role) }}</span>
                  </div>
                </div>
              </div>

              <div class="mb-3">
                <label class="form-label">Yeni Rol</label>
                <select class="form-select" formControlName="newRole">
                  <option value="Member">Üye - Temel grup izinleri</option>
                  <option value="Admin">Yönetici - Üye yönetimi yetkisi</option>
                  <option value="Owner">Sahip - Tam yönetim yetkisi</option>
                </select>
              </div>

              <div class="role-permissions-info">
                <div class="alert alert-info">
                  <h6>Rol İzinleri:</h6>
                  <ul class="permission-list">
                    <li *ngFor="let permission of getRolePermissions(changeRoleForm.get('newRole')?.value)">
                      {{ permission }}
                    </li>
                  </ul>
                </div>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                İptal
              </button>
              <button
                type="submit"
                class="btn btn-primary"
                [disabled]="changeRoleForm.invalid || saving()">
                <span class="spinner-border spinner-border-sm me-2" *ngIf="saving()"></span>
                Rolü Değiştir
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .member-management-container {
      padding: 1.5rem;
      max-width: 1400px;
      margin: 0 auto;
    }

    .page-header {
      margin-bottom: 2rem;
    }

    .header-content {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1.5rem;
    }

    .breadcrumb-nav {
      font-size: 0.9rem;
      color: var(--bs-gray-600);
      margin-bottom: 0.5rem;
    }

    .breadcrumb-link {
      color: var(--bs-primary);
      text-decoration: none;
    }

    .breadcrumb-link:hover {
      text-decoration: underline;
    }

    .breadcrumb-separator {
      margin: 0 0.5rem;
    }

    .breadcrumb-current {
      color: var(--bs-gray-700);
    }

    .title-section {
      flex: 1;
    }

    .title-with-icon {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.5rem;
    }

    .title-icon {
      color: var(--bs-primary);
    }

    .page-title {
      margin: 0;
      font-size: 1.75rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .page-subtitle {
      margin: 0;
      color: var(--bs-gray-600);
      font-size: 0.95rem;
    }

    .header-actions {
      display: flex;
      gap: 0.75rem;
      align-items: center;
    }

    .group-info-card {
      background: white;
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      padding: 1.25rem;
      margin-bottom: 1.5rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .group-basic-info {
      flex: 1;
    }

    .group-name-section {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.5rem;
    }

    .group-name {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .system-badge {
      background: var(--bs-warning-bg);
      color: var(--bs-warning);
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .group-description {
      color: var(--bs-gray-600);
      margin: 0;
    }

    .group-stats-overview {
      display: flex;
      gap: 2rem;
    }

    .stat-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.25rem;
      text-align: center;
    }

    .stat-icon {
      color: var(--bs-gray-500);
    }

    .stat-icon.active {
      color: var(--bs-success);
    }

    .stat-icon.pending {
      color: var(--bs-warning);
    }

    .stat-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--bs-gray-900);
    }

    .stat-label {
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .role-distribution {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 1rem;
      margin-top: 1.5rem;
    }

    .role-card {
      background: white;
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      padding: 1rem;
      text-align: center;
    }

    .role-card-header {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      margin-bottom: 0.75rem;
    }

    .role-icon.owner { color: var(--bs-warning); }
    .role-icon.admin { color: var(--bs-primary); }
    .role-icon.member { color: var(--bs-success); }

    .role-name {
      font-weight: 600;
      color: var(--bs-gray-700);
    }

    .role-count {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--bs-gray-900);
    }

    .role-percentage {
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .controls-section {
      background: white;
      border-radius: 0.5rem;
      padding: 1.25rem;
      margin-bottom: 1.5rem;
      border: 1px solid var(--bs-gray-200);
    }

    .filters-row {
      display: flex;
      gap: 1rem;
      align-items: center;
      flex-wrap: wrap;
    }

    .search-input-group {
      position: relative;
      flex: 1;
      min-width: 250px;
    }

    .search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--bs-gray-500);
      z-index: 2;
    }

    .search-input {
      padding-left: 2.5rem;
      padding-right: 2.5rem;
    }

    .btn-clear-search {
      position: absolute;
      right: 0.5rem;
      top: 50%;
      transform: translateY(-50%);
      background: none;
      border: none;
      color: var(--bs-gray-500);
      padding: 0.25rem;
      border-radius: 0.25rem;
    }

    .btn-clear-search:hover {
      background: var(--bs-gray-100);
      color: var(--bs-gray-700);
    }

    .filter-group {
      min-width: 140px;
    }

    .sort-controls {
      display: flex;
      gap: 0.25rem;
    }

    .sort-select {
      min-width: 180px;
    }

    .sort-direction {
      border-left: none;
      border-top-left-radius: 0;
      border-bottom-left-radius: 0;
    }

    .view-toggle .btn-group {
      box-shadow: none;
    }

    .view-toggle .btn {
      border-color: var(--bs-gray-300);
    }

    .view-toggle .btn.active {
      background: var(--bs-primary);
      border-color: var(--bs-primary);
      color: white;
    }

    .bulk-operations-bar {
      background: var(--bs-primary-bg);
      border: 1px solid var(--bs-primary-border);
      border-radius: 0.5rem;
      padding: 1rem 1.25rem;
      margin-bottom: 1.5rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .bulk-info {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .selected-count {
      font-weight: 600;
      color: var(--bs-primary);
    }

    .btn-link {
      background: none;
      border: none;
      color: var(--bs-primary);
      text-decoration: none;
      font-size: 0.9rem;
    }

    .btn-link:hover {
      text-decoration: underline;
    }

    .bulk-actions {
      display: flex;
      gap: 0.5rem;
    }

    .members-content {
      background: white;
      border-radius: 0.5rem;
      overflow: hidden;
      border: 1px solid var(--bs-gray-200);
    }

    .table-container {
      overflow-x: auto;
    }

    .members-table {
      margin: 0;
    }

    .members-table th {
      background: var(--bs-gray-50);
      border-bottom: 2px solid var(--bs-gray-200);
      font-weight: 600;
      color: var(--bs-gray-700);
      padding: 1rem 0.75rem;
    }

    .members-table td {
      padding: 1rem 0.75rem;
      vertical-align: middle;
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .members-table tbody tr:hover {
      background: var(--bs-gray-50);
    }

    .members-table tbody tr.selected {
      background: var(--bs-primary-bg);
    }

    .select-column,
    .actions-column {
      width: 60px;
    }

    .member-info {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .member-avatar,
    .default-avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      overflow: hidden;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .avatar-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .default-avatar {
      background: var(--bs-primary);
      color: white;
      font-weight: 600;
      font-size: 0.9rem;
    }

    .member-details {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .member-name {
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .member-username {
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .member-email {
      color: var(--bs-gray-600);
      font-size: 0.9rem;
    }

    .role-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.375rem 0.75rem;
      border-radius: 0.25rem;
      font-size: 0.85rem;
      font-weight: 500;
    }

    .role-badge.owner {
      background: var(--bs-warning-bg);
      color: var(--bs-warning);
      border: 1px solid var(--bs-warning-border);
    }

    .role-badge.admin {
      background: var(--bs-primary-bg);
      color: var(--bs-primary);
      border: 1px solid var(--bs-primary-border);
    }

    .role-badge.member {
      background: var(--bs-success-bg);
      color: var(--bs-success);
      border: 1px solid var(--bs-success-border);
    }

    .status-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.85rem;
      font-weight: 500;
    }

    .status-badge.status-active {
      background: var(--bs-success-bg);
      color: var(--bs-success);
    }

    .status-badge.status-inactive {
      background: var(--bs-danger-bg);
      color: var(--bs-danger);
    }

    .join-date {
      color: var(--bs-gray-600);
      font-size: 0.9rem;
    }

    .action-buttons {
      display: flex;
      gap: 0.25rem;
    }

    .action-buttons .btn {
      padding: 0.375rem 0.5rem;
    }

    .members-grid {
      background: var(--bs-gray-50);
      padding: 1.5rem;
      border-radius: 0.5rem;
    }

    .member-card-container {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1.5rem;
    }

    .member-card {
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
      transition: all 0.2s ease;
      overflow: hidden;
    }

    .member-card:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      transform: translateY(-2px);
    }

    .member-card.selected {
      border-color: var(--bs-primary);
      box-shadow: 0 0 0 2px var(--bs-primary-bg);
    }

    .card-header {
      padding: 1.25rem;
      border-bottom: 1px solid var(--bs-gray-200);
      display: flex;
      align-items: flex-start;
      gap: 1rem;
    }

    .member-avatar-section {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .status-indicator {
      position: absolute;
      bottom: 0;
      right: 0;
      width: 12px;
      height: 12px;
      border-radius: 50%;
      border: 2px solid white;
    }

    .status-indicator.status-active {
      background: var(--bs-success);
    }

    .status-indicator.status-inactive {
      background: var(--bs-danger);
    }

    .card-body {
      padding: 1.25rem;
    }

    .member-name {
      font-size: 1.1rem;
      font-weight: 600;
      color: var(--bs-gray-900);
      margin-bottom: 0.25rem;
    }

    .member-username {
      color: var(--bs-gray-600);
      font-size: 0.9rem;
      margin-bottom: 0.25rem;
    }

    .member-email {
      color: var(--bs-gray-600);
      font-size: 0.85rem;
      margin-bottom: 1rem;
    }

    .member-role-section {
      margin-bottom: 1rem;
    }

    .member-meta {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.85rem;
    }

    .join-info {
      display: flex;
      align-items: center;
      gap: 0.375rem;
    }

    .meta-icon {
      color: var(--bs-gray-500);
    }

    .card-footer {
      padding: 1rem 1.25rem;
      background: var(--bs-gray-50);
      border-top: 1px solid var(--bs-gray-200);
      display: flex;
      gap: 0.5rem;
    }

    .card-footer .btn {
      flex: 1;
      font-size: 0.85rem;
      padding: 0.5rem 0.75rem;
    }

    .empty-state {
      text-align: center;
      padding: 4rem 2rem;
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
    }

    .empty-state-content {
      max-width: 400px;
      margin: 0 auto;
    }

    .empty-icon {
      color: var(--bs-gray-400);
      margin-bottom: 1.5rem;
    }

    .empty-title {
      margin-bottom: 1rem;
      color: var(--bs-gray-700);
    }

    .empty-description {
      color: var(--bs-gray-600);
      margin-bottom: 2rem;
    }

    .loading-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255, 255, 255, 0.8);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1050;
    }

    .loading-spinner {
      text-align: center;
    }

    .loading-text {
      margin-top: 1rem;
      color: var(--bs-gray-600);
    }

    .modal-xl {
      max-width: 1200px;
    }

    .member-search-section {
      margin-bottom: 1.5rem;
    }

    .available-users-section {
      margin-bottom: 1.5rem;
    }

    .section-title {
      font-weight: 600;
      color: var(--bs-gray-700);
      margin-bottom: 1rem;
    }

    .users-list {
      max-height: 400px;
      overflow-y: auto;
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
    }

    .user-candidate {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      border-bottom: 1px solid var(--bs-gray-200);
      transition: background-color 0.2s ease;
    }

    .user-candidate:hover {
      background: var(--bs-gray-50);
    }

    .user-candidate.selected {
      background: var(--bs-primary-bg);
    }

    .user-candidate:last-child {
      border-bottom: none;
    }

    .candidate-info {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex: 1;
    }

    .candidate-avatar,
    .candidate-details .default-avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      overflow: hidden;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .candidate-details {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .candidate-name {
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .candidate-username,
    .candidate-email {
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .candidate-role {
      min-width: 120px;
    }

    .selected-members-summary {
      background: var(--bs-gray-50);
      border-radius: 0.5rem;
      padding: 1rem;
    }

    .selected-list {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .selected-member-tag {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      background: var(--bs-primary);
      color: white;
      padding: 0.375rem 0.75rem;
      border-radius: 0.25rem;
      font-size: 0.85rem;
    }

    .role-in-tag {
      background: rgba(255, 255, 255, 0.2);
      padding: 0.125rem 0.375rem;
      border-radius: 0.125rem;
      font-size: 0.75rem;
    }

    .btn-remove-selection {
      background: none;
      border: none;
      color: white;
      padding: 0.125rem;
      border-radius: 0.125rem;
      display: flex;
      align-items: center;
    }

    .btn-remove-selection:hover {
      background: rgba(255, 255, 255, 0.2);
    }

    .member-info-summary {
      background: var(--bs-gray-50);
      border-radius: 0.5rem;
      padding: 1rem;
      margin-bottom: 1.5rem;
    }

    .member-summary {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .current-role {
      font-size: 0.85rem;
      color: var(--bs-primary);
      font-weight: 500;
    }

    .role-permissions-info {
      margin-top: 1rem;
    }

    .permission-list {
      margin: 0.5rem 0 0 0;
      padding-left: 1.25rem;
    }

    .permission-list li {
      margin-bottom: 0.25rem;
      font-size: 0.9rem;
    }

    @media (max-width: 768px) {
      .member-management-container {
        padding: 1rem;
      }

      .header-content {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .header-actions {
        justify-content: stretch;
      }

      .group-info-card {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .group-stats-overview {
        justify-content: space-around;
      }

      .filters-row {
        flex-direction: column;
        align-items: stretch;
      }

      .role-distribution {
        grid-template-columns: 1fr;
      }

      .members-grid .member-card-container {
        grid-template-columns: 1fr;
      }

      .bulk-operations-bar {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .bulk-actions {
        justify-content: center;
      }
    }
  `]
})
export class GroupMemberManagementComponent implements OnInit {
  // Input for group ID (can be set via route parameter)
  @Input() groupId?: string;

  // Dependency Injection
  private readonly groupService = inject(GroupService);
  private readonly userService = inject(UserService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly errorHandler = inject(ErrorHandlerService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  // Icons
  readonly UsersIcon = Users;
  readonly UserPlusIcon = UserPlus;
  readonly UserMinusIcon = UserMinus;
  readonly SearchIcon = Search;
  readonly FilterIcon = Filter;
  readonly MoreVerticalIcon = MoreVertical;
  readonly Edit2Icon = Edit2;
  readonly Trash2Icon = Trash2;
  readonly CrownIcon = Crown;
  readonly ShieldIcon = Shield;
  readonly UserIcon = User;
  readonly MailIcon = Mail;
  readonly CalendarIcon = Calendar;
  readonly ArrowUpDownIcon = ArrowUpDown;
  readonly DownloadIcon = Download;
  readonly UploadIcon = Upload;
  readonly CheckCircleIcon = CheckCircle;
  readonly AlertCircleIcon = AlertCircle;
  readonly ClockIcon = Clock;
  readonly XIcon = X;
  readonly PlusIcon = Plus;
  readonly SettingsIcon = Settings;
  readonly EyeIcon = Eye;
  readonly CopyIcon = Copy;
  readonly RefreshCwIcon = RefreshCw;
  readonly ChevronDownIcon = ChevronDown;
  readonly GridIcon = Grid;
  readonly ListIcon = List;

  // State Signals
  group = signal<GroupDto | null>(null);
  members = signal<GroupMemberDto[]>([]);
  selectedMembers = signal<GroupMemberDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  statistics = signal<MemberStatistics | null>(null);

  // UI State
  viewMode = signal<'table' | 'grid'>('table');
  selectedMemberForRoleChange = signal<GroupMemberDto | null>(null);

  // Filters
  filters = signal<MemberFilter>({
    search: '',
    role: 'all',
    status: 'all',
    sortBy: 'userName',
    sortDirection: 'asc'
  });

  // Add Member Modal State
  memberSearchTerm = '';
  availableUsers = signal<AddMemberCandidate[]>([]);

  // Forms
  changeRoleForm: FormGroup;

  // Computed Values
  filteredMembers = computed(() => {
    const members = this.members();
    const f = this.filters();

    let filtered = members.filter(member => {
      // Search filter
      const searchTerm = f.search.toLowerCase();
      const matchesSearch = !searchTerm ||
        member.firstName.toLowerCase().includes(searchTerm) ||
        member.lastName.toLowerCase().includes(searchTerm) ||
        member.userName.toLowerCase().includes(searchTerm) ||
        member.email.toLowerCase().includes(searchTerm);

      // Role filter
      const matchesRole = f.role === 'all' || member.role === f.role;

      // Status filter
      const matchesStatus = f.status === 'all' ||
        (f.status === 'active' && member.isActive) ||
        (f.status === 'inactive' && !member.isActive);

      return matchesSearch && matchesRole && matchesStatus;
    });

    // Sort
    filtered.sort((a, b) => {
      let aValue: any;
      let bValue: any;

      switch (f.sortBy) {
        case 'userName':
          aValue = a.userName.toLowerCase();
          bValue = b.userName.toLowerCase();
          break;
        case 'role':
          aValue = a.role;
          bValue = b.role;
          break;
        case 'joinedAt':
          aValue = new Date(a.joinedAt);
          bValue = new Date(b.joinedAt);
          break;
        case 'email':
          aValue = a.email.toLowerCase();
          bValue = b.email.toLowerCase();
          break;
        default:
          aValue = a.userName.toLowerCase();
          bValue = b.userName.toLowerCase();
      }

      const comparison = aValue < bValue ? -1 : aValue > bValue ? 1 : 0;
      return f.sortDirection === 'asc' ? comparison : -comparison;
    });

    return filtered;
  });

  selectedCandidates = computed(() => {
    return this.availableUsers().filter(candidate => candidate.selected);
  });

  roleCards = computed(() => {
    const stats = this.statistics();
    if (!stats) return [];

    const total = stats.totalMembers;
    const roles = [
      {
        name: 'Sahip',
        count: stats.membersByRole.Owner || 0,
        percentage: total > 0 ? Math.round(((stats.membersByRole.Owner || 0) / total) * 100) : 0,
        icon: Crown,
        class: 'owner'
      },
      {
        name: 'Yönetici',
        count: stats.membersByRole.Admin || 0,
        percentage: total > 0 ? Math.round(((stats.membersByRole.Admin || 0) / total) * 100) : 0,
        icon: Shield,
        class: 'admin'
      },
      {
        name: 'Üye',
        count: stats.membersByRole.Member || 0,
        percentage: total > 0 ? Math.round(((stats.membersByRole.Member || 0) / total) * 100) : 0,
        icon: User,
        class: 'member'
      }
    ];

    return roles;
  });

  // Bulk Operations
  bulkOperations: BulkMemberOperation[] = [
    {
      type: 'export',
      label: 'Dışa Aktar',
      icon: Download,
      confirmMessage: 'Seçili üyeleri dışa aktarmak istediğinizden emin misiniz?'
    },
    {
      type: 'changeRole',
      label: 'Rol Değiştir',
      icon: Settings,
      confirmMessage: 'Seçili üyelerin rollerini değiştirmek istediğinizden emin misiniz?',
      requiresInput: true
    },
    {
      type: 'activate',
      label: 'Aktifleştir',
      icon: CheckCircle,
      confirmMessage: 'Seçili üyeleri aktifleştirmek istediğinizden emin misiniz?'
    },
    {
      type: 'deactivate',
      label: 'Pasifleştir',
      icon: AlertCircle,
      confirmMessage: 'Seçili üyeleri pasifleştirmek istediğinizden emin misiniz?'
    },
    {
      type: 'remove',
      label: 'Gruptan Çıkar',
      icon: UserMinus,
      confirmMessage: 'Seçili üyeleri gruptan çıkarmak istediğinizden emin misiniz? Bu işlem geri alınamaz.',
      destructive: true
    }
  ];

  constructor() {
    this.changeRoleForm = this.fb.group({
      newRole: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    // Get group ID from route parameter if not provided as input
    if (!this.groupId) {
      this.route.params.subscribe(params => {
        this.groupId = params['id'];
        if (this.groupId) {
          this.loadGroupAndMembers();
        }
      });
    } else {
      this.loadGroupAndMembers();
    }
  }

  // Data Loading Methods
  async loadGroupAndMembers(): Promise<void> {
    if (!this.groupId) return;

    await Promise.all([
      this.loadGroup(),
      this.loadMembers(),
      this.loadStatistics()
    ]);
  }

  async loadGroup(): Promise<void> {
    if (!this.groupId) return;

    try {
      const response = await this.groupService.getGroup(this.groupId).toPromise();
      if (response) {
        this.group.set(response);
      }
    } catch (error) {
      this.errorHandler.handleError(error);
    }
  }

  async loadMembers(): Promise<void> {
    if (!this.groupId) return;

    try {
      this.loading.set(true);
      const response = await this.groupService.getGroupMembers(this.groupId).toPromise();
      if (response) {
        this.members.set(response);
      }
    } catch (error) {
      this.errorHandler.handleError(error);
    } finally {
      this.loading.set(false);
    }
  }

  async loadStatistics(): Promise<void> {
    if (!this.groupId) return;

    try {
      const response = await this.groupService.getGroupMemberStatistics(this.groupId).toPromise();
      if (response) {
        this.statistics.set(response);
      }
    } catch (error) {
      console.error('Member statistics loading failed:', error);
    }
  }

  async refreshData(): Promise<void> {
    await this.loadGroupAndMembers();
  }

  // Filter Methods
  onSearchChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.filters.update(f => ({ ...f, search: target.value }));
  }

  clearSearch(): void {
    this.filters.update(f => ({ ...f, search: '' }));
  }

  onSortChange(): void {
    // Sorting is handled by computed property
  }

  toggleSortDirection(): void {
    this.filters.update(f => ({
      ...f,
      sortDirection: f.sortDirection === 'asc' ? 'desc' : 'asc'
    }));
  }

  applyFilters(): void {
    // Filters are applied automatically through computed property
  }

  clearAllFilters(): void {
    this.filters.set({
      search: '',
      role: 'all',
      status: 'all',
      sortBy: 'userName',
      sortDirection: 'asc'
    });
  }

  hasActiveFilters(): boolean {
    const f = this.filters();
    return !!(f.search || f.role !== 'all' || f.status !== 'all');
  }

  // View Mode Methods
  setViewMode(mode: 'table' | 'grid'): void {
    this.viewMode.set(mode);
  }

  // Selection Methods
  toggleSelectAll(): void {
    const allSelected = this.isAllSelected();
    if (allSelected) {
      this.selectedMembers.set([]);
    } else {
      this.selectedMembers.set([...this.filteredMembers()]);
    }
  }

  toggleMemberSelection(member: GroupMemberDto): void {
    const selected = this.selectedMembers();
    const index = selected.findIndex(m => m.userId === member.userId);

    if (index >= 0) {
      this.selectedMembers.set(selected.filter(m => m.userId !== member.userId));
    } else {
      this.selectedMembers.set([...selected, member]);
    }
  }

  isMemberSelected(member: GroupMemberDto): boolean {
    return this.selectedMembers().some(m => m.userId === member.userId);
  }

  isAllSelected(): boolean {
    const members = this.filteredMembers();
    const selected = this.selectedMembers();
    return members.length > 0 && selected.length === members.length;
  }

  isPartiallySelected(): boolean {
    const selected = this.selectedMembers();
    return selected.length > 0 && selected.length < this.filteredMembers().length;
  }

  clearSelection(): void {
    this.selectedMembers.set([]);
  }

  // Member Management Methods
  async openAddMemberModal(): Promise<void> {
    await this.searchAvailableUsers();
    // Open modal programmatically
  }

  async searchAvailableUsers(): Promise<void> {
    try {
      const currentMemberIds = this.members().map(m => m.userId);
      const response = await this.userService.getAvailableUsers({
        search: this.memberSearchTerm,
        excludeUserIds: currentMemberIds
      }).toPromise();

      if (response) {
        const candidates: AddMemberCandidate[] = response.data.map(user => ({
          user,
          role: 'Member' as GroupMemberRole,
          selected: false
        }));
        this.availableUsers.set(candidates);
      }
    } catch (error) {
      this.errorHandler.handleError(error);
    }
  }

  async addSelectedMembers(): Promise<void> {
    if (!this.groupId) return;

    const selected = this.selectedCandidates();
    if (selected.length === 0) return;

    try {
      this.saving.set(true);

      const requests: GroupMemberRequest[] = selected.map(candidate => ({
        userId: candidate.user.id,
        role: candidate.role
      }));

      await this.groupService.addMembersToGroup(this.groupId, requests).toPromise();
        await this.loadMembers();
        await this.loadStatistics();
        // Close modal
        this.availableUsers.set([]);
        this.memberSearchTerm = '';
      }
    } catch (error) {
      this.errorHandler.handleError(error);
    } finally {
      this.saving.set(false);
    }
  }

  changeMemberRole(member: GroupMemberDto): void {
    this.selectedMemberForRoleChange.set(member);
    this.changeRoleForm.patchValue({
      newRole: member.role
    });
    // Open modal programmatically
  }

  async saveRoleChange(): Promise<void> {
    if (!this.groupId || this.changeRoleForm.invalid) return;

    const member = this.selectedMemberForRoleChange();
    if (!member) return;

    try {
      this.saving.set(true);
      const newRole = this.changeRoleForm.get('newRole')?.value;

      const response = await this.groupService.changeGroupMemberRole(
        this.groupId,
        member.userId,
        newRole
      ).toPromise();

      await this.loadMembers();
      await this.loadStatistics();
      // Close modal
    } catch (error) {
      this.errorHandler.handleError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async removeMemberFromGroup(member: GroupMemberDto): Promise<void> {
    if (!this.groupId || !this.canRemoveMember(member)) return;

    const confirmed = await this.confirmationService.confirm({
      title: 'Üyeyi Gruptan Çıkar',
      message: `${member.firstName} ${member.lastName} kullanıcısını gruptan çıkarmak istediğinizden emin misiniz?`,
      confirmText: 'Çıkar',
      confirmButtonClass: 'btn-danger'
    });

    if (confirmed) {
      try {
        await this.groupService.removeMemberFromGroup(this.groupId, member.userId).toPromise();
        await this.loadMembers();
        await this.loadStatistics();
      } catch (error) {
        this.errorHandler.handleError(error);
      }
    }
  }

  async toggleMemberStatus(member: GroupMemberDto): Promise<void> {
    if (!this.groupId) return;

    try {
      await this.groupService.toggleGroupMemberStatus(this.groupId, member.userId).toPromise();
      await this.loadMembers();
      await this.loadStatistics();
    } catch (error) {
      this.errorHandler.handleError(error);
    }
  }

  // Permission Methods
  canChangeMemberRole(member: GroupMemberDto): boolean {
    // Implement permission logic based on current user's role
    return true; // Simplified for now
  }

  canRemoveMember(member: GroupMemberDto): boolean {
    // Prevent removing the last owner
    const stats = this.statistics();
    if (stats && member.role === 'Owner' && stats.membersByRole.Owner <= 1) {
      return false;
    }
    return true;
  }

  // Navigation Methods
  viewMemberProfile(member: GroupMemberDto): void {
    this.router.navigate(['/user-management/users', member.userId]);
  }

  sendMessageToMember(member: GroupMemberDto): void {
    // Navigate to messaging or open modal
  }

  viewMemberActivity(member: GroupMemberDto): void {
    // Navigate to activity log for this member
  }

  // Bulk Operations
  async executeBulkOperation(operation: BulkMemberOperation): Promise<void> {
    const selectedIds = this.selectedMembers().map(m => m.userId);

    const confirmed = await this.confirmationService.confirm({
      title: `Toplu ${operation.label}`,
      message: operation.confirmMessage,
      confirmText: operation.label,
    });

    if (!confirmed) return;

    try {
      switch (operation.type) {
        case 'remove':
          await this.bulkRemoveMembers(selectedIds);
          break;
        case 'changeRole':
          // Open bulk role change modal
          break;
        case 'activate':
          await this.bulkToggleMemberStatus(selectedIds, true);
          break;
        case 'deactivate':
          await this.bulkToggleMemberStatus(selectedIds, false);
          break;
        case 'export':
          // Export selected members
          break;
      }
    } catch (error) {
      this.errorHandler.handleError(error);
    }
  }

  private async bulkRemoveMembers(userIds: string[]): Promise<void> {
    if (!this.groupId) return;

    await this.groupService.removeMembersFromGroup(this.groupId, userIds);
    await this.loadMembers();
    await this.loadStatistics();
    this.clearSelection();
  }

  private async bulkToggleMemberStatus(userIds: string[], activate: boolean): Promise<void> {
    if (!this.groupId) return;

    await this.groupService.bulkToggleGroupMemberStatus(this.groupId, userIds, activate);
    await this.loadMembers();
    await this.loadStatistics();
    this.clearSelection();
  }

  // Export Methods
  async exportMembers(format: 'excel' | 'csv' | 'pdf'): Promise<void> {
    try {
      const members = this.selectedMembers().length > 0 ? this.selectedMembers() : this.filteredMembers();
      // Implementation for export
    } catch (error) {
      this.errorHandler.handleError(error);
    }
  }

  openBulkImportModal(): void {
    // Open bulk import modal
  }

  // Utility Methods
  trackByMemberId(index: number, member: GroupMemberDto): string {
    return member.userId;
  }

  trackByCandidateId(index: number, candidate: AddMemberCandidate): string {
    return candidate.user.id;
  }

  getInitials(firstName: string, lastName: string): string {
    return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();
  }

  getRoleDisplayName(role: GroupMemberRole): string {
    const names: Record<GroupMemberRole, string> = {
      Owner: 'Sahip',
      Admin: 'Yönetici',
      Member: 'Üye'
    };
    return names[role] || role;
  }

  getRoleIcon(role: GroupMemberRole): any {
    const icons: Record<GroupMemberRole, any> = {
      Owner: Crown,
      Admin: Shield,
      Member: User
    };
    return icons[role] || User;
  }

  getRoleClass(role: GroupMemberRole): string {
    return role.toLowerCase();
  }

  getRolePermissions(role: GroupMemberRole): string[] {
    const permissions: Record<GroupMemberRole, string[]> = {
      Owner: [
        'Grup ayarlarını düzenleme',
        'Üye ekleme ve çıkarma',
        'Tüm üyelerin rollerini değiştirme',
        'Grup izinlerini yönetme',
        'Grubu silme'
      ],
      Admin: [
        'Üye ekleme ve çıkarma',
        'Üye rollerini değiştirme (Sahip hariç)',
        'Grup aktivitelerini görüntüleme',
        'Raporlara erişim'
      ],
      Member: [
        'Grup içeriklerini görüntüleme',
        'Grup aktivitelerine katılım',
        'Temel grup izinleri'
      ]
    };
    return permissions[role] || [];
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(new Date(date));
  }
}