import {
  HttpInterceptorFn,
  HttpErrorResponse,
  HttpRequest,
  HttpHandlerFn,
  HttpEvent
} from '@angular/common/http';
import { inject } from '@angular/core';
import {
  catchError,
  switchMap,
  filter,
  take
} from 'rxjs/operators';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { AuthService } from '../../../features/access-control/data-access/auth.service';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';

let isRefreshing = false;
let refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

export const TokenInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const toastr = inject(ToastrService);
  const router = inject(Router);

  // Login, register, config ve refresh token isteklerini token eklemeden (veya withCredentials ile) gönder
  if (
    req.url.includes('/auth/login') ||
    req.url.includes('/auth/register') ||
    req.url.includes('/auth/refresh') ||
    req.url.includes('/auth/google') ||
    req.url.includes('/auth/authorization-policies')
  ) {
    return next(req);
  }

  // Mevcut access token ile isteği klonla
  const accessToken = authService.getAccessToken();
  if (accessToken) {
    req = addToken(req, accessToken);
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Access token süresi dolmuşsa (401) ve refresh token varsa veya remember-me çerezi olasıysa
      if (error.status === 401) {
        return handle401Error(req, next, authService, router);
      }
      
      // Diğer hataları işle
      handleHttpError(error, authService, router, toastr);
      return throwError(() => error);
    })
  );
};

// İsteklere token ekleyen yardımcı fonksiyon
function addToken(request: HttpRequest<any>, token: string) {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

// 401 (Unauthorized) hatasını işleyen fonksiyon
function handle401Error(
  request: HttpRequest<any>,
  next: HttpHandlerFn,
  authService: AuthService,
  router: Router
): Observable<HttpEvent<any>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap((tokenResponse: any) => {
        isRefreshing = false;
        const newAccess = tokenResponse?.data?.accessToken || tokenResponse?.accessToken;
        refreshTokenSubject.next(newAccess);
        return next(addToken(request, newAccess));
      }),
      catchError((err) => {
        isRefreshing = false;
        // Çıkış sürecindeysek veya remember-me yoksa sessizce login'e yönlen
        if (!authService.isCurrentlyLoggingOut()) {
          authService.logout().subscribe({ complete: () => {
            router.navigate(['/auth/login'], { queryParams: { returnUrl: router.url } });
          }});
        }
        return throwError(() => err);
      })
    );
  } else {
    return refreshTokenSubject.pipe(
      filter((token) => token != null),
      take(1),
      switchMap((jwt) => {
        return next(addToken(request, jwt));
      })
    );
  }
}

// Genel HTTP hatalarını işleyen ve kullanıcıya bildirim gösteren fonksiyon
function handleHttpError(
  error: HttpErrorResponse,
  authService: AuthService,
  router: Router,
  toastr: ToastrService
) {
  // Profil GET isteğinde 404 hatası gelirse, component'in yönetmesi için sessiz kal.
  if (error.status === 404 && error.url?.includes('/api/v1/profiles')) {
    return;
  }
  
  // 400 Bad Request ve 'errors' objesi içeren validasyon hatalarını component'in işlemesi için atla.
  if (error.status === 400 && error.error?.errors) {
    return; // Toaster gösterme, component'in yönetmesine izin ver.
  }

  let errorMessage = 'Bilinmeyen bir hata oluştu!';
  const serverError = error.error;

  switch (error.status) {
    case 400: // Bad Request
      if (serverError && Array.isArray(serverError.errors) && serverError.errors.length > 0) {
        // Validation errors gibi backend'den gelen liste halindeki hatalar
        errorMessage = serverError.errors.map((e: any) => e.message || e).join('<br>');
      } else if (serverError && serverError.message) {
        errorMessage = serverError.message;
      } else {
        errorMessage = 'Geçersiz istek. Lütfen girdiğiniz bilgileri kontrol edin.';
      }
      break;
    case 401: // Unauthorized
      // Explicit toaster gösterme: logout akışında veya token yenileme aşamasında uyarı bastır
      return; // Toastr gösterme
    case 403: // Forbidden
      errorMessage = 'Bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır.';
      break;
    case 404: // Not Found
      errorMessage = 'İstenen kaynak bulunamadı.';
      break;
    case 500: // Internal Server Error
      errorMessage = 'Sunucuda bir hata oluştu. Lütfen daha sonra tekrar deneyin.';
      break;
    default:
      if (serverError && serverError.message) {
        errorMessage = serverError.message;
      } else {
        errorMessage = `Hata Kodu: ${error.status} - ${error.statusText}`;
      }
      break;
  }
  
  // Bu log component'te de tekrarlandığı için buradan kaldırılabilir veya şartlı hale getirilebilir.
  // console.error('Interceptor Error:', {
  //   status: error.status,
  //   message: error.message,
  //   errorBody: error.error
  // });

  toastr.error(errorMessage, 'Hata', { enableHtml: true });
}
