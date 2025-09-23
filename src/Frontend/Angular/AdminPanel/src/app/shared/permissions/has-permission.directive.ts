import { Directive, Input, TemplateRef, ViewContainerRef, inject } from '@angular/core';
import { PermissionsService } from './permissions.service';

@Directive({
  selector: '[appHasPermission]'
})
export class HasPermissionDirective {
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private readonly perms = inject(PermissionsService);

  private required: string[] = [];
  private modeAll = false; // false => any, true => all

  @Input()
  set appHasPermission(value: string | string[]) {
    this.required = Array.isArray(value) ? value : [value];
    this.updateView();
  }

  @Input()
  set appHasPermissionAll(value: boolean) {
    this.modeAll = !!value;
    this.updateView();
  }

  private updateView(): void {
    const canShow = this.required.length === 0
      ? true
      : this.modeAll
        ? this.perms.hasAll(this.required)
        : this.perms.hasAny(this.required);

    this.vcr.clear();
    if (canShow) {
      this.vcr.createEmbeddedView(this.tpl);
    }
  }
}

