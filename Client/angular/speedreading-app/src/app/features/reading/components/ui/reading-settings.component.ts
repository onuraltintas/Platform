import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReadingSettings, ReadingMode } from '../../../../shared/models/reading.models';

@Component({
  selector: 'app-reading-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reading-settings.component.html',
  styles: [`
    .reading-settings {
      padding: 20px;
      background: white;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      max-width: 300px;
    }

    h3 {
      margin: 0 0 16px 0;
      color: #374151;
    }

    .settings-group,
    .checkbox-group {
      margin-bottom: 16px;
    }

    label {
      display: block;
      margin-bottom: 4px;
      font-weight: 500;
      color: #374151;
    }

    select,
    input[type="range"],
    input[type="color"] {
      width: 100%;
      margin-bottom: 4px;
    }

    input[type="range"] {
      height: 6px;
    }

    span {
      font-size: 12px;
      color: #6b7280;
    }

    .checkbox-group label {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    input[type="checkbox"] {
      width: auto;
    }
  `]
})
export class ReadingSettingsComponent implements OnInit, OnDestroy {
  @Input() settings!: ReadingSettings;
  @Input() currentMode!: ReadingMode;

  @Output() settingsChange = new EventEmitter<ReadingSettings>();
  @Output() modeChange = new EventEmitter<ReadingMode>();

  localSettings!: ReadingSettings;
  currentSettings!: ReadingSettings;
  activeTab: string = 'general';
  showPreview: boolean = true;
  showSimple: boolean = true;

  // Available options
  availableModes = [
    {
      id: ReadingMode.CLASSIC,
      name: 'Klasik Okuma',
      description: 'Geleneksel okuma deneyimi',
      icon: 'bi-book'
    },
    {
      id: ReadingMode.RSVP,
      name: 'RSVP Okuma',
      description: 'Kelimeler tek tek merkezi noktada gösterilir',
      icon: 'bi-eye'
    },
    {
      id: ReadingMode.CHUNK,
      name: 'Grup Okuma',
      description: 'Kelimeler gruplar halinde gösterilir',
      icon: 'bi-layers'
    },
    {
      id: ReadingMode.GUIDED,
      name: 'Rehberli Okuma',
      description: 'Hareket eden vurgu çubuğu ile rehberli okuma',
      icon: 'bi-arrow-right'
    }
  ];

  backgroundPresets = ['#ffffff', '#f8fafc', '#f1f5f9', '#ecfdf5', '#fef7ff', '#fffbeb'];
  textPresets = ['#000000', '#374151', '#4b5563', '#6b7280', '#1f2937', '#111827'];
  highlightPresets = ['#3b82f6', '#ef4444', '#10b981', '#f59e0b', '#8b5cf6', '#06b6d4'];

  themePresets = [
    {
      name: 'Açık Tema',
      backgroundColor: '#ffffff',
      textColor: '#374151',
      highlightColor: '#3b82f6'
    },
    {
      name: 'Koyu Tema',
      backgroundColor: '#1f2937',
      textColor: '#f9fafb',
      highlightColor: '#60a5fa'
    },
    {
      name: 'Sepia',
      backgroundColor: '#fef7cd',
      textColor: '#78716c',
      highlightColor: '#92400e'
    },
    {
      name: 'Yüksek Kontrast',
      backgroundColor: '#000000',
      textColor: '#ffffff',
      highlightColor: '#fbbf24'
    }
  ];

  ngOnInit(): void {
    try {
      console.log('Reading Settings: Initializing component');
      
      if (!this.settings) {
        console.warn('Reading Settings: No settings provided, using defaults');
        this.settings = this.getDefaultSettings();
      }
      
      this.localSettings = { ...this.settings };
      this.currentSettings = { ...this.settings };
      // Basit/Gelişmiş seçimlerini kalıcı tut
      try {
        const pref = localStorage.getItem('reading_settings_simple');
        if (pref !== null) this.showSimple = pref === '1';
      } catch {}
      
      console.log('Reading Settings: Initialized successfully');
    } catch (error) {
      console.error('Reading Settings: Error during initialization', error);
      // Use safe defaults
      this.localSettings = this.getDefaultSettings();
      this.currentSettings = this.getDefaultSettings();
    }
  }

  ngOnDestroy(): void {
    try {
      console.log('Reading Settings: Component destroying, cleaning up resources');
      
      // Clear object references
      this.localSettings = {} as ReadingSettings;
      this.currentSettings = {} as ReadingSettings;
      
      // Reset state
      this.activeTab = 'general';
      this.showPreview = false;
      
    } catch (error) {
      console.error('Reading Settings: Error during component cleanup', error);
    }
  }


  // Tab management
  setActiveTab(tab: string): void {
    this.activeTab = tab;
  }

  toggleSimpleMode(): void {
    this.showSimple = !this.showSimple;
    try { localStorage.setItem('reading_settings_simple', this.showSimple ? '1' : '0'); } catch {}
  }

