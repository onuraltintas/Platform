// Google Sign-In TypeScript definitions
export interface GoogleCredentialResponse {
  credential: string;
  select_by: string;
}

export interface GoogleOneTapConfig {
  client_id: string;
  callback: (response: GoogleCredentialResponse) => void;
  auto_select?: boolean;
  cancel_on_tap_outside?: boolean;
}

export interface GoogleButtonConfig {
  type: 'standard' | 'icon';
  theme: 'outline' | 'filled_blue' | 'filled_black';
  size: 'large' | 'medium' | 'small';
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
  shape?: 'rectangular' | 'pill' | 'circle' | 'square';
  logo_alignment?: 'left' | 'center';
  width?: string;
  locale?: string;
}

declare global {
  interface Window {
    google: {
      accounts: {
        id: {
          initialize: (config: GoogleOneTapConfig) => void;
          prompt: (momentListener?: (notification: any) => void) => void;
          renderButton: (
            parent: HTMLElement,
            config: GoogleButtonConfig & {
              callback: (response: GoogleCredentialResponse) => void;
            }
          ) => void;
          cancel: () => void;
          disableAutoSelect: () => void;
        };
      };
    };
  }
}