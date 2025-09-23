import { ApplicationConfig, provideZoneChangeDetection, importProvidersFrom } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';

import { routes } from './app.routes';
import { enhancedJwtInterceptor } from './core/auth/interceptors/enhanced-jwt.interceptor';
import { errorInterceptor } from './core/api/interceptors/error.interceptor';
import { loadingInterceptor } from './core/api/interceptors/loading.interceptor';
import { groupInterceptor } from './core/api/interceptors/group.interceptor';
import { LucideAngularModule, List, Grid3x3, ChevronUp, ChevronDown, ChevronLeft, ChevronRight, Download, FileSpreadsheet, FileText, Upload, Filter, Search, Inbox, MoreHorizontal, Eye, Edit, Trash, Users, UserCheck, Shield, Settings as SettingsIcon, BookOpen, User, Activity, Layers, Package as PackageIcon, BarChart3, Key, Server, RefreshCw, Star, Save, RotateCcw, Minimize2, Copy, CheckCircle, Plus, AlertCircle } from 'lucide-angular';
// import { OptimizationModule } from './core/optimization/optimization.module';
// import { CacheModule } from './core/cache/cache.module';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([
        enhancedJwtInterceptor,
        groupInterceptor,
        errorInterceptor,
        loadingInterceptor
      ])
    ),
    importProvidersFrom(LucideAngularModule.pick({
      List,
      Grid3x3,
      CheckCircle,
      ChevronUp,
      ChevronDown,
      ChevronLeft,
      ChevronRight,
      Download,
      FileSpreadsheet,
      FileText,
      Upload,
      Filter,
      Search,
      Inbox,
      MoreHorizontal,
      Eye,
      Edit,
      Trash,
      Users,
      UserCheck,
      Shield,
      Settings: SettingsIcon,
      BookOpen,
      User,
      Activity,
      Layers,
      Package: PackageIcon,
      BarChart3,
      Key,
      Server,
      RefreshCw,
      Star,
      Save,
      RotateCcw,
      Minimize2,
      Copy,
      Plus,
      AlertCircle
    })),
    // Import cache module for token optimization
    // importProvidersFrom(CacheModule),
    provideAnimations(),
    // Enhanced toastr configuration
    provideToastr({
      timeOut: 5000,
      positionClass: 'toast-top-right',
      preventDuplicates: true,
      closeButton: true,
      progressBar: true,
      extendedTimeOut: 2000,
      enableHtml: true,
      tapToDismiss: true,
      maxOpened: 5,
      autoDismiss: true,
      newestOnTop: true
    })
  ]
};
