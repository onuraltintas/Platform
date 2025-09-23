import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, timer, combineLatest } from 'rxjs';
import { map, debounceTime } from 'rxjs/operators';
import { PerformanceAnalyticsService } from './performance-analytics.service';
import { MemoryMonitorService } from './memory-monitor.service';
import { NetworkMonitorService } from './network-monitor.service';
import { UserExperienceMonitorService } from './user-experience-monitor.service';

export interface OptimizationRecommendation {
  id: string;
  category: 'performance' | 'memory' | 'network' | 'ux' | 'caching' | 'bundle' | 'rendering';
  priority: 'low' | 'medium' | 'high' | 'critical';
  title: string;
  description: string;
  impact: 'low' | 'medium' | 'high';
  effort: 'low' | 'medium' | 'high';
  estimatedGain: string; // e.g., "200ms faster", "30% less memory"
  implementationSteps: string[];
  technicalDetails: string;
  relatedMetrics: string[];
  autoApplicable: boolean;
  confidence: number; // 0-1
  timestamp: number;
  status: 'pending' | 'applied' | 'dismissed' | 'failed';
  metadata?: any;
}

export interface OptimizationStrategy {
  name: string;
  description: string;
  recommendations: OptimizationRecommendation[];
  estimatedImpact: {
    performanceGain: number; // percentage
    memoryReduction: number;
    loadTimeImprovement: number; // ms
    userExperienceScore: number; // 0-100
  };
  implementationTime: number; // hours
  riskLevel: 'low' | 'medium' | 'high';
}

export interface AutoOptimization {
  id: string;
  name: string;
  description: string;
  enabled: boolean;
  trigger: 'threshold' | 'schedule' | 'event';
  condition: string;
  action: string;
  lastRun?: number;
  nextRun?: number;
  successCount: number;
  failureCount: number;
}

export interface OptimizationMetrics {
  applicationsCount: number;
  successRate: number;
  totalGainMs: number;
  memoryReductionMB: number;
  userExperienceImprovement: number;
  automationRate: number;
  recommendationsCount: {
    pending: number;
    applied: number;
    dismissed: number;
    failed: number;
  };
  categoryBreakdown: { [category: string]: number };
}

/**
 * Performance Optimizer Service
 * Intelligent performance optimization recommendations and automation
 */
@Injectable({
  providedIn: 'root'
})
export class PerformanceOptimizerService {
  private performanceAnalytics = inject(PerformanceAnalyticsService);
  private memoryMonitor = inject(MemoryMonitorService);
  private networkMonitor = inject(NetworkMonitorService);
  private uxMonitor = inject(UserExperienceMonitorService);

  private recommendations$ = new BehaviorSubject<OptimizationRecommendation[]>([]);
  private autoOptimizations$ = new BehaviorSubject<AutoOptimization[]>(this.getDefaultAutoOptimizations());
  private optimizationMetrics$ = new BehaviorSubject<OptimizationMetrics>(this.getInitialMetrics());

  private readonly ANALYSIS_INTERVAL = 60000; // 1 minute
  private readonly CONFIDENCE_THRESHOLD = 0.7;
  private appliedOptimizations = new Map<string, any>();

  constructor() {
    this.initializeOptimizer();
  }

  /**
   * Initialize performance optimizer
   */
  private initializeOptimizer(): void {
    this.startAnalysisEngine();
    this.setupAutoOptimizations();
    this.startMetricsCollection();

    console.log('🚀 Performance Optimizer Service initialized');
  }

  /**
   * Start analysis engine
   */
  private startAnalysisEngine(): void {
    // Analyze performance data every minute
    timer(this.ANALYSIS_INTERVAL, this.ANALYSIS_INTERVAL).subscribe(() => {
      this.analyzePerformanceData();
    });

    // React to significant changes
    combineLatest([
      this.performanceAnalytics.getPerformanceReports(),
      this.memoryMonitor.getMemoryMetrics(),
      this.networkMonitor.getNetworkMetrics(),
      this.uxMonitor.getUXMetrics()
    ]).pipe(
      debounceTime(5000) // Wait for stability
    ).subscribe(([perfReports, memoryMetrics, networkMetrics, uxMetrics]) => {
      this.generateRecommendations(perfReports, memoryMetrics, networkMetrics, uxMetrics);
    });
  }

