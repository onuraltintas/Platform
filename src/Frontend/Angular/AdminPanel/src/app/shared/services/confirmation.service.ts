import { Injectable } from '@angular/core';
// import { MatDialog } from '@angular/material/dialog';

export interface ConfirmationConfig {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  type?: 'warning' | 'danger' | 'info' | 'success';
}

@Injectable({
  providedIn: 'root'
})
export class ConfirmationService {
  // private _dialog = inject(MatDialog); // Not used currently

  confirm(config: ConfirmationConfig): Promise<boolean> {
    return new Promise((resolve) => {
      const confirmResult = window.confirm(
        `${config.title || 'Onay'}\n\n${config.message}`
      );
      resolve(confirmResult);
    });
  }

  confirmDelete(itemName: string = 'öğe'): Promise<boolean> {
    return this.confirm({
      title: 'Silme Onayı',
      message: `Bu ${itemName} silinecek. Bu işlem geri alınamaz. Devam etmek istiyor musunuz?`,
      type: 'danger'
    });
  }

  confirmBulkAction(action: string, count: number): Promise<boolean> {
    return this.confirm({
      title: 'Toplu İşlem Onayı',
      message: `${count} öğe üzerinde "${action}" işlemi gerçekleştirilecek. Devam etmek istiyor musunuz?`,
      type: 'warning'
    });
  }
}