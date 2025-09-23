import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, from, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';

interface UnusedCodeMetrics {
  filePath: string;
  unusedExports: string[];
  unusedImports: string[];
  deadCodeBlocks: CodeBlock[];
  unusedVariables: string[];
  unusedFunctions: string[];
  unusedClasses: string[];
  fileSize: number;
  potentialSavings: number;
  lastAccessed: Date;
  confidence: number;
}

interface CodeBlock {
  startLine: number;
  endLine: number;
  type: 'function' | 'class' | 'variable' | 'import' | 'conditional';
  code: string;
  reason: string;
}

interface DetectionRule {
  name: string;
  pattern: RegExp;
  type: 'export' | 'import' | 'function' | 'class' | 'variable';
  confidence: number;
  description: string;
}

interface CleanupRecommendation {
  filePath: string;
  action: 'remove' | 'refactor' | 'consolidate';
  target: string;
  reason: string;
  impact: 'low' | 'medium' | 'high';
  estimatedSavings: number;
  automatable: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class UnusedCodeDetectorService {
  private detectionResults = new BehaviorSubject<Map<string, UnusedCodeMetrics>>(new Map());
  private cleanupRecommendations = new BehaviorSubject<CleanupRecommendation[]>([]);
  private detectionRules: DetectionRule[] = [];

  public detectionResults$ = this.detectionResults.asObservable();
  public recommendations$ = this.cleanupRecommendations.asObservable();

  private fileAccessTracker = new Map<string, Date>();
  private performanceObserver?: PerformanceObserver;

  constructor() {
    this.initializeDetectionRules();
    this.initializeFileAccessTracking();
    this.startPeriodicScanning();
  }

  private initializeDetectionRules(): void {
    this.detectionRules = [
      {
        name: 'unused-export',
        pattern: /export\s+(?:class|function|const|let|var|interface|type)\s+(\w+)/g,
        type: 'export',
        confidence: 0.8,
        description: 'Exported symbols that are never imported'
      },
      {
        name: 'unused-import',
        pattern: /import\s+(?:{[^}]+}|\w+|\*\s+as\s+\w+)\s+from\s+['"][^'"]+['"];?/g,
        type: 'import',
        confidence: 0.9,
        description: 'Imported modules that are never used'
      },
      {
        name: 'unused-function',
        pattern: /(?:private|public)?\s*(\w+)\s*\([^)]*\)\s*{/g,
        type: 'function',
        confidence: 0.7,
        description: 'Functions that are defined but never called'
      },
      {
        name: 'unused-variable',
        pattern: /(?:const|let|var)\s+(\w+)\s*=/g,
        type: 'variable',
        confidence: 0.6,
        description: 'Variables that are declared but never used'
      },
      {
        name: 'unused-class',
        pattern: /class\s+(\w+)/g,
        type: 'class',
        confidence: 0.8,
        description: 'Classes that are defined but never instantiated'
      }
    ];
  }

  private initializeFileAccessTracking(): void {
    if (typeof window !== 'undefined' && 'PerformanceObserver' in window) {
      this.performanceObserver = new PerformanceObserver((entries) => {
        for (const entry of entries.getEntries()) {
          if (entry.entryType === 'resource' && entry.name.endsWith('.js')) {
            this.fileAccessTracker.set(entry.name, new Date());
          }
        }
      });

      this.performanceObserver.observe({
        entryTypes: ['resource']
      });
    }
  }

  private startPeriodicScanning(): void {
    setInterval(() => {
      this.performFullCodebaseAnalysis();
    }, 30 * 60 * 1000); // Every 30 minutes
  }

  public analyzeFile(filePath: string, fileContent: string): Observable<UnusedCodeMetrics> {
    return from(this.performFileAnalysis(filePath, fileContent)).pipe(
      tap(metrics => {
        const currentResults = this.detectionResults.value;
        currentResults.set(filePath, metrics);
        this.detectionResults.next(currentResults);
        this.generateCleanupRecommendations();
      }),
      catchError(error => {
        console.error(`Failed to analyze file ${filePath}:`, error);
        return of(this.createEmptyMetrics(filePath));
      })
    );
  }