  /**
   * Setup auto optimizations
   */
  private setupAutoOptimizations(): void {
    // Check and run auto optimizations every 30 seconds
    timer(30000, 30000).subscribe(() => {
      this.runAutoOptimizations();
    });
  }

  /**
   * Start metrics collection
   */
  private startMetricsCollection(): void {
    // Update optimization metrics every 2 minutes
    timer(120000, 120000).subscribe(() => {
      this.updateOptimizationMetrics();
    });
  }

  /**
   * Analyze performance data and identify optimization opportunities
   */
  private analyzePerformanceData(): void {
    console.log('🔍 Analyzing performance data for optimization opportunities...');

    const currentRecommendations = this.recommendations$.value;
    const newRecommendations: OptimizationRecommendation[] = [];

    // Check for various optimization opportunities
    this.analyzePageLoadPerformance(newRecommendations);
    this.analyzeMemoryUsage(newRecommendations);
    this.analyzeNetworkPerformance(newRecommendations);
    this.analyzeUserExperience(newRecommendations);
    this.analyzeCachingOpportunities(newRecommendations);
    this.analyzeBundleOptimization(newRecommendations);
    this.analyzeRenderingPerformance(newRecommendations);

    // Filter out low confidence recommendations
    const highConfidenceRecommendations = newRecommendations.filter(
      rec => rec.confidence >= this.CONFIDENCE_THRESHOLD
    );

    // Merge with existing recommendations (avoid duplicates)
    const allRecommendations = this.mergeRecommendations(currentRecommendations, highConfidenceRecommendations);

    this.recommendations$.next(allRecommendations);

    if (highConfidenceRecommendations.length > 0) {
      console.log(`💡 Generated ${highConfidenceRecommendations.length} new optimization recommendations`);
    }
  }

  /**
   * Analyze page load performance
   */
  private analyzePageLoadPerformance(recommendations: OptimizationRecommendation[]): void {
    // This would analyze FCP, LCP, TTI metrics
    const vitals = this.uxMonitor.getCoreWebVitals();

    if (vitals.firstContentfulPaint > 2000) {
      recommendations.push({
        id: `fcp-optimization-${Date.now()}`,
        category: 'performance',
        priority: 'high',
        title: 'Optimize First Contentful Paint',
        description: 'FCP is slower than recommended. Consider optimizing critical rendering path.',
        impact: 'high',
        effort: 'medium',
        estimatedGain: `${Math.round((vitals.firstContentfulPaint - 1800) / 100) * 100}ms faster`,
        implementationSteps: [
          'Minimize blocking resources',
          'Optimize critical CSS',
          'Use resource hints (preload, prefetch)',
          'Implement server-side rendering',
          'Optimize web fonts loading'
        ],
        technicalDetails: 'Focus on reducing the time to first meaningful paint by optimizing above-the-fold content delivery.',
        relatedMetrics: ['FCP', 'Speed Index', 'TTI'],
        autoApplicable: false,
        confidence: 0.9,
        timestamp: Date.now(),
        status: 'pending'
      });
    }

    if (vitals.largestContentfulPaint > 2500) {
      recommendations.push({
        id: `lcp-optimization-${Date.now()}`,
        category: 'performance',
        priority: 'high',
        title: 'Optimize Largest Contentful Paint',
        description: 'LCP indicates slow loading of main content. Optimize largest content element.',
        impact: 'high',
        effort: 'medium',
        estimatedGain: `${Math.round((vitals.largestContentfulPaint - 2500) / 100) * 100}ms faster`,
        implementationSteps: [
          'Optimize images and videos',
          'Preload LCP element',
          'Minimize server response time',
          'Optimize CSS delivery',
          'Use CDN for static assets'
        ],
        technicalDetails: 'Identify and optimize the largest content element visible in the viewport.',
        relatedMetrics: ['LCP', 'Load Event', 'Speed Index'],
        autoApplicable: false,
        confidence: 0.85,
        timestamp: Date.now(),
        status: 'pending'
      });
    }
  }

