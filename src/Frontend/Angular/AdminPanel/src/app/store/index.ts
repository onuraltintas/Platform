import { ActionReducerMap } from '@ngrx/store';

import { authReducer } from './auth/auth.reducer';
import { AuthState } from './auth/auth.state';

export { AuthEffects } from './auth/auth.effects';

export interface AppState {
  auth: AuthState;
}

export const reducers: ActionReducerMap<AppState> = {
  auth: authReducer
};