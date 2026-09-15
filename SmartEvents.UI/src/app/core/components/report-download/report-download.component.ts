import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { Observable } from 'rxjs';
import { ReportFormat } from '../../models/report.models';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-report-download',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './report-download.component.html',
  styleUrls: ['./report-download.component.scss']
})
export class ReportDownloadComponent {
  @Input({ required: true }) label = '';
  @Input({ required: true }) download!: (format: ReportFormat) => Observable<void>;

  busy: ReportFormat | null = null;

  constructor(private toast: ToastService) {}

  run(format: ReportFormat): void {
    if (this.busy) return;
    this.busy = format;
    this.download(format).subscribe({
      next: () => { this.busy = null; },
      error: err => {
        this.toast.error(err.error?.message ?? 'Report download failed.');
        this.busy = null;
      }
    });
  }
}
