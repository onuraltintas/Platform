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
  selector: '[hasRole]',
  standalone: true
})
export class HasRoleDirective implements OnInit, OnDestroy {
  private readonly permissionService = inject(PermissionService);
  private readonly templateRef = inject(TemplateRef<any>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly cdr = inject(ChangeDetectorRef);

  private subscription?: Subscription;
  private isViewCreated = false;

  @Input() hasRole!: string;
  @Input() hasRoleElse?: TemplateRef<any>;

  ngOnInit(): void {
    this.updateView();

    // Subscribe to auth changes
    this.subscription = this.permissionService.hasRole(this.hasRole).subscribe(() => {
      this.updateView();
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  private updateView(): void {
    const hasAccess = this.permissionService.isInRole(this.hasRole);

    if (hasAccess && !this.isViewCreated) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.isViewCreated = true;
    } else if (!hasAccess && this.isViewCreated) {
      this.viewContainer.clear();
      this.isViewCreated = false;

      // Show else template if provided
      if (this.hasRoleElse) {
        this.viewContainer.createEmbeddedView(this.hasRoleElse);
      }
    } else if (!hasAccess && !this.isViewCreated && this.hasRoleElse) {
      this.viewContainer.createEmbeddedView(this.hasRoleElse);
    }

    this.cdr.markForCheck();
  }
}