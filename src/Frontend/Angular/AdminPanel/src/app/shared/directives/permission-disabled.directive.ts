import {
  Directive,
  ElementRef,
  Input,
  OnInit,
  OnDestroy,
  inject,
  Renderer2
} from '@angular/core';
import { Subscription } from 'rxjs';
import { PermissionService } from '../../core/services/permission.service';

@Directive({
  selector: '[permissionDisabled]',
  standalone: true
})
export class PermissionDisabledDirective implements OnInit, OnDestroy {
  private readonly permissionService = inject(PermissionService);
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);

  private subscription?: Subscription;

  @Input() permissionDisabled!: string;
  @Input() disabledClass = 'permission-disabled';
  @Input() disabledTitle = 'You do not have permission to perform this action';

  ngOnInit(): void {
    this.updateElementState();

    // Subscribe to permission changes
    this.subscription = this.permissionService.permissions$.subscribe(() => {
      this.updateElementState();
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  private updateElementState(): void {
    const hasAccess = this.permissionService.canAccessWithWildcard(this.permissionDisabled);

    if (!hasAccess) {
      // Disable the element
      this.renderer.setAttribute(this.el.nativeElement, 'disabled', 'true');
      this.renderer.addClass(this.el.nativeElement, this.disabledClass);
      this.renderer.setAttribute(this.el.nativeElement, 'title', this.disabledTitle);

      // Prevent click events
      this.renderer.listen(this.el.nativeElement, 'click', (event) => {
        event.preventDefault();
        event.stopPropagation();
        return false;
      });
    } else {
      // Enable the element
      this.renderer.removeAttribute(this.el.nativeElement, 'disabled');
      this.renderer.removeClass(this.el.nativeElement, this.disabledClass);
      this.renderer.removeAttribute(this.el.nativeElement, 'title');
    }
  }
}