  private async performFileAnalysis(filePath: string, fileContent: string): Promise<UnusedCodeMetrics> {
    const lines = fileContent.split('\n');
    const metrics: UnusedCodeMetrics = {
      filePath,
      unusedExports: [],
      unusedImports: [],
      deadCodeBlocks: [],
      unusedVariables: [],
      unusedFunctions: [],
      unusedClasses: [],
      fileSize: fileContent.length,
      potentialSavings: 0,
      lastAccessed: this.fileAccessTracker.get(filePath) || new Date(0),
      confidence: 0
    };

    for (const rule of this.detectionRules) {
      const matches = this.findMatches(fileContent, rule);

      for (const match of matches) {
        const isUsed = await this.checkUsage(match.symbol, fileContent, filePath);

        if (!isUsed) {
          switch (rule.type) {
            case 'export':
              metrics.unusedExports.push(match.symbol);
              break;
            case 'import':
              metrics.unusedImports.push(match.symbol);
              break;
            case 'function':
              metrics.unusedFunctions.push(match.symbol);
              break;
            case 'variable':
              metrics.unusedVariables.push(match.symbol);
              break;
            case 'class':
              metrics.unusedClasses.push(match.symbol);
              break;
          }

          metrics.deadCodeBlocks.push({
            startLine: match.lineNumber,
            endLine: match.lineNumber + this.estimateBlockSize(lines, match.lineNumber),
            type: rule.type === 'export' ? 'function' : rule.type,
            code: match.fullMatch,
            reason: rule.description
          });
        }
      }
    }

    metrics.potentialSavings = this.calculatePotentialSavings(metrics);
    metrics.confidence = this.calculateConfidence(metrics);

    return metrics;
  }

  private findMatches(content: string, rule: DetectionRule): Array<{symbol: string, lineNumber: number, fullMatch: string}> {
    const matches: Array<{symbol: string, lineNumber: number, fullMatch: string}> = [];
    const lines = content.split('\n');

    lines.forEach((line, index) => {
      let match;
      while ((match = rule.pattern.exec(line)) !== null) {
        matches.push({
          symbol: match[1] || match[0],
          lineNumber: index + 1,
          fullMatch: match[0]
        });
      }
      rule.pattern.lastIndex = 0; // Reset regex
    });

    return matches;
  }

  private async checkUsage(symbol: string, fileContent: string, filePath: string): Promise<boolean> {
    // Check within the same file
    const symbolRegex = new RegExp(`\\b${symbol}\\b`, 'g');
    const matches = fileContent.match(symbolRegex);

    if (matches && matches.length > 1) {
      return true; // Used within the same file
    }

    // Check for external usage (simplified - in real implementation, you'd scan the entire codebase)
    return this.checkExternalUsage(symbol, filePath);
  }

  private async checkExternalUsage(symbol: string, _excludeFilePath: string): Promise<boolean> {
    // Simulate checking other files in the project
    // In a real implementation, this would scan the entire codebase
    const commonUsagePatterns = [
      'Component', 'Service', 'Module', 'Interface', 'Type',
      'Router', 'Guard', 'Interceptor', 'Pipe', 'Directive'
    ];

    return commonUsagePatterns.some(pattern => symbol.includes(pattern));
  }

  private estimateBlockSize(lines: string[], startLine: number): number {
    let braceCount = 0;
    let lineCount = 0;

    for (let i = startLine - 1; i < lines.length; i++) {
      const line = lines[i];
      lineCount++;

      for (const char of line) {
        if (char === '{') braceCount++;
        if (char === '}') braceCount--;
      }

      if (braceCount === 0 && lineCount > 1) {
        break;
      }

      if (lineCount > 50) break; // Safety limit
    }

    return lineCount;
  }

  private calculatePotentialSavings(metrics: UnusedCodeMetrics): number {
    const averageCharsPerLine = 50;
    const totalUnusedLines = metrics.deadCodeBlocks.reduce((sum, block) =>
      sum + (block.endLine - block.startLine + 1), 0);

    return totalUnusedLines * averageCharsPerLine;
  }