  /**
   * Analyze memory usage
   */
  private analyzeMemoryUsage(recommendations: OptimizationRecommendation[]): void {
    const memoryMetrics = this.memoryMonitor.getMemoryMetrics();

    memoryMetrics.subscribe(metrics => {
      if (metrics.current.usagePercentage > 0.8) {
        recommendations.push({
          id: `memory-optimization-${Date.now()}`,
          category: 'memory',
          priority: 'critical',
          title: 'Reduce Memory Usage',
          description: 'High memory usage detected. Implement memory optimization strategies.',
          impact: 'high',
          effort: 'medium',
          estimatedGain: `${Math.round((metrics.current.usagePercentage - 0.6) * 100)}% memory reduction`,
          implementationSteps: [
            'Clear unused caches',
            'Implement object pooling',
            'Remove event listeners properly',
            'Optimize data structures',
            'Use WeakMap/WeakSet for temporary references'
          ],
          technicalDetails: 'Focus on reducing heap usage and preventing memory leaks.',
          relatedMetrics: ['Memory Usage', 'GC Frequency'],
          autoApplicable: true,
          confidence: 0.8,
          timestamp: Date.now(),
          status: 'pending'
        });
      }

      // Check for memory leak patterns
      const leakDetection = this.memoryMonitor.getLeakDetection();
      leakDetection.subscribe(leak => {
        if (leak.isLeakDetected) {
          recommendations.push({
            id: `memory-leak-fix-${Date.now()}`,
            category: 'memory',
            priority: 'critical',
            title: 'Fix Memory Leak',
            description: 'Memory leak detected. Investigate and fix memory retention issues.',
            impact: 'high',
            effort: 'high',
            estimatedGain: `Stop ${leak.growthRate.toFixed(1)} MB/min growth`,
            implementationSteps: [
              'Profile memory usage patterns',
              'Check for unclosed subscriptions',
              'Review DOM element references',
              'Audit third-party libraries',
              'Implement proper cleanup in components'
            ],
            technicalDetails: `Memory growing at ${leak.growthRate.toFixed(2)} MB/min with ${(leak.confidence * 100).toFixed(1)}% confidence.`,
            relatedMetrics: ['Memory Growth Rate', 'Object Count'],
            autoApplicable: false,
            confidence: leak.confidence,
            timestamp: Date.now(),
            status: 'pending'
          });
        }
      });
    });
  }

  /**
   * Analyze network performance
   */
  private analyzeNetworkPerformance(recommendations: OptimizationRecommendation[]): void {
    const networkMetrics = this.networkMonitor.getNetworkMetrics();

    networkMetrics.subscribe(metrics => {
      if (metrics.requestMetrics.avgResponseTime > 1000) {
        recommendations.push({
          id: `network-optimization-${Date.now()}`,
          category: 'network',
          priority: 'medium',
          title: 'Optimize Network Requests',
          description: 'Slow network requests detected. Optimize request patterns and caching.',
          impact: 'medium',
          effort: 'medium',
          estimatedGain: `${Math.round(metrics.requestMetrics.avgResponseTime - 500)}ms faster requests`,
          implementationSteps: [
            'Implement request batching',
            'Add proper caching headers',
            'Use compression (gzip/brotli)',
            'Optimize API payload size',
            'Implement request prioritization'
          ],
          technicalDetails: 'Focus on reducing request latency and improving caching strategies.',
          relatedMetrics: ['Response Time', 'Request Count', 'Cache Hit Rate'],
          autoApplicable: true,
          confidence: 0.75,
          timestamp: Date.now(),
          status: 'pending'
        });
      }

      // Check for excessive requests
      if (metrics.requestMetrics.totalRequests > 100) {
        recommendations.push({
          id: `request-reduction-${Date.now()}`,
          category: 'network',
          priority: 'medium',
          title: 'Reduce Request Count',
          description: 'High number of requests detected. Consider bundling and caching strategies.',
          impact: 'medium',
          effort: 'low',
          estimatedGain: `${Math.round((metrics.requestMetrics.totalRequests - 50) / 10) * 10} fewer requests`,
          implementationSteps: [
            'Bundle similar resources',
            'Implement resource caching',
            'Use data URLs for small assets',
            'Combine API calls where possible',
            'Implement lazy loading'
          ],
          technicalDetails: 'Reduce network overhead by minimizing HTTP requests.',
          relatedMetrics: ['Request Count', 'Bundle Size', 'Cache Efficiency'],
          autoApplicable: true,
          confidence: 0.8,
          timestamp: Date.now(),
          status: 'pending'
        });
      }
    });
  }

