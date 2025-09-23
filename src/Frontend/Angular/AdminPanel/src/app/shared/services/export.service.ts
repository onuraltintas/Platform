import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';
import * as FileSaver from 'file-saver';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

export interface ExportColumn {
  field: string;
  header: string;
  width?: number;
  format?: (value: unknown) => string;
}

export interface ExportOptions {
  fileName?: string;
  sheetName?: string;
  title?: string;
  subtitle?: string;
  dateFormat?: string;
  includeDate?: boolean;
  orientation?: 'portrait' | 'landscape';
}

@Injectable({
  providedIn: 'root'
})
export class ExportService {

  constructor() {}

  /**
   * Export data to Excel format
   */
  exportToExcel(
    data: unknown[],
    columns: ExportColumn[],
    options: ExportOptions = {}
  ): void {
    const fileName = this.generateFileName(options.fileName || 'export', 'xlsx', options.includeDate);
    const _sheetName = options.sheetName || 'Sheet1';

    // Prepare data
    const exportData = this.prepareData(data, columns);

    // Create worksheet
    const ws: XLSX.WorkSheet = XLSX.utils.json_to_sheet(exportData);

    // Auto-size columns
    const colWidths = this.calculateColumnWidths(exportData, columns);
    ws['!cols'] = colWidths.map(w => ({ wch: w }));

    // Create workbook
    const wb: XLSX.WorkBook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, _sheetName);

