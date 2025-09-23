import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';

export interface ToastConfig {
  message: string;
  title?: string;
  options?: Record<string, unknown>;
}

export interface BulkOperationConfig {
  total: number;
  entityName: string;
  operation: string;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private readonly toastr = inject(ToastrService);

  private readonly defaultConfig = {
    timeOut: 3000,
    progressBar: true,
    positionClass: 'toast-top-right',
    preventDuplicates: true
  };

  /**
   * Başarı bildirimi gösterir
   */
  public basari(mesaj: string, baslik?: string, options?: Record<string, unknown>): void {
    this.toastr.success(mesaj, baslik || 'Başarılı', {
      ...this.defaultConfig,
      ...options
    });
  }

  /**
   * Hata bildirimi gösterir
   */
  public hata(mesaj: string, baslik?: string, options?: Record<string, unknown>): void {
    this.toastr.error(mesaj, baslik || 'Hata', {
      ...this.defaultConfig,
      timeOut: 5000,
      ...options
    });
  }

  /**
   * Uyarı bildirimi gösterir
   */
  public uyari(mesaj: string, baslik?: string, options?: Record<string, unknown>): void {
    this.toastr.warning(mesaj, baslik || 'Uyarı', {
      ...this.defaultConfig,
      timeOut: 4000,
      ...options
    });
  }

  /**
   * Bilgi bildirimi gösterir
   */
  public bilgi(mesaj: string, baslik?: string, options?: Record<string, unknown>): void {
    this.toastr.info(mesaj, baslik || 'Bilgi', {
      ...this.defaultConfig,
      ...options
    });
  }

  /**
   * API hatalarını otomatik parse eder ve kullanıcı dostu mesaj gösterir
   */
  public apiHatasi(error: HttpErrorResponse | Error | any): void {
    let mesaj = 'Bir hata oluştu';
    let baslik = 'Hata';

    // Backend hata formatlarını parse et
    if ((error as any)?.error?.message) {
      mesaj = (error as any).error.message;
    } else if ((error as any)?.error?.error?.message) {
      mesaj = (error as any).error.error.message;
    } else if ((error as any)?.message) {
      mesaj = (error as any).message;
    } else if ((error as any)?.userMessage) {
      mesaj = (error as any).userMessage;
    } else if (typeof error === 'string') {
      mesaj = error;
    }

    // HTTP status kodlarına göre özel mesajlar
    if ((error as any)?.status) {
      switch ((error as any).status) {
        case 400:
          baslik = 'Geçersiz İstek';
          if (!(error as any)?.error?.message) {
            mesaj = 'Gönderilen veriler geçersiz';
          }
          break;
        case 401:
          baslik = 'Yetki Hatası';
          if (!(error as any)?.error?.message) {
            mesaj = 'Bu işlem için yetkiniz bulunmuyor';
          }
          break;
        case 403:
          baslik = 'Erişim Engellendi';
          if (!(error as any)?.error?.message) {
            mesaj = 'Bu kaynağa erişim yetkiniz yok';
          }
          break;
        case 404:
          baslik = 'Bulunamadı';
          if (!(error as any)?.error?.message) {
            mesaj = 'İstenen kaynak bulunamadı';
          }
          break;
        case 422:
          baslik = 'Doğrulama Hatası';
          if (!(error as any)?.error?.message) {
            mesaj = 'Girilen veriler doğrulama kurallarına uymuyor';
          }
          break;
        case 500:
          baslik = 'Sunucu Hatası';
          if (!(error as any)?.error?.message) {
            mesaj = 'Sunucu tarafında bir hata oluştu';
          }
          break;
        case 503:
          baslik = 'Servis Kullanılamıyor';
          if (!(error as any)?.error?.message) {
            mesaj = 'Servis şu anda kullanılamıyor, lütfen daha sonra tekrar deneyin';
          }
          break;
      }
    }

    this.hata(mesaj, baslik);
  }

  /**
   * Toplu işlem başlangıcında bildirim gösterir
   */
  public topluIslemBasladi(config: BulkOperationConfig): void {
    this.bilgi(
      `${config.total} ${config.entityName} ${config.operation} işlemi başlatıldı`,
      'Toplu İşlem',
      { timeOut: 2000 }
    );
  }

  /**
   * Toplu işlem tamamlandığında sonuç bildirimini gösterir
   */
  public topluIslemTamamlandi(basarili: number, basarisiz: number, entityName: string): void {
    if (basarisiz === 0) {
      this.basari(
        `${basarili} ${entityName} başarıyla işlendi`,
        'Toplu İşlem Tamamlandı'
      );
    } else if (basarili === 0) {
      this.hata(
        `${basarisiz} ${entityName} işlenemedi`,
        'Toplu İşlem Başarısız'
      );
    } else {
      this.uyari(
        `${basarili} ${entityName} başarıyla işlendi, ${basarisiz} ${entityName} işlenemedi`,
        'Toplu İşlem Kısmen Başarılı'
      );
    }
  }

  /**
   * İlerleme durumu bildirimi gösterir
   */
  public ilerleme(mesaj: string, yuzde?: number): void {
    const title = yuzde ? `İlerleme: %${yuzde}` : 'İşlem Devam Ediyor';
    this.bilgi(mesaj, title, {
      timeOut: 1500,
      progressBar: true
    });
  }

  /**
   * Kaydetme işlemi bildirimi
   */
  public kaydedildi(entityName?: string): void {
    this.basari(
      `${entityName || 'Kayıt'} başarıyla kaydedildi`,
      'Kaydetme Başarılı'
    );
  }

  /**
   * Güncelleme işlemi bildirimi
   */
  public guncellendi(entityName?: string): void {
    this.basari(
      `${entityName || 'Kayıt'} başarıyla güncellendi`,
      'Güncelleme Başarılı'
    );
  }

  /**
   * Silme işlemi bildirimi
   */
  public silindi(entityName?: string): void {
    this.basari(
      `${entityName || 'Kayıt'} başarıyla silindi`,
      'Silme Başarılı'
    );
  }

  /**
   * Kopyalama işlemi bildirimi
   */
  public kopyalandi(): void {
    this.bilgi('Panoya kopyalandı', 'Kopyalama', { timeOut: 1500 });
  }

  /**
   * Dışa aktarma işlemi bildirimi
   */
  public disaAktarildi(format: string, dosyaAdi?: string): void {
    const mesaj = dosyaAdi
      ? `${dosyaAdi} dosyası ${format} formatında indirildi`
      : `Veriler ${format} formatında dışa aktarıldı`;

    this.basari(mesaj, 'Dışa Aktarma Başarılı');
  }

  /**
   * İçe aktarma işlemi bildirimi
   */
  public iceAktarildi(kayitSayisi: number): void {
    this.basari(
      `${kayitSayisi} kayıt başarıyla içe aktarıldı`,
      'İçe Aktarma Başarılı'
    );
  }

  /**
   * Tüm toast bildirimlerini temizler
   */
  public temizle(): void {
    this.toastr.clear();
  }
}