import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { map, exhaustMap, catchError, tap, switchMap } from 'rxjs/operators';

import { AuthService } from '../../core/auth/services/auth.service';
import { TokenService } from '../../core/auth/services/token.service';
import { NotificationService } from '../../shared/services/notification.service';
import { AuthActions } from './auth.actions';

@Injectable()
export class AuthEffects {
  private readonly actions$ = inject(Actions);
  private readonly authService = inject(AuthService);
  private readonly tokenService = inject(TokenService);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  // Login Effect
  login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.login),
      exhaustMap(({ request }) =>
        this.authService.login(request).pipe(
          map((response) => AuthActions.loginSuccess({ response })),
          catchError((error) =>
            of(AuthActions.loginFailure({ error: error.userMessage || 'Giriş işlemi başarısız' }))
          )
        )
      )
    )
  );

  // Login Success Effect
  loginSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.loginSuccess),
      tap(({ response }) => {
        this.notificationService.success(
          `Hoş geldiniz, ${response.user.firstName}!`,
          'Giriş Başarılı'
        );

        // Redirect to intended page or dashboard
        const redirectUrl = sessionStorage.getItem('redirectUrl') || '/dashboard';
        sessionStorage.removeItem('redirectUrl');
        this.router.navigate([redirectUrl]);
      })
    ),
    { dispatch: false }
  );

  // Login Failure Effect
  loginFailure$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.loginFailure),
      tap(({ error }) => {
        this.notificationService.error(error, 'Giriş Hatası');
      })
    ),
    { dispatch: false }
  );

  // Google Login Effect
  googleLogin$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.googleLogin),
      exhaustMap(({ request }) =>
        this.authService.googleLogin(request).pipe(
          map((response) => AuthActions.googleLoginSuccess({ response })),
          catchError((error) =>
            of(AuthActions.googleLoginFailure({ error: error.userMessage || 'Google giriş işlemi başarısız' }))
          )
        )
      )
    )
  );

  // Register Effect
  register$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.register),
      exhaustMap(({ request }) =>
        this.authService.register(request).pipe(
          map((user) => AuthActions.registerSuccess({ user })),
          catchError((error) =>
            of(AuthActions.registerFailure({ error: error.userMessage || 'Kayıt işlemi başarısız' }))
          )
        )
      )
    )
  );

  // Register Success Effect
  registerSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.registerSuccess),
      tap(() => {
        this.notificationService.success(
          'Hesabınız başarıyla oluşturuldu. E-posta adresinizi doğrulamak için gelen kutunuzu kontrol edin.',
          'Kayıt Başarılı'
        );
        this.router.navigate(['/auth/login']);
      })
    ),
    { dispatch: false }
  );

  // Logout Effect
  logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.logout),
      switchMap(() => {
        // Just logout locally, don't call API
        this.authService.logout();
        return of(AuthActions.logoutSuccess());
      }),
      tap(() => {
        this.router.navigate(['/auth/login']);
      })
    )
  );

  // Refresh Token Effect
  refreshToken$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.refreshToken),
      exhaustMap(() =>
        this.authService.refreshToken().pipe(
          map((response) => AuthActions.refreshTokenSuccess({ response })),
          catchError((error) =>
            of(AuthActions.refreshTokenFailure({ error: error.userMessage || 'Token yenileme başarısız' }))
          )
        )
      )
    )
  );

  // Refresh Token Failure Effect
  refreshTokenFailure$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.refreshTokenFailure),
      tap(() => {
        this.notificationService.error(
          'Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.',
          'Oturum Süresi Doldu'
        );
        this.router.navigate(['/auth/login']);
      })
    ),
    { dispatch: false }
  );

  // Get Current User Effect
  getCurrentUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.getCurrentUser),
      exhaustMap(() =>
        this.authService.getCurrentUser().pipe(
          map((user) => AuthActions.getCurrentUserSuccess({ user })),
          catchError((error) =>
            of(AuthActions.getCurrentUserFailure({ error: error.userMessage || 'Kullanıcı bilgileri alınamadı' }))
          )
        )
      )
    )
  );

  // Change Password Effect
  changePassword$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.changePassword),
      exhaustMap(({ request }) =>
        this.authService.changePassword(request).pipe(
          map(() => AuthActions.changePasswordSuccess()),
          catchError((error) =>
            of(AuthActions.changePasswordFailure({ error: error.userMessage || 'Şifre değiştirme başarısız' }))
          )
        )
      )
    )
  );

  // Change Password Success Effect
  changePasswordSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.changePasswordSuccess),
      tap(() => {
        this.notificationService.success(
          'Şifreniz başarıyla değiştirildi',
          'İşlem Başarılı'
        );
      })
    ),
    { dispatch: false }
  );

  // Request Password Reset Effect
  requestPasswordReset$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.requestPasswordReset),
      exhaustMap(({ email }) =>
        this.authService.requestPasswordReset(email).pipe(
          map(() => AuthActions.requestPasswordResetSuccess()),
          catchError((error) =>
            of(AuthActions.requestPasswordResetFailure({ error: error.userMessage || 'Şifre sıfırlama e-postası gönderilemedi' }))
          )
        )
      )
    )
  );

  // Reset Password Effect
  resetPassword$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.resetPassword),
      exhaustMap(({ request }) =>
        this.authService.resetPassword(request).pipe(
          map(() => AuthActions.resetPasswordSuccess()),
          catchError((error) =>
            of(AuthActions.resetPasswordFailure({ error: error.userMessage || 'Şifre sıfırlama başarısız' }))
          )
        )
      )
    )
  );

  // Reset Password Success Effect
  resetPasswordSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.resetPasswordSuccess),
      tap(() => {
        this.notificationService.success(
          'Şifreniz başarıyla güncellendi',
          'İşlem Başarılı'
        );
      })
    ),
    { dispatch: false }
  );

  // Verify Email Effect
  verifyEmail$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.verifyEmail),
      exhaustMap(({ token }) =>
        this.authService.verifyEmail(token).pipe(
          map(() => AuthActions.verifyEmailSuccess()),
          catchError((error) =>
            of(AuthActions.verifyEmailFailure({ error: error.userMessage || 'E-posta doğrulaması başarısız' }))
          )
        )
      )
    )
  );

  // Verify Email Success Effect
  verifyEmailSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.verifyEmailSuccess),
      tap(() => {
        this.notificationService.success(
          'E-posta adresiniz başarıyla doğrulandı',
          'Doğrulama Başarılı'
        );
      })
    ),
    { dispatch: false }
  );

  // Resend Verification Email Effect
  resendVerificationEmail$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.resendVerificationEmail),
      exhaustMap(() =>
        this.authService.resendVerificationEmail().pipe(
          map(() => AuthActions.resendVerificationEmailSuccess()),
          catchError((error) =>
            of(AuthActions.resendVerificationEmailFailure({ error: error.userMessage || 'Doğrulama e-postası gönderilemedi' }))
          )
        )
      )
    )
  );

  // Check Permission Effect
  checkPermission$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.checkPermission),
      exhaustMap(({ permission }) =>
        this.authService.hasPermission(permission).pipe(
          map((hasPermission) => AuthActions.checkPermissionSuccess({ permission, hasPermission })),
          catchError((error) =>
            of(AuthActions.checkPermissionFailure({ error: error.userMessage || 'İzin kontrolü başarısız' }))
          )
        )
      )
    )
  );

  // Initialize Auth State Effect
  initializeAuthState$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.initializeAuthState),
      switchMap(() => {
        // Check if user has valid tokens
        if (this.tokenService.isTokenValid()) {
          // Try to get current user info
          return of(AuthActions.getCurrentUser());
        } else if (this.tokenService.isRefreshTokenValid()) {
          // Try to refresh token
          return of(AuthActions.refreshToken());
        } else {
          // No valid tokens, clear auth state
          return of(AuthActions.clearAuthState());
        }
      })
    )
  );

  // Session Expired Effect
  sessionExpired$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.sessionExpired),
      tap(() => {
        this.notificationService.warning(
          'Oturum süreniz doldu. Güvenliğiniz için çıkış yapıldı.',
          'Oturum Süresi Doldu'
        );
        this.router.navigate(['/auth/login']);
      })
    ),
    { dispatch: false }
  );
}