import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  LucideAngularModule,
  Eye, EyeOff,
  LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
  Plus, Pencil, Trash2, ArrowLeft, Ticket, QrCode,
  Check, X, Shield, Mail, Phone, Globe, Clock,
  Search, ChevronRight, Settings, AlertCircle, CheckCircle,
  ScanLine, Star, Tag, DollarSign, Briefcase
} from 'lucide-angular';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { tokenRefreshInterceptor } from './core/interceptors/token-refresh.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, tokenRefreshInterceptor])),
    importProvidersFrom(LucideAngularModule.pick({
      Eye, EyeOff,
      LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
      Plus, Pencil, Trash2, ArrowLeft, Ticket, QrCode,
      Check, X, Shield, Mail, Phone, Globe, Clock,
      Search, ChevronRight, Settings, AlertCircle, CheckCircle,
      ScanLine, Star, Tag, DollarSign, Briefcase
    }))
  ]
};