  /**
   * Analyze user experience
   */
  private analyzeUserExperience(recommendations: OptimizationRecommendation[]): void {
    const uxMetrics = this.uxMonitor.getUXMetrics();

    if (uxMetrics.vitals.cumulativeLayoutShift > 0.1) {
      recommendations.push({
        id: `cls-optimization-${Date.now()}`,
        category: 'ux',
        priority: 'high',
        title: 'Reduce Layout Shifts',
        description: 'High Cumulative Layout Shift detected. Stabilize visual elements.',
        impact: 'high',
        effort: 'medium',
        estimatedGain: `${((uxMetrics.vitals.cumulativeLayoutShift - 0.1) * 100).toFixed(1)}% CLS reduction`,
        implementationSteps: [
          'Set dimensions for images and videos',
          'Reserve space for dynamic content',
          'Avoid inserting content above existing content',
          'Use CSS transforms instead of layout changes',
          'Preload web fonts to prevent FOIT/FOUT'
        ],
        technicalDetails: 'Focus on preventing unexpected layout shifts that harm user experience.',
        relatedMetrics: ['CLS', 'Layout Stability', 'Visual Stability'],
        autoApplicable: false,
        confidence: 0.9,
        timestamp: Date.now(),
        status: 'pending'
      });
    }

    if (uxMetrics.vitals.firstInputDelay > 100) {
      recommendations.push({
        id: `fid-optimization-${Date.now()}`,
        category: 'ux',
        priority: 'high',
        title: 'Improve Input Responsiveness',
        description: 'High First Input Delay. Optimize JavaScript execution and main thread blocking.',
        impact: 'high',
        effort: 'high',
        estimatedGain: `${Math.round(uxMetrics.vitals.firstInputDelay - 100)}ms faster response`,
        implementationSteps: [
          'Break up long JavaScript tasks',
          'Use web workers for heavy computation',
          'Defer non-critical JavaScript',
          'Optimize third-party scripts',
          'Implement code splitting'
        ],
        technicalDetails: 'Reduce main thread blocking to improve interaction responsiveness.',
        relatedMetrics: ['FID', 'Total Blocking Time', 'Main Thread Work'],
        autoApplicable: false,
        confidence: 0.85,
        timestamp: Date.now(),
        status: 'pending'
      });
    }
  }

  /**
   * Analyze caching opportunities
   */
  private analyzeCachingOpportunities(recommendations: OptimizationRecommendation[]): void {
    // This would analyze cache hit rates and identify caching improvements
    recommendations.push({
      id: `caching-strategy-${Date.now()}`,
      category: 'caching',
      priority: 'medium',
      title: 'Implement Advanced Caching',
      description: 'Optimize caching strategy to reduce server load and improve response times.',
      impact: 'medium',
      effort: 'low',
      estimatedGain: '40-60% faster repeated requests',
      implementationSteps: [
        'Implement service worker caching',
        'Add cache-first strategies for static assets',
        'Use stale-while-revalidate for API data',
        'Implement intelligent cache invalidation',
        'Add offline fallback mechanisms'
      ],
      technicalDetails: 'Leverage browser and service worker caching for better performance.',
      relatedMetrics: ['Cache Hit Rate', 'Network Requests', 'Load Time'],
      autoApplicable: true,
      confidence: 0.7,
      timestamp: Date.now(),
      status: 'pending'
    });
  }

