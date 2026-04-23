import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-skeleton',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="skeleton-wrapper">
      @for (_ of items; track $index) {
        <div class="skeleton-card">
          <div class="skeleton-img pulse"></div>
          <div class="skeleton-body">
            <div class="skeleton-line wide pulse"></div>
            <div class="skeleton-line medium pulse"></div>
            <div class="skeleton-line narrow pulse"></div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .skeleton-wrapper {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.5rem;
    }
    .skeleton-card {
      border-radius: 12px;
      overflow: hidden;
      background: #fff;
      box-shadow: 0 2px 8px rgba(0,0,0,.06);
    }
    .skeleton-img {
      height: 180px;
      background: #e2e8f0;
    }
    .skeleton-body {
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: .75rem;
    }
    .skeleton-line {
      height: 14px;
      border-radius: 6px;
      background: #e2e8f0;
    }
    .skeleton-line.wide   { width: 80%; }
    .skeleton-line.medium { width: 55%; }
    .skeleton-line.narrow { width: 35%; }
    .pulse {
      animation: pulse 1.5s ease-in-out infinite;
    }
    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50%       { opacity: .45; }
    }
  `]
})
export class SkeletonComponent {
  @Input() count = 6;
  get items(): unknown[] { return Array(this.count); }
}