    // Save file
    XLSX.writeFile(wb, fileName);
  }

  /**
   * Export data to CSV format
   */
  exportToCSV(
    data: unknown[],
    columns: ExportColumn[],
    options: ExportOptions = {}
  ): void {
    const fileName = this.generateFileName(options.fileName || 'export', 'csv', options.includeDate);

    // Prepare data
    const exportData = this.prepareData(data, columns);

    // Convert to CSV
    const csvContent = this.convertToCSV(exportData);

    // Save file
    const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' });
    FileSaver.saveAs(blob, fileName);
  }

  /**
   * Export data to PDF format
   */
  exportToPDF(
    data: unknown[],
    columns: ExportColumn[],
    options: ExportOptions = {}
  ): void {
    const fileName = this.generateFileName(options.fileName || 'export', 'pdf', options.includeDate);
    const orientation = options.orientation || 'portrait';

    // Create PDF document
    const doc = new jsPDF({
      orientation: orientation,
      unit: 'mm',
      format: 'a4'
    });

    // Add title if provided
    if (options.title) {
      doc.setFontSize(16);
      doc.text(options.title, doc.internal.pageSize.getWidth() / 2, 15, { align: 'center' });
    }

    // Add subtitle if provided
    if (options.subtitle) {
      doc.setFontSize(12);
      doc.text(options.subtitle, doc.internal.pageSize.getWidth() / 2, 22, { align: 'center' });
    }

    // Prepare table data
    const headers = columns.map(col => col.header);
    const rows = data.map(item =>
      columns.map(col => {
        const value = this.getNestedProperty(item, col.field);
        return col.format ? col.format(value) : this.formatValue(value);
      })
    );

    // Generate table
    autoTable(doc, {
      head: [headers],
      body: rows,
      startY: options.title ? (options.subtitle ? 30 : 25) : 10,
      theme: 'striped',
      headStyles: {
        fillColor: [66, 139, 202],
        textColor: 255,
        fontStyle: 'bold'
      },
      alternateRowStyles: {
        fillColor: [245, 245, 245]
      },
      margin: { top: 10, right: 10, bottom: 10, left: 10 },
      styles: {
        fontSize: 9,
        cellPadding: 3
      }
    });

    // Add page numbers
    const pageCount = doc.getNumberOfPages();
    for (let i = 1; i <= pageCount; i++) {
      doc.setPage(i);
      doc.setFontSize(8);
      doc.text(
        `Sayfa ${i} / ${pageCount}`,
        doc.internal.pageSize.getWidth() - 20,
        doc.internal.pageSize.getHeight() - 10
      );
    }

    // Save PDF
    doc.save(fileName);
  }

  /**
   * Export JSON data
   */
  exportToJSON(
    data: unknown[],
    options: ExportOptions = {}
  ): void {
    const fileName = this.generateFileName(options.fileName || 'export', 'json', options.includeDate);

    const jsonContent = JSON.stringify(data, null, 2);
    const blob = new Blob([jsonContent], { type: 'application/json' });
    FileSaver.saveAs(blob, fileName);
  }

  /**
   * Export HTML table
   */
  exportTableToExcel(
    tableElement: HTMLTableElement,
    options: ExportOptions = {}
  ): void {
    const fileName = this.generateFileName(options.fileName || 'table-export', 'xlsx', options.includeDate);
    const wb = XLSX.utils.table_to_book(tableElement);
    XLSX.writeFile(wb, fileName);
  }

  /**
   * Batch export - multiple sheets
   */
  exportMultipleSheets(
    sheets: { data: unknown[]; columns: ExportColumn[]; name: string }[],
    options: ExportOptions = {}
  ): void {
    const fileName = this.generateFileName(options.fileName || 'multi-export', 'xlsx', options.includeDate);

    const wb: XLSX.WorkBook = XLSX.utils.book_new();

    sheets.forEach(sheet => {
      const exportData = this.prepareData(sheet.data, sheet.columns);
      const ws: XLSX.WorkSheet = XLSX.utils.json_to_sheet(exportData);

      const colWidths = this.calculateColumnWidths(exportData, sheet.columns);
      ws['!cols'] = colWidths.map(w => ({ wch: w }));

      XLSX.utils.book_append_sheet(wb, ws, sheet.name);
    });

    XLSX.writeFile(wb, fileName);
  }

  /**
   * Export with custom template
   */
  exportWithTemplate(
    data: unknown[],
    _templatePath: string,
    options: ExportOptions = {}
  ): void {
    // This would require server-side template processing
    // Placeholder for future implementation
    console.warn('Template-based export requires server-side processing');
    this.exportToExcel(data, [], options);
  }

  /**
   * Private helper methods
   */
  private prepareData(data: unknown[], columns: ExportColumn[]): Record<string, unknown>[] {
    return data.map(item => {
      const row: Record<string, unknown> = {};
      columns.forEach(col => {
        const value = this.getNestedProperty(item, col.field);
        row[col.header] = col.format ? col.format(value) : this.formatValue(value);
      });
      return row;
    });
  }

  private getNestedProperty(obj: unknown, path: string): unknown {
    return path.split('.').reduce((current: unknown, prop: string) => {
      return (current as Record<string, unknown>)?.[prop];
    }, obj);
  }

  private formatValue(value: unknown): string {
    if (value === null || value === undefined) {
      return '';
    }
    if (value instanceof Date) {
      return value.toLocaleDateString('tr-TR');
    }
    if (typeof value === 'boolean') {
      return value ? 'Evet' : 'Hayır';
    }
    if (typeof value === 'object') {
      return JSON.stringify(value);
    }
    return String(value);
  }

  private calculateColumnWidths(data: Record<string, unknown>[], columns: ExportColumn[]): number[] {
    const widths: number[] = [];

    columns.forEach(col => {
      if (col.width) {
        widths.push(col.width);
      } else {
        // Calculate based on content
        const headerLength = col.header.length;
        const maxDataLength = Math.max(
          ...data.map(row => String(row[col.header] || '').length)
        );
        widths.push(Math.min(Math.max(headerLength, maxDataLength) + 2, 50));
      }
    });

    return widths;
  }

  private convertToCSV(data: Record<string, unknown>[]): string {
    if (data.length === 0) return '';

    const headers = Object.keys(data[0]);
    const csvHeaders = headers.join(',');

    const csvRows = data.map(row =>
      headers.map(header => {
        const value = row[header];
        // Escape quotes and wrap in quotes if contains comma
        const escaped = String(value).replace(/"/g, '""');
        return escaped.includes(',') ? `"${escaped}"` : escaped;
      }).join(',')
    );

    return [csvHeaders, ...csvRows].join('\n');
  }

  private generateFileName(baseName: string, extension: string, includeDate: boolean = true): string {
    if (includeDate) {
      const date = new Date();
      const dateStr = date.toISOString().slice(0, 10).replace(/-/g, '');
      const timeStr = date.toTimeString().slice(0, 8).replace(/:/g, '');
      return `${baseName}_${dateStr}_${timeStr}.${extension}`;
    }
    return `${baseName}.${extension}`;
  }

  /**
   * Export configuration presets
   */
  getExportPresets(): { [key: string]: ExportOptions } {
    return {
      default: {
        includeDate: true,
        orientation: 'portrait'
      },
      report: {
        includeDate: true,
        orientation: 'landscape',
        title: 'Rapor',
        subtitle: new Date().toLocaleDateString('tr-TR')
      },
      simple: {
        includeDate: false
      }
    };
  }

  /**
   * Validate export data
   */
  validateExportData(data: unknown[], columns: ExportColumn[]): { valid: boolean; errors: string[] } {
    const errors: string[] = [];

    if (!data || data.length === 0) {
      errors.push('Veri bulunamadı');
    }

    if (!columns || columns.length === 0) {
      errors.push('Kolon tanımları bulunamadı');
    }

    columns.forEach(col => {
      if (!col.field || !col.header) {
        errors.push(`Eksik kolon tanımı: ${JSON.stringify(col)}`);
      }
    });

    return {
      valid: errors.length === 0,
      errors
    };
  }
}