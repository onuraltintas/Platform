import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, signal, computed, inject, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { LucideAngularModule, Search, Filter, X, Plus, Minus, Settings, Save, RefreshCw, Download,
         Calendar, Clock, User, Shield, Database, ChevronDown, ChevronRight, AlertTriangle,
         CheckCircle, Copy, Trash2, Edit, Eye, MoreVertical, Target, Layers, Tag } from 'lucide-angular';

import { UserService } from '../../services/user.service';
import { RoleService } from '../../services/role.service';
import { GroupService } from '../../services/group.service';
import { PermissionService } from '../../services/permission.service';

interface SearchField {
  key: string;
  label: string;
  type: 'text' | 'number' | 'date' | 'select' | 'multiselect' | 'boolean' | 'range';
  dataType: 'string' | 'number' | 'date' | 'boolean';
  options?: SearchOption[];
  validations?: SearchValidation[];
  placeholder?: string;
  description?: string;
  category: string;
}

interface SearchOption {
  value: any;
  label: string;
  disabled?: boolean;
  group?: string;
}

interface SearchValidation {
  type: 'required' | 'min' | 'max' | 'pattern' | 'custom';
  value?: any;
  message: string;
}

interface SearchCondition {
  id: string;
  field: string;
  operator: string;
  value: any;
  logicalOperator?: 'AND' | 'OR';
  group?: string;
}

interface SearchOperator {
  key: string;
  label: string;
  supportedTypes: string[];
  requiresValue: boolean;
  multipleValues?: boolean;
}

interface SearchTemplate {
  id: string;
  name: string;
  description: string;
  conditions: SearchCondition[];
  createdAt: Date;
  createdBy: string;
  isPublic: boolean;
  category: string;
  tags: string[];
  useCount: number;
  lastUsed?: Date;
}

interface SearchResult {
  totalCount: number;
  items: any[];
  facets?: SearchFacet[];
  suggestions?: string[];
  executionTime: number;
}

interface SearchFacet {
  field: string;
  values: { value: any; count: number; label: string }[];
}

interface QuickFilter {
  key: string;
  label: string;
  conditions: SearchCondition[];
  icon: any;
  color: string;
}

