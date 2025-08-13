import { ErrorHandler, Injectable, NgZone } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {

  constructor(private zone: NgZone, private toastr: ToastrService) { }

  handleError(error: any): void {
    // Hatanın Zone.js dışından geldiğinden emin olun, aksi takdirde sonsuz döngüye girilebilir.
    this.zone.run(() => {
      let errorMessage = 'Beklenmedik bir hata oluştu!';

      // Konsola hatayı logla (geliştirme aşamasında faydalıdır)
      console.error('Angular global hata:', error);

      // Hata mesajını daha anlaşılır hale getir
      if (error && error.message) {
        errorMessage = `Hata: ${error.message}`;
      } else if (typeof error === 'string') {
        errorMessage = `Hata: ${error}`;
      }

      // Toastr ile kullanıcıya bildirim göster
      this.toastr.error(errorMessage, 'Uygulama Hatası');
    });
  }
}