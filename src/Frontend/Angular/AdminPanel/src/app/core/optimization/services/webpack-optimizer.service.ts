import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, from, of } from 'rxjs';
import { map, catchError, tap, switchMap } from 'rxjs/operators';

interface WebpackConfig {
  mode: 'development' | 'production';
  optimization: OptimizationConfig;
  splitChunks: SplitChunksConfig;
  performance: PerformanceConfig;
  resolve: ResolveConfig;
  module: ModuleConfig;
}

interface OptimizationConfig {
  minimize: boolean;
  usedExports: boolean;
  sideEffects: boolean;
  splitChunks: SplitChunksConfig;
  runtimeChunk: 'single' | 'multiple' | boolean;
  removeAvailableModules: boolean;
  removeEmptyChunks: boolean;
  mergeDuplicateChunks: boolean;
}

interface SplitChunksConfig {
  chunks: 'all' | 'async' | 'initial';
  minSize: number;
  maxSize: number;
  minChunks: number;
  maxAsyncRequests: number;
  maxInitialRequests: number;
  automaticNameDelimiter: string;
  cacheGroups: Record<string, CacheGroup>;
}

interface CacheGroup {
  test?: RegExp;
  name?: string;
  chunks?: 'all' | 'async' | 'initial';
  priority: number;
  reuseExistingChunk: boolean;
  enforce?: boolean;
  minSize?: number;
  maxSize?: number;
  minChunks?: number;
}

interface PerformanceConfig {
  hints: 'warning' | 'error' | false;
  maxAssetSize: number;
  maxEntrypointSize: number;
  assetFilter: (assetFilename: string) => boolean;
}

interface ResolveConfig {
  extensions: string[];
  alias: Record<string, string>;
  modules: string[];
  fallback: Record<string, string | false>;
}

interface ModuleConfig {
  rules: ModuleRule[];
}

interface ModuleRule {
  test: RegExp;
  use: string | string[] | LoaderConfig[];
  exclude?: RegExp;
  include?: RegExp;
}

interface LoaderConfig {
  loader: string;
  options?: any;
}

interface OptimizationRecommendation {
  category: 'splitChunks' | 'minification' | 'treeshaking' | 'caching' | 'performance';
  priority: 'high' | 'medium' | 'low';
  description: string;
  configChange: any;
  estimatedImprovement: string;
  implementation: string;
}

interface BundleAnalysis {
  totalSize: number;
  chunkCount: number;
  vendorSize: number;
  appSize: number;
  duplicateModules: string[];
  unusedModules: string[];
  largestModules: Array<{name: string, size: number}>;
  recommendations: OptimizationRecommendation[];
}

@Injectable({
  providedIn: 'root'
})
export class WebpackOptimizerService {
  private currentConfig = new BehaviorSubject<WebpackConfig>(this.getDefaultConfig());
  private bundleAnalysis = new BehaviorSubject<BundleAnalysis | null>(null);
  private optimizationRecommendations = new BehaviorSubject<OptimizationRecommendation[]>([]);

  public config$ = this.currentConfig.asObservable();
  public analysis$ = this.bundleAnalysis.asObservable();
  public recommendations$ = this.optimizationRecommendations.asObservable();

  constructor() {
    this.initializeAnalysis();
  }

