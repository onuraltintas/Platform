import { ApplicationConfig, ErrorHandler, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { PreloadAllModules, provideRouter, withPreloading } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { provideHttpClient, withInterceptors, withInterceptorsFromDi } from '@angular/common/http';
import { routes } from './app.routes';
import { TokenInterceptor } from './interceptors/token.interceptor';
import { GlobalErrorHandler } from './core/global-error-handler';
import { AuthorizationPolicyService, preloadAuthorizationPolicies } from './services/authorization-policy.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withPreloading(PreloadAllModules)),
    provideAnimations(),
    provideToastr({
      timeOut: 3000,
      positionClass: 'toast-top-right',
      preventDuplicates: true,
    }),
    provideHttpClient(withInterceptorsFromDi(), withInterceptors([TokenInterceptor])),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    { provide: APP_INITIALIZER, useFactory: preloadAuthorizationPolicies, deps: [AuthorizationPolicyService], multi: true },
  ]
};
