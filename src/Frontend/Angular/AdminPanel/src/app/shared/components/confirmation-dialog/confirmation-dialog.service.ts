import { Injectable, inject } from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

import { ConfirmationDialogComponent } from './confirmation-dialog.component';
import {
  ConfirmationDialogConfig,
  ConfirmationDialogResult,
  ConfirmationDialogOptions,
  IConfirmationDialogService,
  CONFIRMATION_DIALOG_PRESETS
} from './confirmation-dialog.models';

@Injectable({
  providedIn: 'root'
})
export class ConfirmationDialogService implements IConfirmationDialogService {
  private readonly dialog = inject(MatDialog);

  /**
   * Genel onay dialog'u açar
   */
  public async confirm(options: ConfirmationDialogOptions): Promise<boolean> {
    const config: ConfirmationDialogConfig = {
      title: options.title || 'Onay',
      message: options.message,
      confirmText: options.confirmText || 'Evet',
      cancelText: options.cancelText || 'Hayır',
      details: options.details,
      requireTextConfirmation: options.requireTextConfirmation,
      confirmationText: options.confirmationText,
      ...CONFIRMATION_DIALOG_PRESETS.question.config
    };

    const result = await this.open(config);
    return result.confirmed;
  }

  /**
   * Bilgi dialog'u açar
   */
  public async info(message: string, title: string = 'Bilgi'): Promise<void> {
    const config: ConfirmationDialogConfig = {
      title,
      message,
      ...CONFIRMATION_DIALOG_PRESETS.info.config
    };

    await this.open(config);
  }

  /**
   * Uyarı dialog'u açar
   */
  public async warning(message: string, title: string = 'Uyarı'): Promise<boolean> {
    const config: ConfirmationDialogConfig = {
      title,
      message,
      ...CONFIRMATION_DIALOG_PRESETS.warning.config
    };

    const result = await this.open(config);
    return result.confirmed;
  }

  /**
   * Tehlikeli işlem dialog'u açar
   */
  public async danger(
    message: string,
    title: string = 'Dikkat',
    requireConfirmation: boolean = false
  ): Promise<boolean> {
    const config: ConfirmationDialogConfig = {
      title,
      message,
      requireTextConfirmation: requireConfirmation,
      confirmationText: requireConfirmation ? 'SİL' : undefined,
      ...CONFIRMATION_DIALOG_PRESETS.danger.config
    };

    const result = await this.open(config);
    return result.confirmed;
  }

  /**
   * Başarı dialog'u açar
   */
  public async success(message: string, title: string = 'Başarılı'): Promise<void> {
    const config: ConfirmationDialogConfig = {
      title,
      message,
      ...CONFIRMATION_DIALOG_PRESETS.success.config
    };

    await this.open(config);
  }

  /**
   * Gelişmiş dialog açar
   */
  public async open(config: ConfirmationDialogConfig): Promise<ConfirmationDialogResult> {
    const dialogConfig: MatDialogConfig = {
      width: config.width || '400px',
      maxWidth: config.maxWidth || '90vw',
      height: config.height,
      maxHeight: config.maxHeight || '90vh',
      disableClose: config.disableClose || false,
      hasBackdrop: config.hasBackdrop !== false,
      panelClass: config.panelClass,
      autoFocus: false, // Manual focus control
      restoreFocus: true,
      data: config
    };

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, dialogConfig);

    try {
      const result = await firstValueFrom(dialogRef.afterClosed());
      return result || { confirmed: false };
    } catch (error) {
      return { confirmed: false };
    }
  }

  /**
   * Silme onayı dialog'u
   */
  public async deleteConfirmation(itemName?: string): Promise<boolean> {
    const message = itemName
      ? `"${itemName}" kalıcı olarak silinecek. Bu işlem geri alınamaz.`
      : 'Seçili öğe kalıcı olarak silinecek. Bu işlem geri alınamaz.';

    return this.danger(message, 'Silme Onayı', true);
  }

  /**
   * Kaydedilmemiş değişiklikler dialog'u
   */
  public async unsavedChanges(): Promise<boolean> {
    const config: ConfirmationDialogConfig = {
      title: 'Kaydedilmemiş Değişiklikler',
      message: 'Kaydedilmemiş değişiklikleriniz var. Çıkmak istediğinizden emin misiniz?',
      confirmText: 'Çık',
      cancelText: 'Kalsam',
      details: 'Bu sayfadan çıkarsanız, yaptığınız değişiklikler kaybolacak.',
      ...CONFIRMATION_DIALOG_PRESETS.warning.config
    };

    const result = await this.open(config);
    return result.confirmed;
  }

  /**
   * Çıkış onayı dialog'u
   */
  public async logoutConfirmation(): Promise<boolean> {
    const config: ConfirmationDialogConfig = {
      title: 'Çıkış Onayı',
      message: 'Sistemden çıkmak istediğinizden emin misiniz?',
      confirmText: 'Çıkış Yap',
      cancelText: 'İptal',
      ...CONFIRMATION_DIALOG_PRESETS.question.config
    };

    const result = await this.open(config);
    return result.confirmed;
  }

  /**
   * Toplu silme onayı dialog'u
   */
  public async bulkDeleteConfirmation(count: number): Promise<boolean> {
    const config: ConfirmationDialogConfig = {
      title: 'Toplu Silme Onayı',
      message: `${count} öğe kalıcı olarak silinecek. Bu işlem geri alınamaz.`,
      confirmText: 'Tümünü Sil',
      cancelText: 'İptal',
      requireTextConfirmation: count > 10, // 10'dan fazla öğe için metin onayı iste
      confirmationText: count > 10 ? 'SİL' : undefined,
      details: count > 10 ? 'Çok sayıda öğe silmek üzeresiniz. Lütfen dikkatli olun.' : undefined,
      ...CONFIRMATION_DIALOG_PRESETS.danger.config
    };

    const result = await this.open(config);
    return result.confirmed;
  }

  /**
   * Hızlı onay shortcut'ları
   */
  public readonly shortcuts = {
    /**
     * Hızlı silme onayı
     */
    quickDelete: (itemName?: string): Promise<boolean> => {
      return this.deleteConfirmation(itemName);
    },

    /**
     * Hızlı çıkış onayı
     */
    quickLogout: (): Promise<boolean> => {
      return this.logoutConfirmation();
    },

    /**
     * Hızlı kayıt onayı
     */
    quickSave: async (message: string = 'Değişiklikler kaydedilsin mi?'): Promise<boolean> => {
      return this.confirm({
        title: 'Kaydetme Onayı',
        message,
        confirmText: 'Kaydet',
        cancelText: 'İptal'
      });
    },

    /**
     * Hızlı iptal onayı
     */
    quickCancel: async (message: string = 'İşlemi iptal etmek istediğinizden emin misiniz?'): Promise<boolean> => {
      return this.confirm({
        title: 'İptal Onayı',
        message,
        confirmText: 'İptal Et',
        cancelText: 'Devam Et'
      });
    }
  };

  /**
   * Dialog açık mı kontrol eder
   */
  public hasOpenDialogs(): boolean {
    return this.dialog.openDialogs.length > 0;
  }

  /**
   * Tüm dialog'ları kapatır
   */
  public closeAll(): void {
    this.dialog.closeAll();
  }
}