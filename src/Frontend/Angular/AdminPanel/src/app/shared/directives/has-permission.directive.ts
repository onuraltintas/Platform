import {
  Directive,
  Input,
  TemplateRef,
  ViewContainerRef,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectorRef
} from '@angular/core';
import { Subscription } from 'rxjs';
import { PermissionService } from '../../core/services/permission.service';

@Directive({
  selector: '[hasPermission]',
  standalone: true
})
export class HasPermissionDirective implements OnInit, OnDestroy {
  private readonly permissionService = inject(PermissionService);
  private readonly templateRef = inject(TemplateRef<any>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly cdr = inject(ChangeDetectorRef);

  private subscription?: Subscription;
  private isViewCreated = false;

  @Input() hasPermission!: string;
  @Input() hasPermissionElse?: TemplateRef<any>;

  ngOnInit(): void {
    this.updateView();

    // Subscribe to permission changes
    this.subscription = this.permissionService.permissions$.subscribe(() => {
      this.updateView();
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  private updateView(): void {
    const hasAccess = this.permissionService.canAccessWithWildcard(this.hasPermission);

    if (hasAccess && !this.isViewCreated) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.isViewCreated = true;
    } else if (!hasAccess && this.isViewCreated) {
      this.viewContainer.clear();
      this.isViewCreated = false;

      // Show else template if provided
      if (this.hasPermissionElse) {
        this.viewContainer.createEmbeddedView(this.hasPermissionElse);
      }
    } else if (!hasAccess && !this.isViewCreated && this.hasPermissionElse) {
      this.viewContainer.createEmbeddedView(this.hasPermissionElse);
    }

    this.cdr.markForCheck();
  }
}