  private getDefaultConfig(): WebpackConfig {
    return {
      mode: 'production',
      optimization: {
        minimize: true,
        usedExports: true,
        sideEffects: false,
        splitChunks: this.getDefaultSplitChunksConfig(),
        runtimeChunk: 'single',
        removeAvailableModules: true,
        removeEmptyChunks: true,
        mergeDuplicateChunks: true
      },
      splitChunks: this.getDefaultSplitChunksConfig(),
      performance: {
        hints: 'warning',
        maxAssetSize: 500 * 1024, // 500KB
        maxEntrypointSize: 500 * 1024, // 500KB
        assetFilter: (assetFilename: string) => {
          return !assetFilename.endsWith('.map');
        }
      },
      resolve: {
        extensions: ['.ts', '.js', '.json'],
        alias: {
          '@': 'src',
          '@app': 'src/app',
          '@core': 'src/app/core',
          '@shared': 'src/app/shared',
          '@features': 'src/app/features'
        },
        modules: ['node_modules'],
        fallback: {}
      },
      module: {
        rules: [
          {
            test: /\.ts$/,
            use: ['@ngtools/webpack'],
            exclude: /node_modules/
          },
          {
            test: /\.css$/,
            use: ['style-loader', 'css-loader']
          },
          {
            test: /\.scss$/,
            use: ['style-loader', 'css-loader', 'sass-loader']
          }
        ]
      }
    };
  }

  private getDefaultSplitChunksConfig(): SplitChunksConfig {
    return {
      chunks: 'all',
      minSize: 20000,
      maxSize: 244000,
      minChunks: 1,
      maxAsyncRequests: 30,
      maxInitialRequests: 30,
      automaticNameDelimiter: '-',
      cacheGroups: {
        vendor: {
          test: /[\\/]node_modules[\\/]/,
          name: 'vendors',
          priority: 10,
          reuseExistingChunk: true,
          chunks: 'all'
        },
        common: {
          name: 'common',
          minChunks: 2,
          priority: 5,
          reuseExistingChunk: true,
          chunks: 'all'
        },
        angular: {
          test: /[\\/]node_modules[\\/]@angular[\\/]/,
          name: 'angular',
          priority: 20,
          reuseExistingChunk: true,
          chunks: 'all'
        },
        primeng: {
          test: /[\\/]node_modules[\\/]primeng[\\/]/,
          name: 'primeng',
          priority: 15,
          reuseExistingChunk: true,
          chunks: 'all'
        },
        rxjs: {
          test: /[\\/]node_modules[\\/]rxjs[\\/]/,
          name: 'rxjs',
          priority: 15,
          reuseExistingChunk: true,
          chunks: 'all'
        }
      }
    };
  }

  private initializeAnalysis(): void {
    // Simulate bundle analysis
    setTimeout(() => {
      this.performBundleAnalysis();
    }, 1000);
  }

  private performBundleAnalysis(): void {
    // Simulate analyzing current bundle
    const mockAnalysis: BundleAnalysis = {
      totalSize: 2.5 * 1024 * 1024, // 2.5MB
      chunkCount: 8,
      vendorSize: 1.8 * 1024 * 1024, // 1.8MB
      appSize: 0.7 * 1024 * 1024, // 700KB
      duplicateModules: [
        'node_modules/moment/moment.js',
        'node_modules/lodash/lodash.js'
      ],
      unusedModules: [
        'node_modules/@angular/animations',
        'node_modules/chart.js/unused-components'
      ],
      largestModules: [
        { name: 'node_modules/primeng/primeng.js', size: 500 * 1024 },
        { name: 'node_modules/@angular/material/bundles', size: 300 * 1024 },
        { name: 'node_modules/moment/moment.js', size: 250 * 1024 },
        { name: 'node_modules/rxjs/bundles', size: 200 * 1024 },
        { name: 'src/app/features', size: 180 * 1024 }
      ],
      recommendations: []
    };

    this.bundleAnalysis.next(mockAnalysis);
    this.generateOptimizationRecommendations(mockAnalysis);
  }