  closeSettings(): void {
    // Emit close event or handle closing
    console.log('Settings closed');
  }

  // Mode checking methods
  isChunkMode(): boolean {
    return this.currentMode === ReadingMode.CHUNK;
  }

  isRSVPMode(): boolean {
    return this.currentMode === ReadingMode.RSVP;
  }

  isGuidedMode(): boolean {
    return this.currentMode === ReadingMode.GUIDED;
  }

  isClassicMode(): boolean {
    return this.currentMode === ReadingMode.CLASSIC;
  }

  // General settings with error handling
  onWordsPerMinuteChange(event: any): void {
    try {
      if (!event?.target?.value || !this.currentSettings) return;
      
      const newSpeed = parseInt(event.target.value);
      if (isNaN(newSpeed)) {
        console.warn('Reading Settings: Invalid WPM value');
        return;
      }
      
      if (newSpeed >= 60 && newSpeed <= 1000) {
        this.currentSettings.wordsPerMinute = newSpeed;
        this.emitSettingsChange();
      } else {
        console.warn('Reading Settings: WPM value out of range (60-1000)');
      }
    } catch (error) {
      console.error('Reading Settings: Error changing WPM', error);
    }
  }

  onWordsPerMinuteNumberChange(event: any): void {
    try {
      if (!event?.target?.value || !this.currentSettings) return;
      
      const newSpeed = parseInt(event.target.value);
      if (isNaN(newSpeed)) {
        console.warn('Reading Settings: Invalid WPM number value');
        return;
      }
      
      if (newSpeed >= 60 && newSpeed <= 1000) {
        this.currentSettings.wordsPerMinute = newSpeed;
        this.emitSettingsChange();
      } else {
        console.warn('Reading Settings: WPM number value out of range (60-1000)');
      }
    } catch (error) {
      console.error('Reading Settings: Error changing WPM number', error);
    }
  }

  changeChunkSize(delta: number): void {
    try {
      if (!this.currentSettings) return;
      
      const newSize = Math.max(1, Math.min(7, this.currentSettings.chunkSize + delta));
      if (newSize !== this.currentSettings.chunkSize) {
        this.currentSettings.chunkSize = newSize;
        this.emitSettingsChange();
      }
    } catch (error) {
      console.error('Reading Settings: Error changing chunk size', error);
    }
  }

  selectMode(mode: ReadingMode): void {
    try {
      if (!mode) {
        console.warn('Reading Settings: Invalid mode selected');
        return;
      }
      
      this.modeChange.emit(mode);
    } catch (error) {
      console.error('Reading Settings: Error selecting mode', error);
    }
  }

  // Visual settings
  onFontSizeChange(event: any): void {
    this.currentSettings.fontSize = parseInt(event.target.value);
    this.emitSettingsChange();
  }

  onFontFamilyChange(event: any): void {
    this.currentSettings.fontFamily = event.target.value;
    this.emitSettingsChange();
  }

  onLineHeightChange(event: any): void {
    this.currentSettings.lineHeight = parseFloat(event.target.value);
    this.emitSettingsChange();
  }

  // Color settings
  onBackgroundColorChange(event: any): void {
    this.currentSettings.backgroundColor = event.target.value;
    this.emitSettingsChange();
  }

  onTextColorChange(event: any): void {
    this.currentSettings.textColor = event.target.value;
    this.emitSettingsChange();
  }

  onHighlightColorChange(event: any): void {
    this.currentSettings.highlightColor = event.target.value;
    this.emitSettingsChange();
  }

  setBackgroundColor(color: string): void {
    this.currentSettings.backgroundColor = color;
    this.emitSettingsChange();
  }

  setTextColor(color: string): void {
    this.currentSettings.textColor = color;
    this.emitSettingsChange();
  }

  setHighlightColor(color: string): void {
    this.currentSettings.highlightColor = color;
    this.emitSettingsChange();
  }

  // Theme presets
  applyTheme(theme: any): void {
    this.currentSettings.backgroundColor = theme.backgroundColor;
    this.currentSettings.textColor = theme.textColor;
    this.currentSettings.highlightColor = theme.highlightColor;
    this.emitSettingsChange();
  }

  isThemeActive(theme: any): boolean {
    return this.currentSettings.backgroundColor === theme.backgroundColor &&
           this.currentSettings.textColor === theme.textColor &&
           this.currentSettings.highlightColor === theme.highlightColor;
  }

  // Behavior settings
  onAutoStartChange(event: any): void {
    this.currentSettings.autoStart = event.target.checked;
    this.emitSettingsChange();
  }

  onAutoPauseChange(event: any): void {
    this.currentSettings.autoPause = event.target.checked;
    this.emitSettingsChange();
  }

