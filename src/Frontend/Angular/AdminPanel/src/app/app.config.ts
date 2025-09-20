import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { provideToastr } from 'ngx-toastr';

import { routes } from './app.routes';
import { jwtInterceptor } from './core/auth/interceptors/jwt.interceptor';
import { errorInterceptor } from './core/api/interceptors/error.interceptor';
import { loadingInterceptor } from './core/api/interceptors/loading.interceptor';
import { reducers, AuthEffects } from './store';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([
        jwtInterceptor,
        errorInterceptor,
        loadingInterceptor
      ])
    ),
    provideAnimations(),
    // Enhanced toastr configuration
    provideToastr({
      timeOut: 5000,
      positionClass: 'toast-top-right',
      preventDuplicates: true,
      closeButton: true,
      progressBar: true,
      extendedTimeOut: 2000,
      enableHtml: true,
      tapToDismiss: true,
      maxOpened: 5,
      autoDismiss: true,
      newestOnTop: true
    }),
    provideStore(reducers),
    provideEffects([AuthEffects]),
    provideStoreDevtools({
      maxAge: 25,
      logOnly: environment.production,
      autoPause: true
    })
  ]
};