@Component({
  selector: 'app-advanced-search-filters',
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
    <div class="advanced-search-container">
      <!-- Header -->
      <div class="search-header">
        <div class="title-section">
          <div class="title-with-icon">
            <lucide-angular [img]="SearchIcon" size="24" class="title-icon"></lucide-angular>
            <div>
              <h1 class="search-title">Gelişmiş Arama</h1>
              <p class="search-subtitle">Detaylı filtreler ile hassas arama yapın</p>
            </div>
          </div>
        </div>

        <div class="search-actions">
          <div class="dropdown">
            <button
              type="button"
              class="btn btn-outline-secondary dropdown-toggle"
              data-bs-toggle="dropdown">
              <lucide-angular [img]="SettingsIcon" size="16"></lucide-angular>
              Şablonlar
            </button>
            <ul class="dropdown-menu">
              <li><a class="dropdown-item" (click)="openTemplateManager()">Şablon Yöneticisi</a></li>
              <li><a class="dropdown-item" (click)="saveAsTemplate()">Şablon Olarak Kaydet</a></li>
              <li><hr class="dropdown-divider"></li>
              <li *ngFor="let template of recentTemplates()" class="dropdown-item-template">
                <a class="dropdown-item" (click)="loadTemplate(template)">
                  {{ template.name }}
                  <small class="text-muted">{{ template.useCount }} kez kullanıldı</small>
                </a>
              </li>
            </ul>
          </div>

          <button
            type="button"
            class="btn btn-outline-primary"
            (click)="clearAllConditions()"
            [disabled]="!hasConditions()">
            <lucide-angular [img]="XIcon" size="16"></lucide-angular>
            Temizle
          </button>

          <button
            type="button"
            class="btn btn-primary"
            (click)="executeSearch()"
            [disabled]="!hasValidConditions() || searching()">
            <span class="spinner-border spinner-border-sm me-2" *ngIf="searching()"></span>
            <lucide-angular [img]="SearchIcon" size="16"></lucide-angular>
            Ara
          </button>
        </div>
      </div>

      <!-- Quick Filters -->
      <div class="quick-filters" *ngIf="quickFilters().length > 0">
        <h6>Hızlı Filtreler</h6>
        <div class="quick-filter-buttons">
          <button
            *ngFor="let filter of quickFilters()"
            type="button"
            class="btn btn-outline-secondary quick-filter-btn"
            [class]="'btn-outline-' + filter.color"
            (click)="applyQuickFilter(filter)">
            <lucide-angular [img]="filter.icon" size="14" class="me-2"></lucide-angular>
            {{ filter.label }}
          </button>
        </div>
      </div>

      <!-- Search Builder -->
      <div class="search-builder">
        <div class="builder-header">
          <h5>Arama Koşulları</h5>
          <div class="builder-actions">
            <div class="dropdown">
              <button
                type="button"
                class="btn btn-sm btn-outline-secondary dropdown-toggle"
                data-bs-toggle="dropdown">
                <lucide-angular [img]="PlusIcon" size="14"></lucide-angular>
                Koşul Ekle
              </button>
              <ul class="dropdown-menu">
                <li class="dropdown-header">Kategoriler</li>
                <li *ngFor="let category of fieldCategories()">
                  <div class="dropdown-submenu">
                    <a class="dropdown-item dropdown-toggle" (click)="toggleCategory(category)">
                      {{ category.label }}
                      <lucide-angular [img]="ChevronRightIcon" size="12" class="float-end"></lucide-angular>
                    </a>
                    <ul class="dropdown-menu" [class.show]="isCategoryExpanded(category.key)">
                      <li *ngFor="let field of getFieldsByCategory(category.key)">
                        <a class="dropdown-item" (click)="addCondition(field.key)">
                          {{ field.label }}
                          <small class="text-muted d-block">{{ field.description }}</small>
                        </a>
                      </li>
                    </ul>
                  </div>
                </li>
              </ul>
            </div>

            <button
              type="button"
              class="btn btn-sm btn-outline-secondary"
              (click)="addConditionGroup()"
              title="Koşul Grubu Ekle">
              <lucide-angular [img]="LayersIcon" size="14"></lucide-angular>
            </button>
          </div>
        </div>

        <!-- Condition Groups -->
        <div class="condition-groups">
          <div
            *ngFor="let group of conditionGroups(); let groupIndex = index; trackBy: trackByGroupId"
            class="condition-group"
            [class.has-error]="hasGroupError(group)">

            <!-- Group Header -->
            <div class="group-header" *ngIf="conditionGroups().length > 1">
              <div class="group-info">
                <span class="group-label">Grup {{ groupIndex + 1 }}</span>
                <select
                  class="form-select form-select-sm group-operator"
                  [(ngModel)]="group.logicalOperator"
                  (change)="onGroupOperatorChange()">
                  <option value="AND">VE</option>
                  <option value="OR">VEYA</option>
                </select>
              </div>
              <button
                type="button"
                class="btn btn-sm btn-outline-danger"
                (click)="removeConditionGroup(group.id)"
                title="Grubu Sil">
                <lucide-angular [img]="Trash2Icon" size="12"></lucide-angular>
              </button>
            </div>

            <!-- Conditions in Group -->
            <div class="conditions-list">
              <div
                *ngFor="let condition of group.conditions; let conditionIndex = index; trackBy: trackByConditionId"
                class="condition-item"
                [class.has-error]="hasConditionError(condition)">

                <!-- Logical Operator (except first condition) -->
                <div class="logical-operator" *ngIf="conditionIndex > 0">
                  <select
                    class="form-select form-select-sm"
                    [(ngModel)]="condition.logicalOperator"
                    (change)="onConditionChange()">
                    <option value="AND">VE</option>
                    <option value="OR">VEYA</option>
                  </select>
                </div>

                <!-- Field Selection -->
                <div class="field-selection">
                  <select
                    class="form-select"
                    [(ngModel)]="condition.field"
                    (change)="onFieldChange(condition)"
                    [class.is-invalid]="!condition.field">
                    <option value="">Alan Seç</option>
                    <optgroup *ngFor="let category of fieldCategories()" [label]="category.label">
                      <option
                        *ngFor="let field of getFieldsByCategory(category.key)"
                        [value]="field.key">
                        {{ field.label }}
                      </option>
                    </optgroup>
                  </select>
                </div>

                <!-- Operator Selection -->
                <div class="operator-selection">
                  <select
                    class="form-select"
                    [(ngModel)]="condition.operator"
                    (change)="onOperatorChange(condition)"
                    [disabled]="!condition.field">
                    <option value="">Operatör</option>
                    <option
                      *ngFor="let operator of getAvailableOperators(condition.field)"
                      [value]="operator.key">
                      {{ operator.label }}
                    </option>
                  </select>
                </div>

                <!-- Value Input -->
                <div class="value-input" *ngIf="shouldShowValueInput(condition)">
                  <ng-container [ngSwitch]="getFieldInputType(condition.field)">
                    <!-- Text Input -->
                    <input
                      *ngSwitchCase="'text'"
                      type="text"
                      class="form-control"
                      [(ngModel)]="condition.value"
                      [placeholder]="getFieldPlaceholder(condition.field)"
                      (input)="onConditionChange()">

                    <!-- Number Input -->
                    <input
                      *ngSwitchCase="'number'"
                      type="number"
                      class="form-control"
                      [(ngModel)]="condition.value"
                      [placeholder]="getFieldPlaceholder(condition.field)"
                      (input)="onConditionChange()">

                    <!-- Date Input -->
                    <input
                      *ngSwitchCase="'date'"
                      type="date"
                      class="form-control"
                      [(ngModel)]="condition.value"
                      (change)="onConditionChange()">

                    <!-- DateTime Input -->
                    <input
                      *ngSwitchCase="'datetime'"
                      type="datetime-local"
                      class="form-control"
                      [(ngModel)]="condition.value"
                      (change)="onConditionChange()">

                    <!-- Select Input -->
                    <select
                      *ngSwitchCase="'select'"
                      class="form-select"
                      [(ngModel)]="condition.value"
                      (change)="onConditionChange()">
                      <option value="">Seçin</option>
                      <option
                        *ngFor="let option of getFieldOptions(condition.field)"
                        [value]="option.value">
                        {{ option.label }}
                      </option>
                    </select>

                    <!-- Multi-Select Input -->
                    <div *ngSwitchCase="'multiselect'" class="multiselect-container">
                      <div class="selected-values" *ngIf="condition.value?.length">
                        <span
                          *ngFor="let value of condition.value"
                          class="selected-value">
                          {{ getOptionLabel(condition.field, value) }}
                          <button
                            type="button"
                            class="btn-remove-value"
                            (click)="removeMultiSelectValue(condition, value)">
                            <lucide-angular [img]="XIcon" size="10"></lucide-angular>
                          </button>
                        </span>
                      </div>
                      <select
                        class="form-select"
                        (change)="addMultiSelectValue(condition, $event)"
                        [value]="''">
                        <option value="">Değer Ekle</option>
                        <option
                          *ngFor="let option of getAvailableMultiSelectOptions(condition)"
                          [value]="option.value">
                          {{ option.label }}
                        </option>
                      </select>
                    </div>

                    <!-- Boolean Input -->
                    <select
                      *ngSwitchCase="'boolean'"
                      class="form-select"
                      [(ngModel)]="condition.value"
                      (change)="onConditionChange()">
                      <option value="">Seçin</option>
                      <option value="true">Evet</option>
                      <option value="false">Hayır</option>
                    </select>

                    <!-- Range Input -->
                    <div *ngSwitchCase="'range'" class="range-inputs">
                      <input
                        type="number"
                        class="form-control"
                        [(ngModel)]="condition.value?.min"
                        placeholder="Min"
                        (input)="onRangeChange(condition)">
                      <span class="range-separator">-</span>
                      <input
                        type="number"
                        class="form-control"
                        [(ngModel)]="condition.value?.max"
                        placeholder="Max"
                        (input)="onRangeChange(condition)">
                    </div>
                  </ng-container>
                </div>

                <!-- Remove Condition -->
                <button
                  type="button"
                  class="btn btn-sm btn-outline-danger remove-condition"
                  (click)="removeCondition(group.id, condition.id)"
                  title="Koşulu Sil">
                  <lucide-angular [img]="Trash2Icon" size="12"></lucide-angular>
                </button>
              </div>

              <!-- Add Condition in Group -->
              <button
                type="button"
                class="btn btn-sm btn-outline-primary add-condition-in-group"
                (click)="addConditionToGroup(group.id)">
                <lucide-angular [img]="PlusIcon" size="12" class="me-1"></lucide-angular>
                Koşul Ekle
              </button>
            </div>
          </div>

          <!-- Empty State -->
          <div class="empty-conditions" *ngIf="!hasConditions()">
            <lucide-angular [img]="SearchIcon" size="48" class="empty-icon"></lucide-angular>
            <h5>Arama koşulu ekleyin</h5>
            <p>Gelişmiş arama yapmak için yukarıdaki "Koşul Ekle" düğmesini kullanın.</p>
            <button type="button" class="btn btn-primary" (click)="addCondition()">
              <lucide-angular [img]="PlusIcon" size="16" class="me-2"></lucide-angular>
              İlk Koşulu Ekle
            </button>
          </div>
        </div>
      </div>

      <!-- Search Preview -->
      <div class="search-preview" *ngIf="hasConditions()">
        <div class="preview-header">
          <h6>Arama Önizlemesi</h6>
          <div class="preview-actions">
            <button type="button" class="btn btn-sm btn-outline-secondary" (click)="copyQuery()">
              <lucide-angular [img]="CopyIcon" size="12" class="me-1"></lucide-angular>
              Kopyala
            </button>
            <button type="button" class="btn btn-sm btn-outline-secondary" (click)="exportQuery()">
              <lucide-angular [img]="DownloadIcon" size="12" class="me-1"></lucide-angular>
              Dışa Aktar
            </button>
          </div>
        </div>
        <div class="preview-content">
          <div class="query-visualization">
            {{ getQueryPreview() }}
          </div>
          <div class="expected-results" *ngIf="estimatedResultCount() > 0">
            <small class="text-muted">
              Tahmini sonuç: {{ estimatedResultCount() | number }} kayıt
            </small>
          </div>
        </div>
      </div>

      <!-- Search Results -->
      <div class="search-results" *ngIf="searchResults()">
        <div class="results-header">
          <div class="results-info">
            <h5>Arama Sonuçları</h5>
            <div class="results-meta">
              <span class="result-count">{{ searchResults()?.totalCount | number }} sonuç bulundu</span>
              <span class="execution-time">({{ searchResults()?.executionTime }}ms)</span>
            </div>
          </div>

          <div class="results-actions">
            <button type="button" class="btn btn-outline-secondary" (click)="exportResults()">
              <lucide-angular [img]="DownloadIcon" size="16" class="me-2"></lucide-angular>
              Sonuçları Dışa Aktar
            </button>
          </div>
        </div>

        <!-- Facets -->
        <div class="search-facets" *ngIf="searchResults()?.facets?.length">
          <h6>Filtreler</h6>
          <div class="facet-groups">
            <div
              *ngFor="let facet of searchResults()?.facets"
              class="facet-group">
              <h6 class="facet-title">{{ getFieldLabel(facet.field) }}</h6>
              <div class="facet-values">
                <label
                  *ngFor="let value of facet.values.slice(0, 5)"
                  class="facet-value">
                  <input
                    type="checkbox"
                    (change)="toggleFacetValue(facet.field, value.value)">
                  <span class="facet-label">{{ value.label }}</span>
                  <span class="facet-count">({{ value.count }})</span>
                </label>
                <button
                  *ngIf="facet.values.length > 5"
                  type="button"
                  class="btn btn-sm btn-link show-more-facets"
                  (click)="toggleFacetExpansion(facet.field)">
                  {{ isFacetExpanded(facet.field) ? 'Daha az göster' : 'Daha fazla göster' }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Results List -->
        <div class="results-content">
          <div
            *ngFor="let item of searchResults()?.items; trackBy: trackByItemId"
            class="result-item">
            <div class="result-header">
              <h6 class="result-title">{{ getResultTitle(item) }}</h6>
              <div class="result-actions">
                <button type="button" class="btn btn-sm btn-outline-primary" (click)="viewItem(item)">
                  <lucide-angular [img]="EyeIcon" size="12" class="me-1"></lucide-angular>
                  Görüntüle
                </button>
              </div>
            </div>
            <div class="result-content">
              <div class="result-fields">
                <div
                  *ngFor="let field of getDisplayFields(item)"
                  class="result-field">
                  <span class="field-label">{{ field.label }}:</span>
                  <span class="field-value">{{ field.value }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Pagination -->
          <div class="results-pagination" *ngIf="totalResultPages() > 1">
            <nav aria-label="Search results pagination">
              <ul class="pagination justify-content-center">
                <li class="page-item" [class.disabled]="currentResultPage() === 1">
                  <a class="page-link" (click)="goToResultPage(currentResultPage() - 1)">Önceki</a>
                </li>

                <li
                  *ngFor="let page of visibleResultPages()"
                  class="page-item"
                  [class.active]="page === currentResultPage()">
                  <a class="page-link" (click)="goToResultPage(page)">{{ page }}</a>
                </li>

                <li class="page-item" [class.disabled]="currentResultPage() === totalResultPages()">
                  <a class="page-link" (click)="goToResultPage(currentResultPage() + 1)">Sonraki</a>
                </li>
              </ul>
            </nav>
          </div>
        </div>
      </div>
    </div>

    <!-- Template Manager Modal -->
    <div class="modal fade" id="templateManagerModal" tabindex="-1">
      <div class="modal-dialog modal-xl">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Arama Şablonları</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>

          <div class="modal-body">
            <div class="template-manager">
              <div class="template-actions">
                <button type="button" class="btn btn-primary" (click)="createNewTemplate()">
                  <lucide-angular [img]="PlusIcon" size="16" class="me-2"></lucide-angular>
                  Yeni Şablon
                </button>
              </div>

              <div class="template-list">
                <div
                  *ngFor="let template of searchTemplates(); trackBy: trackByTemplateId"
                  class="template-item">
                  <div class="template-info">
                    <h6 class="template-name">{{ template.name }}</h6>
                    <p class="template-description">{{ template.description }}</p>
                    <div class="template-meta">
                      <span class="template-category">{{ template.category }}</span>
                      <span class="template-usage">{{ template.useCount }} kez kullanıldı</span>
                      <span class="template-date">{{ formatDate(template.createdAt) }}</span>
                    </div>
                    <div class="template-tags">
                      <span
                        *ngFor="let tag of template.tags"
                        class="template-tag">
                        {{ tag }}
                      </span>
                    </div>
                  </div>

                  <div class="template-actions">
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-primary"
                      (click)="loadTemplate(template)">
                      <lucide-angular [img]="TargetIcon" size="14"></lucide-angular>
                      Kullan
                    </button>
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-secondary"
                      (click)="editTemplate(template)">
                      <lucide-angular [img]="EditIcon" size="14"></lucide-angular>
                      Düzenle
                    </button>
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-danger"
                      (click)="deleteTemplate(template)">
                      <lucide-angular [img]="Trash2Icon" size="14"></lucide-angular>
                      Sil
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              Kapat
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Save Template Modal -->
    <div class="modal fade" id="saveTemplateModal" tabindex="-1">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Şablon Olarak Kaydet</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>

          <form [formGroup]="templateForm" (ngSubmit)="saveTemplate()">
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Şablon Adı</label>
                <input type="text" class="form-control" formControlName="name" placeholder="Şablon adı">
              </div>

              <div class="mb-3">
                <label class="form-label">Açıklama</label>
                <textarea class="form-control" formControlName="description" rows="3" placeholder="Şablon açıklaması"></textarea>
              </div>

              <div class="mb-3">
                <label class="form-label">Kategori</label>
                <select class="form-select" formControlName="category">
                  <option value="general">Genel</option>
                  <option value="users">Kullanıcılar</option>
                  <option value="roles">Roller</option>
                  <option value="permissions">İzinler</option>
                  <option value="groups">Gruplar</option>
                  <option value="security">Güvenlik</option>
                </select>
              </div>

              <div class="mb-3">
                <label class="form-label">Etiketler</label>
                <input
                  type="text"
                  class="form-control"
                  formControlName="tags"
                  placeholder="Virgülle ayırarak etiket ekleyin">
              </div>

              <div class="form-check">
                <input class="form-check-input" type="checkbox" formControlName="isPublic" id="isPublic">
                <label class="form-check-label" for="isPublic">
                  Herkese açık yap
                </label>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                İptal
              </button>
              <button
                type="submit"
                class="btn btn-primary"
                [disabled]="templateForm.invalid || savingTemplate()">
                <span class="spinner-border spinner-border-sm me-2" *ngIf="savingTemplate()"></span>
                Kaydet
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .advanced-search-container {
      padding: 1.5rem;
      max-width: 1400px;
      margin: 0 auto;
    }

    .search-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 2rem;
    }

    .title-section {
      flex: 1;
    }

    .title-with-icon {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
    }

    .title-icon {
      color: var(--bs-primary);
      margin-top: 0.25rem;
    }

    .search-title {
      margin: 0;
      font-size: 1.75rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .search-subtitle {
      margin: 0.25rem 0 0 0;
      color: var(--bs-gray-600);
      font-size: 0.95rem;
    }

    .search-actions {
      display: flex;
      gap: 0.75rem;
      align-items: center;
    }

    .quick-filters {
      background: white;
      border-radius: 0.5rem;
      padding: 1.25rem;
      margin-bottom: 1.5rem;
      border: 1px solid var(--bs-gray-200);
    }

    .quick-filters h6 {
      margin-bottom: 1rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .quick-filter-buttons {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .quick-filter-btn {
      display: flex;
      align-items: center;
      transition: all 0.2s ease;
    }

    .search-builder {
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
      margin-bottom: 1.5rem;
    }

    .builder-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 1.25rem;
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .builder-header h5 {
      margin: 0;
      font-weight: 600;
    }

    .builder-actions {
      display: flex;
      gap: 0.5rem;
    }

    .condition-groups {
      padding: 1.25rem;
    }

    .condition-group {
      margin-bottom: 1.5rem;
      padding: 1rem;
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      background: var(--bs-gray-50);
    }

    .condition-group.has-error {
      border-color: var(--bs-danger);
      background: var(--bs-danger-bg);
    }

    .group-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
      padding-bottom: 0.75rem;
      border-bottom: 1px solid var(--bs-gray-300);
    }

    .group-info {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .group-label {
      font-weight: 600;
      color: var(--bs-gray-700);
    }

    .group-operator {
      width: auto;
      min-width: 80px;
    }

    .conditions-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .condition-item {
      display: grid;
      grid-template-columns: auto 1fr 150px 1fr auto;
      gap: 0.75rem;
      align-items: center;
      padding: 1rem;
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
    }

    .condition-item.has-error {
      border-color: var(--bs-danger);
      background: var(--bs-danger-bg);
    }

    .logical-operator {
      width: 80px;
    }

    .field-selection {
      min-width: 200px;
    }

    .operator-selection {
      min-width: 150px;
    }

    .value-input {
      min-width: 200px;
    }

    .remove-condition {
      padding: 0.375rem 0.5rem;
    }

    .multiselect-container {
      width: 100%;
    }

    .selected-values {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-bottom: 0.5rem;
    }

    .selected-value {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      background: var(--bs-primary-bg);
      color: var(--bs-primary);
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.85rem;
    }

    .btn-remove-value {
      background: none;
      border: none;
      color: inherit;
      padding: 0.125rem;
      border-radius: 0.125rem;
    }

    .btn-remove-value:hover {
      background: rgba(0, 0, 0, 0.1);
    }

    .range-inputs {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .range-separator {
      color: var(--bs-gray-500);
      font-weight: 500;
    }

    .add-condition-in-group {
      justify-self: start;
      margin-top: 0.5rem;
    }

    .empty-conditions {
      text-align: center;
      padding: 3rem 2rem;
      color: var(--bs-gray-600);
    }

    .empty-icon {
      color: var(--bs-gray-400);
      margin-bottom: 1.5rem;
    }

    .search-preview {
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
      margin-bottom: 1.5rem;
    }

    .preview-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 1.25rem;
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .preview-header h6 {
      margin: 0;
      font-weight: 600;
    }

    .preview-actions {
      display: flex;
      gap: 0.5rem;
    }

    .preview-content {
      padding: 1.25rem;
    }

    .query-visualization {
      background: var(--bs-gray-50);
      border-radius: 0.25rem;
      padding: 1rem;
      font-family: monospace;
      font-size: 0.9rem;
      color: var(--bs-gray-800);
      margin-bottom: 0.75rem;
      white-space: pre-wrap;
      word-break: break-word;
    }

    .expected-results {
      text-align: right;
    }

    .search-results {
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
    }

    .results-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 1rem 1.25rem;
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .results-info h5 {
      margin: 0 0 0.5rem 0;
      font-weight: 600;
    }

    .results-meta {
      display: flex;
      gap: 1rem;
      font-size: 0.9rem;
      color: var(--bs-gray-600);
    }

    .result-count {
      font-weight: 500;
    }

    .execution-time {
      color: var(--bs-gray-500);
    }

    .search-facets {
      padding: 1.25rem;
      border-bottom: 1px solid var(--bs-gray-200);
      background: var(--bs-gray-50);
    }

    .search-facets h6 {
      margin-bottom: 1rem;
      font-weight: 600;
    }

    .facet-groups {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
    }

    .facet-group {
      background: white;
      border-radius: 0.25rem;
      padding: 1rem;
      border: 1px solid var(--bs-gray-200);
    }

    .facet-title {
      font-size: 0.9rem;
      font-weight: 600;
      margin-bottom: 0.75rem;
      color: var(--bs-gray-900);
    }

    .facet-values {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .facet-value {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.85rem;
      cursor: pointer;
    }

    .facet-label {
      flex: 1;
      color: var(--bs-gray-700);
    }

    .facet-count {
      color: var(--bs-gray-500);
      font-size: 0.8rem;
    }

    .show-more-facets {
      font-size: 0.8rem;
      padding: 0;
      text-align: left;
    }

    .results-content {
      padding: 1.25rem;
    }

    .result-item {
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      margin-bottom: 1rem;
      overflow: hidden;
    }

    .result-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem;
      background: var(--bs-gray-50);
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .result-title {
      margin: 0;
      font-size: 1rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .result-content {
      padding: 1rem;
    }

    .result-fields {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 0.75rem;
    }

    .result-field {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .field-label {
      font-size: 0.8rem;
      font-weight: 500;
      color: var(--bs-gray-600);
      text-transform: uppercase;
    }

    .field-value {
      font-size: 0.9rem;
      color: var(--bs-gray-900);
      word-break: break-word;
    }

    .results-pagination {
      margin-top: 2rem;
      padding-top: 1rem;
      border-top: 1px solid var(--bs-gray-200);
    }

    /* Modal Styles */
    .template-manager {
      max-height: 600px;
      overflow-y: auto;
    }

    .template-actions {
      margin-bottom: 1.5rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .template-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .template-item {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 1rem;
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      background: white;
    }

    .template-info {
      flex: 1;
    }

    .template-name {
      margin: 0 0 0.5rem 0;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .template-description {
      margin: 0 0 0.75rem 0;
      color: var(--bs-gray-600);
      font-size: 0.9rem;
    }

    .template-meta {
      display: flex;
      gap: 1rem;
      margin-bottom: 0.75rem;
      font-size: 0.8rem;
      color: var(--bs-gray-500);
    }

    .template-tags {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .template-tag {
      background: var(--bs-gray-100);
      color: var(--bs-gray-700);
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.75rem;
    }

    .template-actions {
      display: flex;
      gap: 0.5rem;
      flex-shrink: 0;
    }

    .dropdown-submenu {
      position: relative;
    }

    .dropdown-submenu .dropdown-menu {
      position: absolute;
      top: 0;
      left: 100%;
      margin-left: 0.125rem;
      display: none;
    }

    .dropdown-submenu .dropdown-menu.show {
      display: block;
    }

    .dropdown-item-template {
      max-width: 300px;
    }

    .dropdown-item-template .dropdown-item {
      white-space: normal;
      word-wrap: break-word;
    }

    @media (max-width: 768px) {
      .advanced-search-container {
        padding: 1rem;
      }

      .search-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .search-actions {
        justify-content: space-between;
      }

      .quick-filter-buttons {
        justify-content: center;
      }

      .condition-item {
        grid-template-columns: 1fr;
        gap: 1rem;
      }

      .logical-operator {
        width: 100%;
      }

      .builder-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .builder-actions {
        justify-content: space-between;
      }

      .preview-header,
      .results-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .facet-groups {
        grid-template-columns: 1fr;
      }

      .result-fields {
        grid-template-columns: 1fr;
      }

      .template-item {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .template-actions {
        justify-content: center;
      }
    }
  `]
})
export class AdvancedSearchFiltersComponent implements OnInit, OnDestroy {
  // Dependency Injection
  private readonly userService = inject(UserService);
  private readonly roleService = inject(RoleService);
  private readonly groupService = inject(GroupService);
  private readonly permissionService = inject(PermissionService);
  private readonly fb = inject(FormBuilder);

  // Icons
  readonly SearchIcon = Search;
  readonly FilterIcon = Filter;
  readonly XIcon = X;
  readonly PlusIcon = Plus;
  readonly MinusIcon = Minus;
  readonly SettingsIcon = Settings;
  readonly SaveIcon = Save;
  readonly RefreshCwIcon = RefreshCw;
  readonly DownloadIcon = Download;
  readonly CalendarIcon = Calendar;
  readonly ClockIcon = Clock;
  readonly UserIcon = User;
  readonly ShieldIcon = Shield;
  readonly DatabaseIcon = Database;
  readonly ChevronDownIcon = ChevronDown;
  readonly ChevronRightIcon = ChevronRight;
  readonly AlertTriangleIcon = AlertTriangle;
  readonly CheckCircleIcon = CheckCircle;
  readonly CopyIcon = Copy;
  readonly Trash2Icon = Trash2;
  readonly EditIcon = Edit;
  readonly EyeIcon = Eye;
  readonly MoreVerticalIcon = MoreVertical;
  readonly TargetIcon = Target;
  readonly LayersIcon = Layers;
  readonly TagIcon = Tag;

  // Input/Output
  @Input() searchType: 'users' | 'roles' | 'permissions' | 'groups' | 'all' = 'all';
  @Output() searchExecuted = new EventEmitter<SearchResult>();
  @Output() conditionsChanged = new EventEmitter<SearchCondition[]>();

  // State Signals
  conditionGroups = signal<{ id: string; logicalOperator: 'AND' | 'OR'; conditions: SearchCondition[] }[]>([]);
  searchResults = signal<SearchResult | null>(null);
  searchTemplates = signal<SearchTemplate[]>([]);
  searchFields = signal<SearchField[]>([]);
  quickFilters = signal<QuickFilter[]>([]);
  searching = signal(false);
  savingTemplate = signal(false);
  estimatedResultCount = signal(0);

  // UI State
  expandedCategories = signal<Set<string>>(new Set());
  expandedFacets = signal<Set<string>>(new Set());
  currentResultPage = signal(1);
  resultPageSize = signal(20);

  // Forms
  templateForm: FormGroup;

  // Data Sources
  searchOperators = signal<SearchOperator[]>([
    { key: 'equals', label: 'Eşittir', supportedTypes: ['string', 'number', 'boolean'], requiresValue: true },
    { key: 'not_equals', label: 'Eşit Değildir', supportedTypes: ['string', 'number', 'boolean'], requiresValue: true },
    { key: 'contains', label: 'İçerir', supportedTypes: ['string'], requiresValue: true },
    { key: 'not_contains', label: 'İçermez', supportedTypes: ['string'], requiresValue: true },
    { key: 'starts_with', label: 'İle Başlar', supportedTypes: ['string'], requiresValue: true },
    { key: 'ends_with', label: 'İle Biter', supportedTypes: ['string'], requiresValue: true },
    { key: 'is_empty', label: 'Boş', supportedTypes: ['string'], requiresValue: false },
    { key: 'is_not_empty', label: 'Boş Değil', supportedTypes: ['string'], requiresValue: false },
    { key: 'greater_than', label: 'Büyüktür', supportedTypes: ['number', 'date'], requiresValue: true },
    { key: 'greater_than_or_equal', label: 'Büyük Eşittir', supportedTypes: ['number', 'date'], requiresValue: true },
    { key: 'less_than', label: 'Küçüktür', supportedTypes: ['number', 'date'], requiresValue: true },
    { key: 'less_than_or_equal', label: 'Küçük Eşittir', supportedTypes: ['number', 'date'], requiresValue: true },
    { key: 'between', label: 'Arasında', supportedTypes: ['number', 'date'], requiresValue: true },
    { key: 'in', label: 'İçinde', supportedTypes: ['string', 'number'], requiresValue: true, multipleValues: true },
    { key: 'not_in', label: 'İçinde Değil', supportedTypes: ['string', 'number'], requiresValue: true, multipleValues: true }
  ]);

  fieldCategories = signal([
    { key: 'user', label: 'Kullanıcı Bilgileri' },
    { key: 'role', label: 'Roller' },
    { key: 'permission', label: 'İzinler' },
    { key: 'group', label: 'Gruplar' },
    { key: 'system', label: 'Sistem' },
    { key: 'audit', label: 'Denetim' }
  ]);

  // Computed Values
  hasConditions = computed(() => {
    return this.conditionGroups().some(group => group.conditions.length > 0);
  });

  hasValidConditions = computed(() => {
    return this.conditionGroups().some(group =>
      group.conditions.some(condition =>
        condition.field && condition.operator && (
          !this.shouldShowValueInput(condition) ||
          (condition.value !== null && condition.value !== undefined && condition.value !== '')
        )
      )
    );
  });

  totalResultPages = computed(() => {
    const total = this.searchResults()?.totalCount || 0;
    return Math.ceil(total / this.resultPageSize());
  });

  visibleResultPages = computed(() => {
    const current = this.currentResultPage();
    const total = this.totalResultPages();
    const pages: number[] = [];

    let start = Math.max(1, current - 2);
    let end = Math.min(total, current + 2);

    if (end - start < 4) {
      if (start === 1) {
        end = Math.min(total, 5);
      } else if (end === total) {
        start = Math.max(1, total - 4);
      }
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  });

  recentTemplates = computed(() => {
    return this.searchTemplates()
      .filter(t => t.lastUsed)
      .sort((a, b) => (b.lastUsed?.getTime() || 0) - (a.lastUsed?.getTime() || 0))
      .slice(0, 5);
  });

  constructor() {
    this.templateForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      category: ['general', Validators.required],
      tags: [''],
      isPublic: [false]
    });

    // Initialize with first condition group
    this.conditionGroups.set([
      {
        id: this.generateId(),
        logicalOperator: 'AND',
        conditions: []
      }
    ]);
  }

  ngOnInit(): void {
    this.initializeSearchFields();
    this.initializeQuickFilters();
    this.loadSearchTemplates();
  }

  ngOnDestroy(): void {
    // Cleanup subscriptions
  }

  // Initialization
  private initializeSearchFields(): void {
    const fields: SearchField[] = [
      // User fields
      {
        key: 'user.fullName',
        label: 'Ad Soyad',
        type: 'text',
        dataType: 'string',
        placeholder: 'Kullanıcı adı girin',
        description: 'Kullanıcının tam adı',
        category: 'user'
      },
      {
        key: 'user.email',
        label: 'E-posta',
        type: 'text',
        dataType: 'string',
        placeholder: 'E-posta adresi girin',
        description: 'Kullanıcının e-posta adresi',
        category: 'user'
      },
      {
        key: 'user.isActive',
        label: 'Aktif Durum',
        type: 'boolean',
        dataType: 'boolean',
        description: 'Kullanıcının aktif olup olmadığı',
        category: 'user'
      },
      {
        key: 'user.createdAt',
        label: 'Oluşturulma Tarihi',
        type: 'date',
        dataType: 'date',
        description: 'Kullanıcının oluşturulma tarihi',
        category: 'user'
      },
      {
        key: 'user.lastLoginAt',
        label: 'Son Giriş',
        type: 'date',
        dataType: 'date',
        description: 'Kullanıcının son giriş tarihi',
        category: 'user'
      },
      {
        key: 'user.department',
        label: 'Departman',
        type: 'select',
        dataType: 'string',
        options: [
          { value: 'IT', label: 'Bilgi İşlem' },
          { value: 'HR', label: 'İnsan Kaynakları' },
          { value: 'Finance', label: 'Finans' },
          { value: 'Marketing', label: 'Pazarlama' }
        ],
        description: 'Kullanıcının departmanı',
        category: 'user'
      },

      // Role fields
      {
        key: 'role.name',
        label: 'Rol Adı',
        type: 'text',
        dataType: 'string',
        placeholder: 'Rol adı girin',
        description: 'Rolün adı',
        category: 'role'
      },
      {
        key: 'role.isSystemRole',
        label: 'Sistem Rolü',
        type: 'boolean',
        dataType: 'boolean',
        description: 'Sistem rolü olup olmadığı',
        category: 'role'
      },
      {
        key: 'role.userCount',
        label: 'Kullanıcı Sayısı',
        type: 'range',
        dataType: 'number',
        description: 'Role atanmış kullanıcı sayısı',
        category: 'role'
      },

      // Group fields
      {
        key: 'group.name',
        label: 'Grup Adı',
        type: 'text',
        dataType: 'string',
        placeholder: 'Grup adı girin',
        description: 'Grubun adı',
        category: 'group'
      },
      {
        key: 'group.memberCount',
        label: 'Üye Sayısı',
        type: 'range',
        dataType: 'number',
        description: 'Gruptaki üye sayısı',
        category: 'group'
      },

      // Permission fields
      {
        key: 'permission.name',
        label: 'İzin Adı',
        type: 'text',
        dataType: 'string',
        placeholder: 'İzin adı girin',
        description: 'İznin adı',
        category: 'permission'
      },
      {
        key: 'permission.service',
        label: 'Servis',
        type: 'select',
        dataType: 'string',
        options: [
          { value: 'Identity', label: 'Kimlik Yönetimi' },
          { value: 'UserManagement', label: 'Kullanıcı Yönetimi' },
          { value: 'System', label: 'Sistem' }
        ],
        description: 'İznin ait olduğu servis',
        category: 'permission'
      }
    ];

    this.searchFields.set(fields);
  }

  private initializeQuickFilters(): void {
    const filters: QuickFilter[] = [
      {
        key: 'active_users',
        label: 'Aktif Kullanıcılar',
        icon: UserIcon,
        color: 'success',
        conditions: [
          {
            id: this.generateId(),
            field: 'user.isActive',
            operator: 'equals',
            value: true
          }
        ]
      },
      {
        key: 'recent_users',
        label: 'Son 30 Gün',
        icon: ClockIcon,
        color: 'info',
        conditions: [
          {
            id: this.generateId(),
            field: 'user.createdAt',
            operator: 'greater_than',
            value: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
          }
        ]
      },
      {
        key: 'system_roles',
        label: 'Sistem Rolleri',
        icon: ShieldIcon,
        color: 'warning',
        conditions: [
          {
            id: this.generateId(),
            field: 'role.isSystemRole',
            operator: 'equals',
            value: true
          }
        ]
      }
    ];

    this.quickFilters.set(filters);
  }

  private loadSearchTemplates(): void {
    // Mock templates for now
    const templates: SearchTemplate[] = [
      {
        id: '1',
        name: 'Aktif IT Kullanıcıları',
        description: 'IT departmanındaki aktif kullanıcılar',
        conditions: [
          {
            id: this.generateId(),
            field: 'user.isActive',
            operator: 'equals',
            value: true
          },
          {
            id: this.generateId(),
            field: 'user.department',
            operator: 'equals',
            value: 'IT',
            logicalOperator: 'AND'
          }
        ],
        createdAt: new Date(),
        createdBy: 'admin',
        isPublic: true,
        category: 'users',
        tags: ['aktif', 'IT', 'kullanıcı'],
        useCount: 15,
        lastUsed: new Date()
      }
    ];

    this.searchTemplates.set(templates);
  }

  // Field Methods
  getFieldsByCategory(category: string): SearchField[] {
    return this.searchFields().filter(field => field.category === category);
  }

  getField(fieldKey: string): SearchField | undefined {
    return this.searchFields().find(field => field.key === fieldKey);
  }

  getFieldLabel(fieldKey: string): string {
    const field = this.getField(fieldKey);
    return field?.label || fieldKey;
  }

  getFieldInputType(fieldKey: string): string {
    const field = this.getField(fieldKey);
    return field?.type || 'text';
  }

  getFieldPlaceholder(fieldKey: string): string {
    const field = this.getField(fieldKey);
    return field?.placeholder || '';
  }

  getFieldOptions(fieldKey: string): SearchOption[] {
    const field = this.getField(fieldKey);
    return field?.options || [];
  }

  getAvailableOperators(fieldKey: string): SearchOperator[] {
    const field = this.getField(fieldKey);
    if (!field) return [];

    return this.searchOperators().filter(operator =>
      operator.supportedTypes.includes(field.dataType)
    );
  }

  // Condition Management
  addCondition(fieldKey?: string): void {
    const firstGroup = this.conditionGroups()[0];
    if (firstGroup) {
      this.addConditionToGroup(firstGroup.id, fieldKey);
    }
  }

  addConditionToGroup(groupId: string, fieldKey?: string): void {
    const groups = this.conditionGroups();
    const groupIndex = groups.findIndex(g => g.id === groupId);

    if (groupIndex >= 0) {
      const newCondition: SearchCondition = {
        id: this.generateId(),
        field: fieldKey || '',
        operator: '',
        value: null,
        logicalOperator: groups[groupIndex].conditions.length > 0 ? 'AND' : undefined
      };

      groups[groupIndex].conditions.push(newCondition);
      this.conditionGroups.set([...groups]);
      this.onConditionChange();
    }
  }

  removeCondition(groupId: string, conditionId: string): void {
    const groups = this.conditionGroups();
    const groupIndex = groups.findIndex(g => g.id === groupId);

    if (groupIndex >= 0) {
      const conditions = groups[groupIndex].conditions.filter(c => c.id !== conditionId);

      // Fix logical operators after removal
      if (conditions.length > 0 && conditions[0].logicalOperator) {
        conditions[0].logicalOperator = undefined;
      }

      groups[groupIndex].conditions = conditions;
      this.conditionGroups.set([...groups]);
      this.onConditionChange();
    }
  }

  addConditionGroup(): void {
    const groups = this.conditionGroups();
    const newGroup = {
      id: this.generateId(),
      logicalOperator: 'AND' as const,
      conditions: []
    };

    this.conditionGroups.set([...groups, newGroup]);
  }

  removeConditionGroup(groupId: string): void {
    const groups = this.conditionGroups().filter(g => g.id !== groupId);
    this.conditionGroups.set(groups);
    this.onConditionChange();
  }

  clearAllConditions(): void {
    this.conditionGroups.set([
      {
        id: this.generateId(),
        logicalOperator: 'AND',
        conditions: []
      }
    ]);
    this.searchResults.set(null);
    this.onConditionChange();
  }

  // Event Handlers
  onFieldChange(condition: SearchCondition): void {
    condition.operator = '';
    condition.value = null;
    this.onConditionChange();
  }

  onOperatorChange(condition: SearchCondition): void {
    const operator = this.searchOperators().find(op => op.key === condition.operator);
    if (operator && !operator.requiresValue) {
      condition.value = null;
    } else if (operator?.multipleValues) {
      condition.value = [];
    } else {
      condition.value = null;
    }
    this.onConditionChange();
  }

  onConditionChange(): void {
    const allConditions = this.conditionGroups().flatMap(group => group.conditions);
    this.conditionsChanged.emit(allConditions);
    this.updateEstimatedResults();
  }

  onGroupOperatorChange(): void {
    this.onConditionChange();
  }

  onRangeChange(condition: SearchCondition): void {
    if (!condition.value) {
      condition.value = {};
    }
    this.onConditionChange();
  }

  // Multi-select handling
  addMultiSelectValue(condition: SearchCondition, event: Event): void {
    const target = event.target as HTMLSelectElement;
    const value = target.value;

    if (value && value !== '') {
      if (!condition.value) {
        condition.value = [];
      }

      if (!condition.value.includes(value)) {
        condition.value.push(value);
        this.onConditionChange();
      }

      target.value = '';
    }
  }

  removeMultiSelectValue(condition: SearchCondition, value: any): void {
    if (condition.value && Array.isArray(condition.value)) {
      condition.value = condition.value.filter(v => v !== value);
      this.onConditionChange();
    }
  }

  getAvailableMultiSelectOptions(condition: SearchCondition): SearchOption[] {
    const allOptions = this.getFieldOptions(condition.field);
    const selectedValues = condition.value || [];

    return allOptions.filter(option => !selectedValues.includes(option.value));
  }

  getOptionLabel(fieldKey: string, value: any): string {
    const options = this.getFieldOptions(fieldKey);
    const option = options.find(opt => opt.value === value);
    return option?.label || String(value);
  }

  // Validation
  shouldShowValueInput(condition: SearchCondition): boolean {
    const operator = this.searchOperators().find(op => op.key === condition.operator);
    return operator?.requiresValue || false;
  }

  hasConditionError(condition: SearchCondition): boolean {
    if (!condition.field || !condition.operator) return true;
    if (this.shouldShowValueInput(condition) && (condition.value === null || condition.value === undefined || condition.value === '')) {
      return true;
    }
    return false;
  }

  hasGroupError(group: { conditions: SearchCondition[] }): boolean {
    return group.conditions.some(condition => this.hasConditionError(condition));
  }

  // Category Management
  toggleCategory(category: { key: string }): void {
    const expanded = this.expandedCategories();
    if (expanded.has(category.key)) {
      expanded.delete(category.key);
    } else {
      expanded.add(category.key);
    }
    this.expandedCategories.set(new Set(expanded));
  }

  isCategoryExpanded(categoryKey: string): boolean {
    return this.expandedCategories().has(categoryKey);
  }

  // Quick Filters
  applyQuickFilter(filter: QuickFilter): void {
    this.clearAllConditions();

    const firstGroup = this.conditionGroups()[0];
    if (firstGroup) {
      firstGroup.conditions = [...filter.conditions];
      this.conditionGroups.set([...this.conditionGroups()]);
      this.onConditionChange();
    }
  }

  // Search Execution
  async executeSearch(): Promise<void> {
    if (!this.hasValidConditions()) return;

    try {
      this.searching.set(true);

      // Build search query from conditions
      const query = this.buildSearchQuery();

      // Mock search execution
      const mockResults: SearchResult = {
        totalCount: 150,
        items: this.generateMockResults(),
        facets: [
          {
            field: 'user.department',
            values: [
              { value: 'IT', count: 45, label: 'Bilgi İşlem' },
              { value: 'HR', count: 32, label: 'İnsan Kaynakları' },
              { value: 'Finance', count: 28, label: 'Finans' }
            ]
          }
        ],
        suggestions: ['Aktif kullanıcılar', 'IT departmanı', 'Son 30 gün'],
        executionTime: Math.floor(Math.random() * 100) + 50
      };

      this.searchResults.set(mockResults);
      this.currentResultPage.set(1);
      this.searchExecuted.emit(mockResults);

    } catch (error) {
      console.error('Search execution failed:', error);
    } finally {
      this.searching.set(false);
    }
  }

  private buildSearchQuery(): any {
    const groups = this.conditionGroups();
    // Build query object from conditions
    return {
      groups: groups.map(group => ({
        logicalOperator: group.logicalOperator,
        conditions: group.conditions.filter(c => !this.hasConditionError(c))
      }))
    };
  }

  private generateMockResults(): any[] {
    const results = [];
    for (let i = 0; i < 20; i++) {
      results.push({
        id: `item-${i}`,
        type: 'user',
        title: `Kullanıcı ${i + 1}`,
        fullName: `John Doe ${i + 1}`,
        email: `user${i + 1}@example.com`,
        department: ['IT', 'HR', 'Finance'][i % 3],
        isActive: Math.random() > 0.2,
        createdAt: new Date(Date.now() - Math.random() * 365 * 24 * 60 * 60 * 1000)
      });
    }
    return results;
  }

  private updateEstimatedResults(): void {
    // Mock estimation logic
    const conditionsCount = this.conditionGroups().reduce((sum, group) => sum + group.conditions.length, 0);
    const estimated = Math.max(0, 1000 - (conditionsCount * 100) + Math.floor(Math.random() * 200));
    this.estimatedResultCount.set(estimated);
  }

  // Search Results
  getResultTitle(item: any): string {
    return item.title || item.fullName || item.name || `Item ${item.id}`;
  }

  getDisplayFields(item: any): { label: string; value: any }[] {
    return [
      { label: 'E-posta', value: item.email },
      { label: 'Departman', value: item.department },
      { label: 'Durum', value: item.isActive ? 'Aktif' : 'Pasif' },
      { label: 'Oluşturulma', value: this.formatDate(item.createdAt) }
    ];
  }

  viewItem(item: any): void {
    // Navigate to item details
  }

  goToResultPage(page: number): void {
    if (page >= 1 && page <= this.totalResultPages()) {
      this.currentResultPage.set(page);
      // Re-execute search with new page
    }
  }

  // Facets
  toggleFacetValue(field: string, value: any): void {
    // Add facet filter to conditions
  }

  toggleFacetExpansion(field: string): void {
    const expanded = this.expandedFacets();
    if (expanded.has(field)) {
      expanded.delete(field);
    } else {
      expanded.add(field);
    }
    this.expandedFacets.set(new Set(expanded));
  }

  isFacetExpanded(field: string): boolean {
    return this.expandedFacets().has(field);
  }

  // Export/Import
  getQueryPreview(): string {
    const groups = this.conditionGroups();
    const lines: string[] = [];

    groups.forEach((group, groupIndex) => {
      if (groupIndex > 0) {
        lines.push(`${group.logicalOperator}`);
      }

      if (groups.length > 1) {
        lines.push('(');
      }

      group.conditions.forEach((condition, conditionIndex) => {
        if (conditionIndex > 0 && condition.logicalOperator) {
          lines.push(`  ${condition.logicalOperator}`);
        }

        const field = this.getFieldLabel(condition.field);
        const operator = this.searchOperators().find(op => op.key === condition.operator)?.label || condition.operator;
        const value = this.formatConditionValue(condition);

        lines.push(`  ${field} ${operator} ${value}`);
      });

      if (groups.length > 1) {
        lines.push(')');
      }
    });

    return lines.join('\n');
  }

  private formatConditionValue(condition: SearchCondition): string {
    if (!this.shouldShowValueInput(condition)) return '';

    if (Array.isArray(condition.value)) {
      return `[${condition.value.map(v => this.getOptionLabel(condition.field, v)).join(', ')}]`;
    }

    if (typeof condition.value === 'object' && condition.value?.min !== undefined && condition.value?.max !== undefined) {
      return `${condition.value.min} - ${condition.value.max}`;
    }

    const field = this.getField(condition.field);
    if (field?.type === 'select') {
      return this.getOptionLabel(condition.field, condition.value);
    }

    return String(condition.value || '');
  }

  copyQuery(): void {
    const queryText = this.getQueryPreview();
    navigator.clipboard.writeText(queryText);
  }

  exportQuery(): void {
    const query = this.buildSearchQuery();
    const blob = new Blob([JSON.stringify(query, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `search-query-${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }

  exportResults(): void {
    const results = this.searchResults();
    if (!results) return;

    // Export search results as Excel/CSV
  }

  // Template Management
  openTemplateManager(): void {
    // Open template manager modal
  }

  saveAsTemplate(): void {
    if (!this.hasValidConditions()) return;
    // Open save template modal
  }

  createNewTemplate(): void {
    this.templateForm.reset({ category: 'general', isPublic: false });
    // Open template creation modal
  }

  async saveTemplate(): Promise<void> {
    if (this.templateForm.invalid || !this.hasValidConditions()) return;

    try {
      this.savingTemplate.set(true);
      const formValue = this.templateForm.value;

      const template: SearchTemplate = {
        id: this.generateId(),
        name: formValue.name,
        description: formValue.description,
        conditions: this.conditionGroups().flatMap(group => group.conditions),
        createdAt: new Date(),
        createdBy: 'current-user',
        isPublic: formValue.isPublic,
        category: formValue.category,
        tags: formValue.tags ? formValue.tags.split(',').map((tag: string) => tag.trim()) : [],
        useCount: 0
      };

      const currentTemplates = this.searchTemplates();
      this.searchTemplates.set([...currentTemplates, template]);

      // Close modal and reset form
      this.templateForm.reset();

    } catch (error) {
      console.error('Template save failed:', error);
    } finally {
      this.savingTemplate.set(false);
    }
  }

  loadTemplate(template: SearchTemplate): void {
    this.clearAllConditions();

    // Group conditions by their group property or create single group
    const groups = new Map<string, SearchCondition[]>();

    template.conditions.forEach(condition => {
      const groupKey = condition.group || 'default';
      if (!groups.has(groupKey)) {
        groups.set(groupKey, []);
      }
      groups.get(groupKey)!.push({ ...condition, id: this.generateId() });
    });

    const conditionGroups = Array.from(groups.entries()).map(([groupKey, conditions]) => ({
      id: this.generateId(),
      logicalOperator: 'AND' as const,
      conditions
    }));

    this.conditionGroups.set(conditionGroups);

    // Update usage statistics
    template.useCount++;
    template.lastUsed = new Date();

    this.onConditionChange();
  }

  editTemplate(template: SearchTemplate): void {
    this.templateForm.patchValue({
      name: template.name,
      description: template.description,
      category: template.category,
      tags: template.tags.join(', '),
      isPublic: template.isPublic
    });
    // Open edit modal
  }

  deleteTemplate(template: SearchTemplate): void {
    const currentTemplates = this.searchTemplates();
    this.searchTemplates.set(currentTemplates.filter(t => t.id !== template.id));
  }

  // Utility Methods
  private generateId(): string {
    return Math.random().toString(36).substr(2, 9);
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(new Date(date));
  }

  trackByGroupId(index: number, group: any): string {
    return group.id;
  }

  trackByConditionId(index: number, condition: SearchCondition): string {
    return condition.id;
  }

  trackByTemplateId(index: number, template: SearchTemplate): string {
    return template.id;
  }

  trackByItemId(index: number, item: any): string {
    return item.id;
  }
}