  /**
   * Analyze bundle optimization
   */
  private analyzeBundleOptimization(recommendations: OptimizationRecommendation[]): void {
    // This would analyze bundle sizes and suggest optimizations
    recommendations.push({
      id: `bundle-optimization-${Date.now()}`,
      category: 'bundle',
      priority: 'medium',
      title: 'Optimize Bundle Size',
      description: 'Reduce JavaScript bundle size through code splitting and tree shaking.',
      impact: 'medium',
      effort: 'medium',
      estimatedGain: '20-30% smaller bundles',
      implementationSteps: [
        'Implement route-based code splitting',
        'Enable tree shaking',
        'Remove unused dependencies',
        'Use dynamic imports for heavy modules',
        'Optimize third-party libraries'
      ],
      technicalDetails: 'Reduce initial bundle size and improve loading performance.',
      relatedMetrics: ['Bundle Size', 'Parse Time', 'Download Time'],
      autoApplicable: false,
      confidence: 0.8,
      timestamp: Date.now(),
      status: 'pending'
    });
  }

  /**
   * Analyze rendering performance
   */
  private analyzeRenderingPerformance(recommendations: OptimizationRecommendation[]): void {
    // This would analyze rendering metrics and suggest optimizations
    recommendations.push({
      id: `rendering-optimization-${Date.now()}`,
      category: 'rendering',
      priority: 'low',
      title: 'Optimize Rendering Performance',
      description: 'Improve rendering efficiency through component and change detection optimization.',
      impact: 'low',
      effort: 'low',
      estimatedGain: '10-20% smoother interactions',
      implementationSteps: [
        'Use OnPush change detection strategy',
        'Implement virtual scrolling for long lists',
        'Optimize expensive operations with memoization',
        'Use trackBy functions in *ngFor',
        'Minimize DOM manipulations'
      ],
      technicalDetails: 'Optimize Angular change detection and rendering pipeline.',
      relatedMetrics: ['Render Time', 'Change Detection Cycles', 'Frame Rate'],
      autoApplicable: true,
      confidence: 0.6,
      timestamp: Date.now(),
      status: 'pending'
    });
  }

  /**
   * Generate comprehensive recommendations
   */
  private generateRecommendations(perfReports: any[], memoryMetrics: any, networkMetrics: any, uxMetrics: any): void {
    // This method would analyze all metrics together and generate contextual recommendations
    console.log('📊 Generating comprehensive optimization recommendations...');
  }

  /**
   * Merge new recommendations with existing ones
   */
  private mergeRecommendations(
    existing: OptimizationRecommendation[],
    newRecs: OptimizationRecommendation[]
  ): OptimizationRecommendation[] {
    const mergedRecs = [...existing];

    newRecs.forEach(newRec => {
      const existingIndex = mergedRecs.findIndex(existing =>
        existing.category === newRec.category &&
        existing.title === newRec.title &&
        existing.status === 'pending'
      );

      if (existingIndex === -1) {
        mergedRecs.push(newRec);
      } else {
        // Update existing recommendation if new one has higher confidence
        if (newRec.confidence > mergedRecs[existingIndex].confidence) {
          mergedRecs[existingIndex] = newRec;
        }
      }
    });

    // Sort by priority and confidence
    return mergedRecs.sort((a, b) => {
      const priorityOrder = { critical: 4, high: 3, medium: 2, low: 1 };
      const priorityDiff = priorityOrder[b.priority] - priorityOrder[a.priority];
      if (priorityDiff !== 0) return priorityDiff;
      return b.confidence - a.confidence;
    });
  }

  /**
   * Run auto optimizations
   */
  private runAutoOptimizations(): void {
    const autoOptimizations = this.autoOptimizations$.value.filter(opt => opt.enabled);

    autoOptimizations.forEach(optimization => {
      if (this.shouldRunOptimization(optimization)) {
        this.executeAutoOptimization(optimization);
      }
    });
  }

