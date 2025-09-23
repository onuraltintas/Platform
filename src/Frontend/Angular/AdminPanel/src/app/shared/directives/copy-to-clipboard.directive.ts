import { Directive, Input, HostListener, inject } from '@angular/core';
import { ToastService } from '../../core/bildirimler/toast.service';

@Directive({
  selector: '[appCopyToClipboard]',
  standalone: true
})
export class CopyToClipboardDirective {
  @Input() copyText: string = '';
  @Input() copySuccessMessage: string = 'Panoya kopyalandı';
  @Input() copyErrorMessage: string = 'Kopyalama başarısız';
  @Input() showToast: boolean = true;

  private readonly toastService = inject(ToastService);

  @HostListener('click', ['$event'])
  async onClick(event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();

    try {
      await this.copyToClipboard(this.copyText);

      if (this.showToast) {
        this.toastService.basari(this.copySuccessMessage);
      }
    } catch (error) {
      if (this.showToast) {
        this.toastService.hata(this.copyErrorMessage);
      }
    }
  }

  private async copyToClipboard(text: string): Promise<void> {
    if (!text) {
      throw new Error('Kopyalanacak metin bulunamadı');
    }

    // Modern browsers - Clipboard API
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return;
    }

    // Fallback - Legacy browsers
    return this.legacyCopyToClipboard(text);
  }

  private legacyCopyToClipboard(text: string): Promise<void> {
    return new Promise((resolve, reject) => {
      // Geçici textarea oluştur
      const textArea = document.createElement('textarea');
      textArea.value = text;
      textArea.style.position = 'fixed';
      textArea.style.left = '-999999px';
      textArea.style.top = '-999999px';

      document.body.appendChild(textArea);

      try {
        textArea.focus();
        textArea.select();

        const successful = document.execCommand('copy');
        if (successful) {
          resolve();
        } else {
          reject(new Error('Kopyalama komutu başarısız'));
        }
      } catch (error) {
        reject(error);
      } finally {
        document.body.removeChild(textArea);
      }
    });
  }
}