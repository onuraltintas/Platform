import { NgModule, APP_INITIALIZER } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

// Import all optimization services
import { BundleAnalyzerService } from './services/bundle-analyzer.service';
import { BundleSizeBudgetService } from './services/bundle-size-budget.service';
import { CodeSplittingStrategyService } from './services/code-splitting-strategy.service';
import { DynamicImportsOptimizerService } from './services/dynamic-imports-optimizer.service';
import { LazyLoadingOptimizerService } from './services/lazy-loading-optimizer.service';
import { TreeShakingOptimizerService } from './services/tree-shaking-optimizer.service';
import { UnusedCodeDetectorService } from './services/unused-code-detector.service';
import { WebpackOptimizerService } from './services/webpack-optimizer.service';

/**
 * Optimization initialization factory
 * Initializes all optimization services on application startup
 */
export function optimizationInitializerFactory(
  bundleAnalyzer: BundleAnalyzerService,
  budgetService: BundleSizeBudgetService,
  codeSplitting: CodeSplittingStrategyService,
  _dynamicImports: DynamicImportsOptimizerService,
  lazyLoading: LazyLoadingOptimizerService,
  treeShaking: TreeShakingOptimizerService,
  _unusedCode: UnusedCodeDetectorService,
  _webpackOptimizer: WebpackOptimizerService
) {
  return (): Promise<void> => {
    console.log('🚀 Initializing Bundle Optimization System...');

    return Promise.all([
      // Initialize bundle analysis
      bundleAnalyzer.runBundleAnalysis(),

      // Start performance monitoring
      new Promise<void>(resolve => {
        budgetService.getBudgetStatus().subscribe(() => {
          console.log('📊 Budget monitoring started');
          resolve();
        });
      }),

      // Initialize predictive loading
      new Promise<void>(resolve => {
        codeSplitting.initializePredictiveLoading().subscribe(() => {
          console.log('🔮 Predictive loading initialized');
          resolve();
        });
      }),

      // Start tree shaking analysis
      new Promise<void>(resolve => {
        treeShaking.analyzeProject().subscribe(() => {
          console.log('🌳 Tree shaking analysis completed');
          resolve();
        });
      }),

      // Initialize lazy loading optimization
      new Promise<void>(resolve => {
        lazyLoading.optimizeRoutePreloading().subscribe(() => {
          console.log('⚡ Lazy loading optimization applied');
          resolve();
        });
      })

    ]).then(() => {
      console.log('✅ Bundle Optimization System initialized successfully');
    }).catch((error) => {
      console.error('❌ Bundle Optimization System initialization failed:', error);
    });
  };
}

/**
 * Optimization Module
 * Provides comprehensive bundle optimization services
 */
@NgModule({
  imports: [
    CommonModule,
    RouterModule
  ],
  providers: [
    // Core optimization services
    BundleAnalyzerService,
    BundleSizeBudgetService,
    CodeSplittingStrategyService,
    DynamicImportsOptimizerService,
    LazyLoadingOptimizerService,
    TreeShakingOptimizerService,
    UnusedCodeDetectorService,
    WebpackOptimizerService,

    // Initialize optimization system on app startup
    {
      provide: APP_INITIALIZER,
      useFactory: optimizationInitializerFactory,
      deps: [
        BundleAnalyzerService,
        BundleSizeBudgetService,
        CodeSplittingStrategyService,
        DynamicImportsOptimizerService,
        LazyLoadingOptimizerService,
        TreeShakingOptimizerService,
        UnusedCodeDetectorService,
        WebpackOptimizerService
      ],
      multi: true
    }
  ]
})
export class OptimizationModule {
  constructor(
    private bundleAnalyzer: BundleAnalyzerService,
    private budgetService: BundleSizeBudgetService,
    private codeSplitting: CodeSplittingStrategyService,
    private treeShaking: TreeShakingOptimizerService
  ) {
    this.setupOptimizationIntegration();
  }

  /**
   * Setup integration between optimization services
   */
  private setupOptimizationIntegration(): void {
    // Bundle analyzer triggers other optimizations
    this.bundleAnalyzer.getBundleAnalysis().subscribe(analysis => {
      // Trigger tree shaking if bundle is too large
      if (analysis.totalSize > 1024 * 1024) { // 1MB
        this.treeShaking.analyzeProject().subscribe();
      }

      // Update budget thresholds based on analysis
      if (analysis.recommendations && analysis.recommendations.length > 0) {
        this.budgetService.setBudget('main', {
          maxSize: analysis.totalSize * 0.8 // Set budget to 80% of current size
        }).subscribe();
      }
    });

    // Code splitting integration with budget monitoring
    this.budgetService.violations$.subscribe(violations => {
      if (violations.length > 0) {
        this.codeSplitting.initializePredictiveLoading().subscribe();
      }
    });

    // Performance monitoring integration
    this.setupPerformanceMonitoring();
  }

  /**
   * Setup performance monitoring integration
   */
  private setupPerformanceMonitoring(): void {
    // Monitor optimization effectiveness every 30 seconds
    setInterval(() => {
      this.logOptimizationStatus();
    }, 30000);
  }

  /**
   * Log optimization system status
   */
  private logOptimizationStatus(): void {
    Promise.all([
      this.bundleAnalyzer.getBundleAnalysis().toPromise(),
      this.budgetService.getBudgetStatus().toPromise(),
      this.treeShaking.getOptimizationReport().toPromise()
    ]).then(([analysis, budget, treeShaking]) => {
      if (analysis && budget && treeShaking) {
        console.log('📈 Optimization Status:', {
          bundleSize: `${(analysis.totalSize / 1024).toFixed(1)}KB`,
          budgetUtilization: `${budget.utilizationPercentage.toFixed(1)}%`,
          treeShakingEfficiency: `${treeShaking.optimizationEfficiency.toFixed(1)}%`,
          recommendations: analysis.recommendations?.length || 0
        });
      }
    }).catch(error => {
      console.error('Failed to get optimization status:', error);
    });
  }
}