  /**
   * Check if optimization should run
   */
  private shouldRunOptimization(optimization: AutoOptimization): boolean {
    const now = Date.now();

    // Check if it's time to run
    if (optimization.nextRun && now < optimization.nextRun) {
      return false;
    }

    // Check trigger conditions
    switch (optimization.trigger) {
      case 'threshold':
        return this.evaluateThresholdCondition(optimization.condition);
      case 'schedule':
        return this.evaluateScheduleCondition(optimization.condition);
      case 'event':
        return this.evaluateEventCondition(optimization.condition);
      default:
        return false;
    }
  }

  /**
   * Execute auto optimization
   */
  private async executeAutoOptimization(optimization: AutoOptimization): Promise<void> {
    try {
      console.log(`🤖 Running auto optimization: ${optimization.name}`);

      const success = await this.performOptimizationAction(optimization.action);

      if (success) {
        optimization.successCount++;
        optimization.lastRun = Date.now();
        console.log(`✅ Auto optimization '${optimization.name}' completed successfully`);
      } else {
        optimization.failureCount++;
        console.warn(`❌ Auto optimization '${optimization.name}' failed`);
      }

      // Schedule next run
      optimization.nextRun = this.calculateNextRun(optimization);

    } catch (error) {
      optimization.failureCount++;
      console.error(`❌ Auto optimization '${optimization.name}' error:`, error);
    }

    // Update the optimization
    const updatedOptimizations = this.autoOptimizations$.value.map(opt =>
      opt.id === optimization.id ? optimization : opt
    );
    this.autoOptimizations$.next(updatedOptimizations);
  }

  /**
   * Perform optimization action
   */
  private async performOptimizationAction(action: string): Promise<boolean> {
    switch (action) {
      case 'clear-cache':
        return this.clearApplicationCache();
      case 'gc-hint':
        return this.triggerGarbageCollection();
      case 'optimize-images':
        return this.optimizeImages();
      case 'compress-data':
        return this.compressStoredData();
      default:
        console.warn(`Unknown optimization action: ${action}`);
        return false;
    }
  }

  /**
   * Clear application cache
   */
  private clearApplicationCache(): boolean {
    try {
      // Clear various caches
      if ('caches' in window) {
        caches.keys().then(names => {
          names.forEach(name => {
            caches.delete(name);
          });
        });
      }

      // Clear localStorage selectively
      Object.keys(localStorage).forEach(key => {
        if (key.startsWith('cache_') || key.startsWith('temp_')) {
          localStorage.removeItem(key);
        }
      });

      return true;
    } catch (error) {
      console.error('Cache clearing failed:', error);
      return false;
    }
  }

  /**
   * Trigger garbage collection hint
   */
  private triggerGarbageCollection(): boolean {
    try {
      // Force garbage collection if available
      if ('gc' in window) {
        (window as any).gc();
        return true;
      }

      // Create memory pressure to encourage GC
      const largeArray = new Array(1000000).fill(null);
      largeArray.length = 0;

      return true;
    } catch (error) {
      console.error('GC trigger failed:', error);
      return false;
    }
  }

  /**
   * Optimize images (placeholder implementation)
   */
  private optimizeImages(): boolean {
    // This would implement image optimization strategies
    console.log('🖼️ Image optimization triggered');
    return true;
  }

  /**
   * Compress stored data
   */
  private compressStoredData(): boolean {
    try {
      // Compress large localStorage items
      Object.keys(localStorage).forEach(key => {
        const value = localStorage.getItem(key);
        if (value && value.length > 10000) {
          // Simple compression simulation
          try {
            const compressed = btoa(value);
            if (compressed.length < value.length) {
              localStorage.setItem(key + '_compressed', compressed);
              localStorage.removeItem(key);
            }
          } catch (e) {
            // Ignore compression errors
          }
        }
      });

      return true;
    } catch (error) {
      console.error('Data compression failed:', error);
      return false;
    }
  }

