import { Directive, ElementRef, inject, OnInit, Renderer2 } from '@angular/core';
import { SecurityHeadersService } from '../services/security-headers.service';

/**
 * CSP Nonce Directive
 * Automatically adds nonce attribute to script and style elements for CSP compliance
 */
@Directive({
  selector: 'script[cspNonce], style[cspNonce], link[cspNonce]',
  standalone: true
})
export class CspNonceDirective implements OnInit {
  private readonly element = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly securityService = inject(SecurityHeadersService);

  ngOnInit(): void {
    this.addNonceAttribute();
  }

  private addNonceAttribute(): void {
    const nonce = this.securityService.generateNonce();
    const element = this.element.nativeElement;

    if (element) {
      this.renderer.setAttribute(element, 'nonce', nonce);
    }
  }
}

/**
 * CSP Safe Inline Directive
 * Marks inline scripts/styles as safe by adding appropriate CSP attributes
 */
@Directive({
  selector: '[cspSafe]',
  standalone: true
})
export class CspSafeDirective implements OnInit {
  private readonly element = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly securityService = inject(SecurityHeadersService);

  ngOnInit(): void {
    this.makeSafeForCSP();
  }

  private makeSafeForCSP(): void {
    const element = this.element.nativeElement;
    const tagName = element.tagName.toLowerCase();

    if (tagName === 'script' || tagName === 'style') {
      // Add nonce for inline scripts/styles
      const nonce = this.securityService.generateNonce();
      this.renderer.setAttribute(element, 'nonce', nonce);

      // Add integrity attribute if content is significant
      if (element.textContent && element.textContent.length > 50) {
        this.addIntegrityAttribute(element);
      }
    }
  }

  private addIntegrityAttribute(element: HTMLElement): void {
    // Calculate SRI hash for inline content
    const content = element.textContent || '';

    if (crypto && crypto.subtle) {
      crypto.subtle.digest('SHA-256', new TextEncoder().encode(content))
        .then(hashBuffer => {
          const hashArray = Array.from(new Uint8Array(hashBuffer));
          const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
          const integrity = `sha256-${btoa(hashHex)}`;

          this.renderer.setAttribute(element, 'integrity', integrity);
        })
        .catch(error => {
          console.warn('Failed to calculate integrity hash:', error);
        });
    }
  }
}