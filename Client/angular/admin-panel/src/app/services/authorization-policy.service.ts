import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { AUTHORIZATION_POLICIES, AuthorizationPolicy } from '../auth/authorization.policies';

interface RemotePolicyDTO {
  match: string; // regex string
  requiredRoles?: string[];
  requiredPermissions?: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthorizationPolicyService {
  private policies$ = new BehaviorSubject<AuthorizationPolicy[]>(AUTHORIZATION_POLICIES);

  constructor(private http: HttpClient) {}

  getPolicies(): AuthorizationPolicy[] {
    return this.policies$.getValue();
  }

  async load(): Promise<void> {
    try {
      const url = `${environment.apiUrl}/v1/auth/authorization-policies`;
      const remote = await firstValueFrom(this.http.get<RemotePolicyDTO[]>(url));
      const parsed: AuthorizationPolicy[] = (remote || []).map(p => ({
        match: new RegExp(p.match),
        requiredRoles: p.requiredRoles ?? [],
        requiredPermissions: p.requiredPermissions ?? []
      }));
      if (parsed.length > 0) {
        this.policies$.next(parsed);
      }
    } catch {
      // Sessiz geç: backend endpoint yoksa yerel fallback kullanılacak
    }
  }
}

export function preloadAuthorizationPolicies(policyService: AuthorizationPolicyService) {
  return () => policyService.load();
}

