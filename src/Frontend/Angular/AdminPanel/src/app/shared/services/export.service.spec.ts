import { TestBed } from '@angular/core/testing';
import { ExportService, ExportColumn, ExportOptions } from './export.service';

// Mock file-saver
const mockSaveAs = jest.fn();
jest.mock('file-saver', () => ({
  saveAs: mockSaveAs
}));

// Mock xlsx
const mockXLSX = {
  utils: {
    json_to_sheet: jest.fn(),
    book_new: jest.fn(),
    book_append_sheet: jest.fn(),
    table_to_book: jest.fn()
  },
  writeFile: jest.fn()
};
jest.mock('xlsx', () => mockXLSX);

// Mock jspdf
const mockJsPDF = jest.fn().mockImplementation(() => ({
  internal: {
    pageSize: {
      getWidth: jest.fn().mockReturnValue(210),
      getHeight: jest.fn().mockReturnValue(297)
    }
  },
  setFontSize: jest.fn(),
  text: jest.fn(),
  getNumberOfPages: jest.fn().mockReturnValue(1),
  setPage: jest.fn(),
  save: jest.fn()
}));
jest.mock('jspdf', () => ({ default: mockJsPDF }));

// Mock jspdf-autotable
const mockAutoTable = jest.fn();
jest.mock('jspdf-autotable', () => ({ default: mockAutoTable }));