  onShowProgressChange(event: any): void {
    this.currentSettings.showProgress = event.target.checked;
    this.emitSettingsChange();
  }

  onEnableSoundsChange(event: any): void {
    this.currentSettings.enableSounds = event.target.checked;
    this.emitSettingsChange();
  }

  onEnableSpeedModeChange(event: any): void {
    this.currentSettings.enableSpeedMode = event.target.checked;
    this.emitSettingsChange();
  }

  onEnableHighlightingChange(event: any): void {
    this.currentSettings.enableHighlighting = event.target.checked;
    this.emitSettingsChange();
  }

  onHighlightRangeChange(event: any): void {
    this.currentSettings.highlightRange = parseInt(event.target.value);
    this.emitSettingsChange();
  }

  // RSVP specific settings
  onRSVPFocusPointChange(event: any): void {
    this.currentSettings.rsvpFocusPoint = event.target.checked;
    this.emitSettingsChange();
  }

  onRSVPWordDurationChange(event: any): void {
    this.currentSettings.rsvpWordDuration = parseInt(event.target.value);
    this.emitSettingsChange();
  }

  // Chunk specific settings
  onChunkHighlightDurationChange(event: any): void {
    this.currentSettings.chunkHighlightDuration = parseInt(event.target.value);
    this.emitSettingsChange();
  }

  onChunkPauseDurationChange(event: any): void {
    this.currentSettings.chunkPauseDuration = parseInt(event.target.value);
    this.emitSettingsChange();
  }

  onShowContextChange(event: any): void {
    this.currentSettings.showContext = event.target.checked;
    this.emitSettingsChange();
  }

  onShowFocusPointChange(event: any): void {
    this.currentSettings.showFocusPoint = event.target.checked;
    this.emitSettingsChange();
  }

  // Guided specific settings
  onHighlighterSpeedChange(event: any): void {
    this.currentSettings.highlighterSpeed = parseInt(event.target.value);
    this.emitSettingsChange();
  }

  onHighlighterHeightChange(event: any): void {
    this.currentSettings.highlighterHeight = parseInt(event.target.value);
    this.emitSettingsChange();
  }

  onShowReadingGuideChange(event: any): void {
    this.currentSettings.showReadingGuide = event.target.checked;
    this.emitSettingsChange();
  }

  onShowFocusWindowChange(event: any): void {
    this.currentSettings.showFocusWindow = event.target.checked;
    this.emitSettingsChange();
  }

  onShowGuideLinesChange(event: any): void {
    this.currentSettings.showGuideLines = event.target.checked;
    this.emitSettingsChange();
  }

  // Settings actions
  resetToDefaults(): void {
    this.currentSettings = this.getDefaultSettings();
    this.emitSettingsChange();
  }

  loadPreset(): void {
    // Load preset from storage or server
    console.log('Load preset');
  }

  savePreset(): void {
    // Save current settings as preset
    console.log('Save preset');
  }

  applySettings(): void {
    this.localSettings = { ...this.currentSettings };
    this.emitSettingsChange();
  }

  // Preview methods
  getPreviewStyles(): any {
    return {
      'background-color': this.currentSettings.backgroundColor,
      'color': this.currentSettings.textColor,
      'font-family': this.currentSettings.fontFamily,
      'font-size': this.currentSettings.fontSize + 'px',
      'line-height': this.currentSettings.lineHeight.toString(),
      'padding': '16px',
      'border-radius': '4px',
      'border': '1px solid #e2e8f0'
    };
  }

  getHighlightPreviewStyles(): any {
    return {
      'background-color': this.currentSettings.highlightColor,
      'color': 'white',
      'padding': '2px 4px',
      'border-radius': '2px'
    };
  }

  // Helper methods
  private emitSettingsChange(): void {
    this.settingsChange.emit(this.currentSettings);
  }

  private getDefaultSettings(): ReadingSettings {
    return {
      wordsPerMinute: 250,
      chunkSize: 3,
      fontSize: 16,
      fontFamily: 'Inter, sans-serif',
      lineHeight: 1.6,
      backgroundColor: '#ffffff',
      textColor: '#333333',
      highlightColor: '#3b82f6',
      autoStart: false,
      autoPause: false,
      showProgress: true,
      enableSounds: false,
      rsvpFocusPoint: true,
      rsvpWordDuration: 240,
      chunkHighlightDuration: 800,
      chunkPauseDuration: 200,
      showContext: true,
      showFocusPoint: true,
      highlighterSpeed: 250,
      highlighterHeight: 2,
      showReadingGuide: true,
      showFocusWindow: false,
      showGuideLines: false,
      enableSpeedMode: true,
      enableHighlighting: true,
      highlightRange: 3
    };
  }

  // Legacy methods for compatibility
  onSettingsChange(): void {
    this.emitSettingsChange();
  }

  onModeChange(event: any): void {
    this.modeChange.emit(event.target.value as ReadingMode);
  }
}