  /**
   * Update optimization metrics
   */
  private updateOptimizationMetrics(): void {
    const recommendations = this.recommendations$.value;

    const metrics: OptimizationMetrics = {
      applicationsCount: this.appliedOptimizations.size,
      successRate: this.calculateSuccessRate(),
      totalGainMs: this.calculateTotalGain(),
      memoryReductionMB: this.calculateMemoryReduction(),
      userExperienceImprovement: this.calculateUXImprovement(),
      automationRate: this.calculateAutomationRate(),
      recommendationsCount: {
        pending: recommendations.filter(r => r.status === 'pending').length,
        applied: recommendations.filter(r => r.status === 'applied').length,
        dismissed: recommendations.filter(r => r.status === 'dismissed').length,
        failed: recommendations.filter(r => r.status === 'failed').length
      },
      categoryBreakdown: this.calculateCategoryBreakdown(recommendations)
    };

    this.optimizationMetrics$.next(metrics);
  }

  // Helper methods for condition evaluation and calculations

  private evaluateThresholdCondition(condition: string): boolean {
    // Parse condition like "memory > 80" or "response_time > 1000"
    return false; // Simplified
  }

  private evaluateScheduleCondition(condition: string): boolean {
    // Parse schedule condition like "daily" or "hourly"
    return false; // Simplified
  }

  private evaluateEventCondition(condition: string): boolean {
    // Parse event condition like "page_load" or "user_idle"
    return false; // Simplified
  }

  private calculateNextRun(optimization: AutoOptimization): number {
    // Calculate next run time based on schedule
    return Date.now() + 3600000; // 1 hour default
  }

  private calculateSuccessRate(): number {
    const autoOpts = this.autoOptimizations$.value;
    const totalRuns = autoOpts.reduce((sum, opt) => sum + opt.successCount + opt.failureCount, 0);
    const successRuns = autoOpts.reduce((sum, opt) => sum + opt.successCount, 0);
    return totalRuns > 0 ? successRuns / totalRuns : 1;
  }

  private calculateTotalGain(): number {
    // Calculate total performance gain from applied optimizations
    return Array.from(this.appliedOptimizations.values()).reduce((sum, opt) => sum + (opt.gainMs || 0), 0);
  }

  private calculateMemoryReduction(): number {
    // Calculate total memory reduction
    return Array.from(this.appliedOptimizations.values()).reduce((sum, opt) => sum + (opt.memoryMB || 0), 0);
  }

  private calculateUXImprovement(): number {
    // Calculate UX score improvement
    return 0; // Simplified
  }

  private calculateAutomationRate(): number {
    const recommendations = this.recommendations$.value;
    const autoApplicable = recommendations.filter(r => r.autoApplicable).length;
    return recommendations.length > 0 ? autoApplicable / recommendations.length : 0;
  }

  private calculateCategoryBreakdown(recommendations: OptimizationRecommendation[]): { [category: string]: number } {
    return recommendations.reduce((breakdown, rec) => {
      breakdown[rec.category] = (breakdown[rec.category] || 0) + 1;
      return breakdown;
    }, {} as { [category: string]: number });
  }

  // Public API

  /**
   * Get optimization recommendations
   */
  getRecommendations(): Observable<OptimizationRecommendation[]> {
    return this.recommendations$.asObservable();
  }

  /**
   * Get optimization metrics
   */
  getOptimizationMetrics(): Observable<OptimizationMetrics> {
    return this.optimizationMetrics$.asObservable();
  }

  /**
   * Apply recommendation
   */
  async applyRecommendation(recommendationId: string): Promise<boolean> {
    const recommendations = this.recommendations$.value;
    const recommendation = recommendations.find(r => r.id === recommendationId);

    if (!recommendation) {
      throw new Error('Recommendation not found');
    }

    try {
      // Apply the optimization
      const success = await this.performOptimizationAction(recommendation.title.toLowerCase().replace(/\s+/g, '-'));

      // Update recommendation status
      const updatedRecommendations = recommendations.map(r =>
        r.id === recommendationId
          ? { ...r, status: success ? 'applied' : 'failed' as const }
          : r
      );

      this.recommendations$.next(updatedRecommendations);

      if (success) {
        this.appliedOptimizations.set(recommendationId, recommendation);
      }

      return success;

    } catch (error) {
      console.error('Failed to apply recommendation:', error);
      return false;
    }
  }