  private generateOptimizationRecommendations(analysis: BundleAnalysis): void {
    const recommendations: OptimizationRecommendation[] = [];

    // Large vendor bundle recommendation
    if (analysis.vendorSize > 1.5 * 1024 * 1024) {
      recommendations.push({
        category: 'splitChunks',
        priority: 'high',
        description: 'Vendor bundle is too large - split into smaller chunks',
        configChange: {
          'optimization.splitChunks.cacheGroups.vendor.maxSize': 500 * 1024
        },
        estimatedImprovement: '30% faster initial load',
        implementation: `
splitChunks: {
  cacheGroups: {
    vendor: {
      maxSize: 500000, // 500KB
      test: /[\\/]node_modules[\\/]/,
      chunks: 'all'
    }
  }
}`
      });
    }

    // Duplicate modules recommendation
    if (analysis.duplicateModules.length > 0) {
      recommendations.push({
        category: 'splitChunks',
        priority: 'medium',
        description: 'Duplicate modules detected - optimize chunk splitting',
        configChange: {
          'optimization.splitChunks.cacheGroups.commons.minChunks': 2,
          'optimization.splitChunks.cacheGroups.commons.enforce': true
        },
        estimatedImprovement: '15% bundle size reduction',
        implementation: `
cacheGroups: {
  commons: {
    minChunks: 2,
    enforce: true,
    priority: 5
  }
}`
      });
    }

    // Tree shaking recommendation
    if (analysis.unusedModules.length > 0) {
      recommendations.push({
        category: 'treeshaking',
        priority: 'high',
        description: 'Unused modules detected - enable better tree shaking',
        configChange: {
          'optimization.usedExports': true,
          'optimization.sideEffects': false
        },
        estimatedImprovement: '20% bundle size reduction',
        implementation: `
optimization: {
  usedExports: true,
  sideEffects: false
}`
      });
    }

    // Performance budget recommendation
    if (analysis.totalSize > 3 * 1024 * 1024) {
      recommendations.push({
        category: 'performance',
        priority: 'high',
        description: 'Total bundle size exceeds recommended limits',
        configChange: {
          'performance.maxAssetSize': 300 * 1024,
          'performance.maxEntrypointSize': 300 * 1024,
          'performance.hints': 'error'
        },
        estimatedImprovement: 'Enforce size limits',
        implementation: `
performance: {
  maxAssetSize: 300000,
  maxEntrypointSize: 300000,
  hints: 'error'
}`
      });
    }

    // Minification recommendation
    recommendations.push({
      category: 'minification',
      priority: 'medium',
      description: 'Optimize minification settings for better compression',
      configChange: {
        'optimization.minimize': true,
        'optimization.minimizer': [
          {
            terserOptions: {
              compress: {
                drop_console: true,
                drop_debugger: true,
                pure_funcs: ['console.log']
              }
            }
          }
        ]
      },
      estimatedImprovement: '10% bundle size reduction',
      implementation: `
optimization: {
  minimizer: [
    new TerserPlugin({
      terserOptions: {
        compress: {
          drop_console: true,
          drop_debugger: true
        }
      }
    })
  ]
}`
    });

    this.optimizationRecommendations.next(recommendations);
  }

  public applyOptimization(recommendation: OptimizationRecommendation): Observable<boolean> {
    return from(this.updateConfig(recommendation.configChange)).pipe(
      tap(() => {
        console.log(`Applied optimization: ${recommendation.description}`);
        this.performBundleAnalysis(); // Re-analyze after changes
      }),
      map(() => true),
      catchError(error => {
        console.error('Failed to apply optimization:', error);
        return of(false);
      })
    );
  }

  private async updateConfig(changes: any): Promise<void> {
    const currentConfig = this.currentConfig.value;
    const updatedConfig = this.deepMerge(currentConfig, changes);
    this.currentConfig.next(updatedConfig);
  }

  private deepMerge(target: any, source: any): any {
    const result = { ...target };

    for (const key in source) {
      if (source[key] && typeof source[key] === 'object' && !Array.isArray(source[key])) {
        result[key] = this.deepMerge(result[key] || {}, source[key]);
      } else {
        result[key] = source[key];
      }
    }

    return result;
  }

