import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { GroupContextService } from '../../services/group-context.service';

export const groupInterceptor: HttpInterceptorFn = (req, next) => {
  const groupContext = inject(GroupContextService);
  const groupId = groupContext.groupId;

  if (groupId) {
    const modified = req.clone({ setHeaders: { 'X-Group-Id': groupId } });
    return next(modified);
  }

  return next(req);
};