describe('ExportService', () => {
  let service: ExportService;

  const mockData = [
    { id: 1, name: 'John Doe', email: 'john@example.com', age: 30 },
    { id: 2, name: 'Jane Smith', email: 'jane@example.com', age: 25 }
  ];

  const mockColumns: ExportColumn[] = [
    { field: 'id', header: 'ID' },
    { field: 'name', header: 'Name' },
    { field: 'email', header: 'Email' },
    { field: 'age', header: 'Age' }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ExportService]
    });
    service = TestBed.inject(ExportService);
    jest.clearAllMocks();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('exportToExcel', () => {
    it('should export data to Excel format', () => {
      const mockWorkbook = { SheetNames: [], Sheets: {} };
      const mockWorksheet = {};

      (XLSX.utils.json_to_sheet as jest.Mock).mockReturnValue(mockWorksheet);
      (XLSX.utils.book_new as jest.Mock).mockReturnValue(mockWorkbook);
      (XLSX.utils.book_append_sheet as jest.Mock).mockImplementation(() => {});
      (XLSX.writeFile as jest.Mock).mockImplementation(() => {});

      const options: ExportOptions = {
        fileName: 'test-export',
        sheetName: 'TestSheet'
      };

      service.exportToExcel(mockData, mockColumns, options);

      expect(XLSX.utils.json_to_sheet).toHaveBeenCalled();
      expect(XLSX.utils.book_new).toHaveBeenCalled();
      expect(XLSX.utils.book_append_sheet).toHaveBeenCalledWith(
        mockWorkbook,
        mockWorksheet,
        'TestSheet'
      );
      expect(XLSX.writeFile).toHaveBeenCalled();
    });

    it('should use default values when options not provided', () => {
      (XLSX.utils.json_to_sheet as jest.Mock).mockReturnValue({});
      (XLSX.utils.book_new as jest.Mock).mockReturnValue({ SheetNames: [], Sheets: {} });

      service.exportToExcel(mockData, mockColumns);

      expect(XLSX.utils.book_append_sheet).toHaveBeenCalledWith(
        expect.anything(),
        expect.anything(),
        'Sheet1'
      );
    });
  });

  describe('exportToCSV', () => {
    it('should export data to CSV format', () => {
      const mockBlob = new Blob(['test']);
      jest.spyOn(window, 'Blob').mockReturnValue(mockBlob);

      service.exportToCSV(mockData, mockColumns);

      expect(FileSaver.saveAs).toHaveBeenCalledWith(
        expect.any(Blob),
        expect.stringContaining('.csv')
      );
    });

    it('should handle empty data', () => {
      service.exportToCSV([], mockColumns);
      expect(FileSaver.saveAs).toHaveBeenCalled();
    });
  });

  describe('exportToPDF', () => {
    it('should export data to PDF format', () => {
      const mockPdfInstance = {
        internal: {
          pageSize: {
            getWidth: jest.fn().mockReturnValue(210),
            getHeight: jest.fn().mockReturnValue(297)
          }
        },
        setFontSize: jest.fn(),
        text: jest.fn(),
        getNumberOfPages: jest.fn().mockReturnValue(1),
        setPage: jest.fn(),
        save: jest.fn()
      };

      (jsPDF as unknown as jest.Mock).mockImplementation(() => mockPdfInstance);

      const options: ExportOptions = {
        fileName: 'test-pdf',
        title: 'Test Report',
        subtitle: 'Subtitle'
      };

      service.exportToPDF(mockData, mockColumns, options);

      expect(mockPdfInstance.setFontSize).toHaveBeenCalled();
      expect(mockPdfInstance.text).toHaveBeenCalled();
      expect(mockPdfInstance.save).toHaveBeenCalledWith(expect.stringContaining('.pdf'));
    });

    it('should handle landscape orientation', () => {
      const mockPdfInstance = {
        internal: {
          pageSize: {
            getWidth: jest.fn().mockReturnValue(297),
            getHeight: jest.fn().mockReturnValue(210)
          }
        },
        setFontSize: jest.fn(),
        text: jest.fn(),
        getNumberOfPages: jest.fn().mockReturnValue(1),
        setPage: jest.fn(),
        save: jest.fn()
      };

      (jsPDF as unknown as jest.Mock).mockImplementation(() => mockPdfInstance);

      const options: ExportOptions = {
        orientation: 'landscape'
      };

      service.exportToPDF(mockData, mockColumns, options);

      expect(jsPDF).toHaveBeenCalledWith(
        expect.objectContaining({
          orientation: 'landscape'
        })
      );
    });
  });

  describe('exportToJSON', () => {
    it('should export data to JSON format', () => {
      const mockBlob = new Blob(['test']);
      jest.spyOn(window, 'Blob').mockReturnValue(mockBlob);

      service.exportToJSON(mockData);

      expect(FileSaver.saveAs).toHaveBeenCalledWith(
        expect.any(Blob),
        expect.stringContaining('.json')
      );
    });
  });

  describe('validateExportData', () => {
    it('should validate export data successfully', () => {
      const result = service.validateExportData(mockData, mockColumns);
      expect(result.valid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('should return error for empty data', () => {
      const result = service.validateExportData([], mockColumns);
      expect(result.valid).toBe(false);
      expect(result.errors).toContain('Veri bulunamadı');
    });

    it('should return error for empty columns', () => {
      const result = service.validateExportData(mockData, []);
      expect(result.valid).toBe(false);
      expect(result.errors).toContain('Kolon tanımları bulunamadı');
    });

    it('should return error for invalid column definition', () => {
      const invalidColumns: ExportColumn[] = [
        { field: '', header: 'Test' }
      ];
      const result = service.validateExportData(mockData, invalidColumns);
      expect(result.valid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
    });
  });

  describe('exportMultipleSheets', () => {
    it('should export multiple sheets to Excel', () => {
      const sheets = [
        { data: mockData, columns: mockColumns, name: 'Sheet1' },
        { data: mockData, columns: mockColumns, name: 'Sheet2' }
      ];

      const mockWorkbook = { SheetNames: [], Sheets: {} };
      (XLSX.utils.book_new as jest.Mock).mockReturnValue(mockWorkbook);
      (XLSX.utils.json_to_sheet as jest.Mock).mockReturnValue({});

      service.exportMultipleSheets(sheets);

      expect(XLSX.utils.book_append_sheet).toHaveBeenCalledTimes(2);
      expect(XLSX.writeFile).toHaveBeenCalled();
    });
  });

  describe('Helper methods', () => {
    it('should format dates correctly', () => {
      const testData = [{ date: new Date('2024-01-01') }];
      const columns: ExportColumn[] = [
        { field: 'date', header: 'Date' }
      ];

      service.exportToCSV(testData, columns);

      expect(FileSaver.saveAs).toHaveBeenCalled();
    });

    it('should format boolean values', () => {
      const testData = [{ active: true }, { active: false }];
      const columns: ExportColumn[] = [
        { field: 'active', header: 'Active' }
      ];

      service.exportToCSV(testData, columns);

      expect(FileSaver.saveAs).toHaveBeenCalled();
    });

    it('should handle nested properties', () => {
      const nestedData = [
        { user: { name: 'John', details: { age: 30 } } }
      ];
      const columns: ExportColumn[] = [
        { field: 'user.name', header: 'Name' },
        { field: 'user.details.age', header: 'Age' }
      ];

      service.exportToCSV(nestedData, columns);

      expect(FileSaver.saveAs).toHaveBeenCalled();
    });

    it('should apply custom formatting', () => {
      const columns: ExportColumn[] = [
        {
          field: 'age',
          header: 'Age',
          format: (value) => `${value} years`
        }
      ];

      service.exportToCSV(mockData, columns);

      expect(FileSaver.saveAs).toHaveBeenCalled();
    });
  });

  describe('getExportPresets', () => {
    it('should return export presets', () => {
      const presets = service.getExportPresets();

      expect(presets).toHaveProperty('default');
      expect(presets).toHaveProperty('report');
      expect(presets).toHaveProperty('simple');
      expect(presets.default.includeDate).toBe(true);
    });
  });
});