  /**
   * Dismiss recommendation
   */
  dismissRecommendation(recommendationId: string): void {
    const recommendations = this.recommendations$.value;
    const updatedRecommendations = recommendations.map(r =>
      r.id === recommendationId ? { ...r, status: 'dismissed' as const } : r
    );

    this.recommendations$.next(updatedRecommendations);
  }

  /**
   * Get optimization strategies
   */
  getOptimizationStrategies(): OptimizationStrategy[] {
    const recommendations = this.recommendations$.value.filter(r => r.status === 'pending');

    // Group recommendations into strategies
    const strategies: OptimizationStrategy[] = [
      {
        name: 'Quick Wins',
        description: 'Low effort, high impact optimizations',
        recommendations: recommendations.filter(r => r.effort === 'low' && r.impact === 'high'),
        estimatedImpact: {
          performanceGain: 15,
          memoryReduction: 10,
          loadTimeImprovement: 200,
          userExperienceScore: 85
        },
        implementationTime: 2,
        riskLevel: 'low'
      },
      {
        name: 'Performance Boost',
        description: 'Medium effort optimizations for significant performance gains',
        recommendations: recommendations.filter(r => r.category === 'performance'),
        estimatedImpact: {
          performanceGain: 30,
          memoryReduction: 5,
          loadTimeImprovement: 500,
          userExperienceScore: 90
        },
        implementationTime: 8,
        riskLevel: 'medium'
      },
      {
        name: 'Memory Optimization',
        description: 'Focus on memory usage and leak prevention',
        recommendations: recommendations.filter(r => r.category === 'memory'),
        estimatedImpact: {
          performanceGain: 10,
          memoryReduction: 40,
          loadTimeImprovement: 100,
          userExperienceScore: 80
        },
        implementationTime: 6,
        riskLevel: 'low'
      }
    ];

    return strategies.filter(s => s.recommendations.length > 0);
  }

  /**
   * Generate optimization report
   */
  generateOptimizationReport(): string {
    const recommendations = this.recommendations$.value;
    const metrics = this.optimizationMetrics$.value;

    const report = {
      timestamp: new Date().toISOString(),
      summary: {
        totalRecommendations: recommendations.length,
        criticalIssues: recommendations.filter(r => r.priority === 'critical').length,
        automationOpportunities: recommendations.filter(r => r.autoApplicable).length,
        estimatedGains: this.calculateTotalPotentialGains(recommendations)
      },
      recommendations: recommendations.map(r => ({
        title: r.title,
        category: r.category,
        priority: r.priority,
        impact: r.impact,
        effort: r.effort,
        estimatedGain: r.estimatedGain,
        confidence: r.confidence
      })),
      metrics
    };

    return JSON.stringify(report, null, 2);
  }

  private calculateTotalPotentialGains(recommendations: OptimizationRecommendation[]): any {
    return {
      timeReduction: '1-3 seconds',
      memoryReduction: '20-50 MB',
      performanceScore: '+15-25 points',
      userExperience: '+10-20%'
    };
  }

  // Default configurations

  private getDefaultAutoOptimizations(): AutoOptimization[] {
    return [
      {
        id: 'auto-cache-clear',
        name: 'Auto Cache Clear',
        description: 'Automatically clear cache when memory usage is high',
        enabled: true,
        trigger: 'threshold',
        condition: 'memory > 85',
        action: 'clear-cache',
        successCount: 0,
        failureCount: 0
      },
      {
        id: 'auto-gc-hint',
        name: 'Auto GC Hint',
        description: 'Trigger garbage collection hints during idle time',
        enabled: false,
        trigger: 'event',
        condition: 'user_idle',
        action: 'gc-hint',
        successCount: 0,
        failureCount: 0
      }
    ];
  }

  private getInitialMetrics(): OptimizationMetrics {
    return {
      applicationsCount: 0,
      successRate: 1,
      totalGainMs: 0,
      memoryReductionMB: 0,
      userExperienceImprovement: 0,
      automationRate: 0,
      recommendationsCount: {
        pending: 0,
        applied: 0,
        dismissed: 0,
        failed: 0
      },
      categoryBreakdown: {}
    };
  }
}