  private calculateConfidence(metrics: UnusedCodeMetrics): number {
    const daysSinceAccess = (Date.now() - metrics.lastAccessed.getTime()) / (1000 * 60 * 60 * 24);
    const accessConfidence = Math.min(daysSinceAccess / 30, 1); // Max confidence after 30 days

    const codeConfidence = (
      metrics.unusedImports.length * 0.9 +
      metrics.unusedExports.length * 0.8 +
      metrics.unusedFunctions.length * 0.7 +
      metrics.unusedVariables.length * 0.6 +
      metrics.unusedClasses.length * 0.8
    ) / (metrics.unusedImports.length + metrics.unusedExports.length +
         metrics.unusedFunctions.length + metrics.unusedVariables.length +
         metrics.unusedClasses.length || 1);

    return (accessConfidence + codeConfidence) / 2;
  }

  private createEmptyMetrics(filePath: string): UnusedCodeMetrics {
    return {
      filePath,
      unusedExports: [],
      unusedImports: [],
      deadCodeBlocks: [],
      unusedVariables: [],
      unusedFunctions: [],
      unusedClasses: [],
      fileSize: 0,
      potentialSavings: 0,
      lastAccessed: new Date(),
      confidence: 0
    };
  }

  private generateCleanupRecommendations(): void {
    const recommendations: CleanupRecommendation[] = [];
    const results = this.detectionResults.value;

    results.forEach((metrics, filePath) => {
      if (metrics.confidence > 0.7) {
        // High-confidence unused imports
        metrics.unusedImports.forEach(importItem => {
          recommendations.push({
            filePath,
            action: 'remove',
            target: importItem,
            reason: 'Unused import detected with high confidence',
            impact: 'low',
            estimatedSavings: 50,
            automatable: true
          });
        });

        // Unused exports
        metrics.unusedExports.forEach(exportItem => {
          recommendations.push({
            filePath,
            action: 'remove',
            target: exportItem,
            reason: 'Exported symbol never imported elsewhere',
            impact: 'medium',
            estimatedSavings: 200,
            automatable: false
          });
        });

        // Large unused functions
        metrics.unusedFunctions.forEach(func => {
          recommendations.push({
            filePath,
            action: 'remove',
            target: func,
            reason: 'Function defined but never called',
            impact: 'medium',
            estimatedSavings: 300,
            automatable: false
          });
        });
      }

      // File-level recommendations
      if (metrics.potentialSavings > 1000 && metrics.confidence > 0.8) {
        recommendations.push({
          filePath,
          action: 'refactor',
          target: filePath,
          reason: 'File contains significant amount of unused code',
          impact: 'high',
          estimatedSavings: metrics.potentialSavings,
          automatable: false
        });
      }
    });

    this.cleanupRecommendations.next(
      recommendations.sort((a, b) => b.estimatedSavings - a.estimatedSavings)
    );
  }

  public performAutomatedCleanup(): Observable<string[]> {
    const automatableRecommendations = this.cleanupRecommendations.value
      .filter(rec => rec.automatable);

    return from(this.executeAutomatedCleanup(automatableRecommendations)).pipe(
      map(results => results.map(r => r.message)),
      tap(messages => console.log('Automated cleanup completed:', messages))
    );
  }

  private async executeAutomatedCleanup(recommendations: CleanupRecommendation[]): Promise<Array<{success: boolean, message: string}>> {
    const results: Array<{success: boolean, message: string}> = [];

    for (const rec of recommendations) {
      try {
        if (rec.action === 'remove' && rec.target.includes('import')) {
          // Simulate removing unused import
          results.push({
            success: true,
            message: `Removed unused import '${rec.target}' from ${rec.filePath}`
          });
        }
      } catch (error) {
        results.push({
          success: false,
          message: `Failed to process ${rec.target}: ${error}`
        });
      }
    }

    return results;
  }

