import { Injectable, Inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class GoogleAuthService {

  constructor(
    @Inject(DOCUMENT) private document: Document
  ) {}

  googleLogin(): void {
    try {
      const csrfToken = Date.now().toString(); // Simple CSRF token
      const state = btoa(JSON.stringify({
        csrfToken,
        redirectUrl: '/dashboard'
      }));

      // secrets.env'den GOOGLE_CLIENT_ID gelecek, şimdilik environment'tan al
      const clientId = environment.oauth.google.clientId || '419291322983-k4rl6v4set9caq2p3prk0q6hd03hngik.apps.googleusercontent.com';

      const authUrl = `https://accounts.google.com/o/oauth2/v2/auth?` +
        `client_id=${clientId}&` +
        `redirect_uri=${encodeURIComponent(environment.oauth.google.backendRedirectUri)}&` +
        `response_type=code&` +
        `scope=openid%20profile%20email&` +
        `access_type=offline&` +
        `prompt=select_account&` +
        `state=${state}`;

      console.log('Redirecting to Google OAuth URL:', authUrl);

      // Force immediate redirect to Google
      this.document.location.href = authUrl;

    } catch (error) {
      console.error('Error during Google login redirect:', error);
    }
  }
}