  public generateOptimizedConfig(): Observable<string> {
    return this.config$.pipe(
      map(config => {
        const optimizedConfig = {
          ...config,
          optimization: {
            ...config.optimization,
            splitChunks: {
              ...config.splitChunks,
              cacheGroups: {
                ...config.splitChunks.cacheGroups,
                // Add optimized cache groups
                angular: {
                  test: /[\\/]node_modules[\\/]@angular[\\/]/,
                  name: 'angular',
                  chunks: 'all' as const,
                  priority: 20,
                  reuseExistingChunk: true,
                  maxSize: 400 * 1024
                },
                vendor: {
                  test: /[\\/]node_modules[\\/]/,
                  name: 'vendors',
                  chunks: 'all' as const,
                  priority: 10,
                  reuseExistingChunk: true,
                  maxSize: 500 * 1024,
                  exclude: /[\\/]node_modules[\\/]@angular[\\/]/
                }
              }
            }
          }
        };

        return this.formatConfigAsCode(optimizedConfig);
      })
    );
  }

  private formatConfigAsCode(config: WebpackConfig): string {
    return `
// Optimized Webpack Configuration
// Generated by WebpackOptimizerService

const path = require('path');

module.exports = {
  mode: '${config.mode}',

  optimization: {
    minimize: ${config.optimization.minimize},
    usedExports: ${config.optimization.usedExports},
    sideEffects: ${config.optimization.sideEffects},
    runtimeChunk: '${config.optimization.runtimeChunk}',
    removeAvailableModules: ${config.optimization.removeAvailableModules},
    removeEmptyChunks: ${config.optimization.removeEmptyChunks},
    mergeDuplicateChunks: ${config.optimization.mergeDuplicateChunks},

    splitChunks: {
      chunks: '${config.splitChunks.chunks}',
      minSize: ${config.splitChunks.minSize},
      maxSize: ${config.splitChunks.maxSize},
      minChunks: ${config.splitChunks.minChunks},
      maxAsyncRequests: ${config.splitChunks.maxAsyncRequests},
      maxInitialRequests: ${config.splitChunks.maxInitialRequests},
      automaticNameDelimiter: '${config.splitChunks.automaticNameDelimiter}',

      cacheGroups: {
        ${this.formatCacheGroups(config.splitChunks.cacheGroups)}
      }
    }
  },

  performance: {
    hints: ${config.performance.hints === false ? 'false' : `'${config.performance.hints}'`},
    maxAssetSize: ${config.performance.maxAssetSize},
    maxEntrypointSize: ${config.performance.maxEntrypointSize}
  },

  resolve: {
    extensions: [${config.resolve.extensions.map(ext => `'${ext}'`).join(', ')}],
    alias: {
      ${Object.entries(config.resolve.alias)
        .map(([key, value]) => `'${key}': path.resolve(__dirname, '${value}')`)
        .join(',\n      ')}
    }
  },

  module: {
    rules: [
      ${config.module.rules.map(rule => this.formatModuleRule(rule)).join(',\n      ')}
    ]
  }
};`;
  }

  private formatCacheGroups(cacheGroups: Record<string, CacheGroup>): string {
    return Object.entries(cacheGroups)
      .map(([name, group]) => {
        return `${name}: {
          ${group.test ? `test: ${group.test.toString()},` : ''}
          ${group.name ? `name: '${group.name}',` : ''}
          chunks: '${group.chunks || "all"}',
          priority: ${group.priority},
          reuseExistingChunk: ${group.reuseExistingChunk}${group.maxSize ? `,\n          maxSize: ${group.maxSize}` : ''}
        }`;
      })
      .join(',\n        ');
  }

  private formatModuleRule(rule: ModuleRule): string {
    return `{
        test: ${rule.test.toString()},
        use: ${Array.isArray(rule.use) ?
          `[${rule.use.map(u => typeof u === 'string' ? `'${u}'` : JSON.stringify(u)).join(', ')}]` :
          `'${rule.use}'`}${rule.exclude ? `,\n        exclude: ${rule.exclude.toString()}` : ''}
      }`;
  }