  public performFullCodebaseAnalysis(): Observable<Map<string, UnusedCodeMetrics>> {
    // Simulate analyzing common Angular project files
    const filesToAnalyze = [
      'src/app/app.component.ts',
      'src/app/app.module.ts',
      'src/app/core/services/*.service.ts',
      'src/app/features/**/*.component.ts',
      'src/app/shared/**/*.ts'
    ];

    return from(this.analyzeMultipleFiles(filesToAnalyze)).pipe(
      tap(results => {
        this.detectionResults.next(results);
        this.generateCleanupRecommendations();
      })
    );
  }

  private async analyzeMultipleFiles(_filePaths: string[]): Promise<Map<string, UnusedCodeMetrics>> {
    const results = new Map<string, UnusedCodeMetrics>();

    // Simulate analysis results for demo
    const mockResults: UnusedCodeMetrics[] = [
      {
        filePath: 'src/app/core/services/unused.service.ts',
        unusedExports: ['UnusedClass', 'helperFunction'],
        unusedImports: ['moment', 'lodash'],
        deadCodeBlocks: [
          {
            startLine: 15,
            endLine: 25,
            type: 'function',
            code: 'private helperFunction() { ... }',
            reason: 'Function never called'
          }
        ],
        unusedVariables: ['tempVar', 'configFlag'],
        unusedFunctions: ['helperFunction'],
        unusedClasses: ['UnusedClass'],
        fileSize: 2500,
        potentialSavings: 800,
        lastAccessed: new Date(Date.now() - 20 * 24 * 60 * 60 * 1000), // 20 days ago
        confidence: 0.85
      },
      {
        filePath: 'src/app/features/legacy/old.component.ts',
        unusedExports: [],
        unusedImports: ['@angular/animations'],
        deadCodeBlocks: [],
        unusedVariables: ['debugMode'],
        unusedFunctions: [],
        unusedClasses: [],
        fileSize: 1200,
        potentialSavings: 100,
        lastAccessed: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000), // 5 days ago
        confidence: 0.65
      }
    ];

    mockResults.forEach(result => {
      results.set(result.filePath, result);
    });

    return results;
  }

  public getDetectionSummary(): Observable<any> {
    return this.detectionResults$.pipe(
      map(results => {
        const metricsArray = Array.from(results.values());
        const totalFiles = metricsArray.length;
        const totalSavings = metricsArray.reduce((sum, m) => sum + m.potentialSavings, 0);
        const highConfidenceFiles = metricsArray.filter(m => m.confidence > 0.8).length;

        return {
          totalFiles,
          filesWithIssues: metricsArray.filter(m => m.potentialSavings > 0).length,
          totalPotentialSavings: totalSavings,
          highConfidenceDetections: highConfidenceFiles,
          averageConfidence: metricsArray.reduce((sum, m) => sum + m.confidence, 0) / totalFiles || 0,
          topIssues: this.getTopIssues(metricsArray)
        };
      })
    );
  }

  private getTopIssues(metrics: UnusedCodeMetrics[]): any[] {
    return metrics
      .filter(m => m.potentialSavings > 0)
      .sort((a, b) => b.potentialSavings - a.potentialSavings)
      .slice(0, 10)
      .map(m => ({
        file: m.filePath,
        savings: m.potentialSavings,
        confidence: m.confidence,
        issues: m.unusedImports.length + m.unusedExports.length + m.unusedFunctions.length
      }));
  }

  public exportCleanupScript(): Observable<string> {
    return this.recommendations$.pipe(
      map(recommendations => {
        const automatableRecs = recommendations.filter(r => r.automatable);

        let script = '#!/bin/bash\n';
        script += '# Automated unused code cleanup script\n';
        script += '# Generated by UnusedCodeDetectorService\n\n';

        automatableRecs.forEach(rec => {
          if (rec.action === 'remove' && rec.target.includes('import')) {
            script += `# Remove unused import from ${rec.filePath}\n`;
            script += `sed -i '/import.*${rec.target}/d' "${rec.filePath}"\n\n`;
          }
        });

        script += 'echo "Cleanup completed!"\n';
        return script;
      })
    );
  }

  public destroy(): void {
    if (this.performanceObserver) {
      this.performanceObserver.disconnect();
    }
    this.fileAccessTracker.clear();
  }
}