  public analyzeChunkOptimization(): Observable<any> {
    return this.analysis$.pipe(
      switchMap(analysis => {
        if (!analysis) return of(null);

        const chunkOptimization = {
          currentChunks: analysis.chunkCount,
          recommendedChunks: this.calculateOptimalChunkCount(analysis),
          vendorSplitting: {
            current: 'single-vendor',
            recommended: 'multi-vendor',
            reason: 'Large vendor bundle should be split by library type'
          },
          lazyLoading: {
            currentModules: 0,
            potentialModules: 5,
            estimatedSavings: '40% initial bundle size'
          },
          duplicateElimination: {
            duplicates: analysis.duplicateModules.length,
            potentialSavings: '200KB',
            action: 'Configure commons chunk with minChunks: 2'
          }
        };

        return of(chunkOptimization);
      })
    );
  }

  private calculateOptimalChunkCount(analysis: BundleAnalysis): number {
    // Rule of thumb: aim for 200-300KB per chunk
    const targetChunkSize = 250 * 1024; // 250KB
    return Math.ceil(analysis.totalSize / targetChunkSize);
  }

  public exportWebpackConfig(): Observable<string> {
    return this.generateOptimizedConfig();
  }

  public validateConfig(config: Partial<WebpackConfig>): Observable<{valid: boolean, errors: string[]}> {
    const errors: string[] = [];

    // Validate splitChunks configuration
    if (config.splitChunks) {
      if (config.splitChunks.maxAsyncRequests && config.splitChunks.maxAsyncRequests > 50) {
        errors.push('maxAsyncRequests too high - may cause performance issues');
      }

      if (config.splitChunks.minSize && config.splitChunks.minSize < 10000) {
        errors.push('minSize too small - may create too many small chunks');
      }
    }

    // Validate performance configuration
    if (config.performance) {
      if (config.performance.maxAssetSize && config.performance.maxAssetSize > 1024 * 1024) {
        errors.push('maxAssetSize too large - consider reducing for better performance');
      }
    }

    return of({
      valid: errors.length === 0,
      errors
    });
  }

  public getBundleOptimizationReport(): Observable<any> {
    return this.analysis$.pipe(
      switchMap(analysis => {
        if (!analysis) return of(null);

        const report = {
          summary: {
            totalSize: this.formatBytes(analysis.totalSize),
            chunkCount: analysis.chunkCount,
            optimizationScore: this.calculateOptimizationScore(analysis),
            recommendations: this.optimizationRecommendations.value.length
          },
          breakdown: {
            vendor: this.formatBytes(analysis.vendorSize),
            application: this.formatBytes(analysis.appSize),
            largest: analysis.largestModules.map(m => ({
              name: m.name,
              size: this.formatBytes(m.size)
            }))
          },
          issues: {
            duplicates: analysis.duplicateModules.length,
            unused: analysis.unusedModules.length,
            oversized: analysis.largestModules.filter(m => m.size > 300 * 1024).length
          },
          improvements: this.optimizationRecommendations.value.map(rec => ({
            category: rec.category,
            priority: rec.priority,
            description: rec.description,
            impact: rec.estimatedImprovement
          }))
        };

        return of(report);
      })
    );
  }

  private calculateOptimizationScore(analysis: BundleAnalysis): number {
    let score = 100;

    // Deduct points for large bundles
    if (analysis.totalSize > 3 * 1024 * 1024) score -= 20;
    if (analysis.vendorSize > 1.5 * 1024 * 1024) score -= 15;

    // Deduct points for duplicates and unused modules
    score -= analysis.duplicateModules.length * 5;
    score -= analysis.unusedModules.length * 3;

    // Deduct points for too many or too few chunks
    const optimalChunks = this.calculateOptimalChunkCount(analysis);
    if (Math.abs(analysis.chunkCount - optimalChunks) > 2) score -= 10;

    return Math.max(0, score);
  }

  private formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  public destroy(): void {
    // Cleanup if needed
  }
}