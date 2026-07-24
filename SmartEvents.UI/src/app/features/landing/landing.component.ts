import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

interface Partner {
  name: string;
  file: string;
}

interface Feature {
  icon: string;
  title: string;
  description: string;
}

interface Step {
  number: string;
  title: string;
  description: string;
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {
  readonly partners: Partner[] = [
    { name: 'AfrikaFest', file: 'africafest.jpeg' },
    { name: 'Agritech Zambia Expo', file: 'agritech-zambia-expo.jpeg' },
    { name: 'Copperbelt University', file: 'cbu.jpeg' },
    { name: 'Elite Deco Events & Hiring Zambia', file: 'elite-deco-events.jpeg' },
    { name: 'FNB', file: 'fnb.jpeg' },
    { name: 'Garden Court', file: 'garden-court.jpeg' },
    { name: 'Lusaka Fashion House', file: 'lusaka-fashion-house.jpeg' },
    { name: 'Zambia Tourism Adventure', file: 'zambia-tourism-adventure.jpeg' },
    { name: 'ZamFilm', file: 'zamfilm.jpeg' },
    { name: 'FNB Kopala Run', file: 'fnb-kopala-run.jpeg' },
    { name: 'Copper Eagles Football Academy', file: 'copper-eagles-academy.jpeg' },
    { name: 'Lusaka Arts Collective', file: 'lusaka-arts-collective.jpeg' },
    { name: 'Mukuba Fitness', file: 'mukuba-fitness.jpeg' },
    { name: 'Zambia Innovation Forum', file: 'zambia-innovation-forum.jpeg' },
    { name: 'Blazer Events', file: 'blazer-events.jpeg' }
  ];

  readonly features: Feature[] = [
    { icon: 'Ticket', title: 'Ticketing & QR Check-in', description: 'Sell tickets and check attendees in with a single scan — no paper, no queues.' },
    { icon: 'Building2', title: 'Multi-Company Workspaces', description: 'Run events for multiple organizations from one account, each with its own team.' },
    { icon: 'Bell', title: 'Real-Time Notifications', description: 'Attendees and organizers stay in the loop with instant updates, powered by SignalR.' },
    { icon: 'DollarSign', title: 'Local Payment Methods', description: 'Accept Airtel Money, MTN MoMo, and card payments — priced in Zambian Kwacha.' },
    { icon: 'BookMarked', title: 'Venue Booking', description: 'Browse and book venues directly on the platform, from conference halls to open grounds.' },
    { icon: 'BarChart2', title: 'Analytics Dashboard', description: 'Track registrations, revenue, and attendance trends as they happen.' }
  ];

  readonly steps: Step[] = [
    { number: '01', title: 'Create Your Event', description: 'Set the details, pick a venue, and choose your ticket price in minutes.' },
    { number: '02', title: 'Publish & Sell Tickets', description: 'Share your event and start collecting registrations and payments right away.' },
    { number: '03', title: 'Check Attendees In', description: 'Scan QR codes at the door for instant, fraud-proof check-in.' },
    { number: '04', title: 'Track Performance', description: 'Watch registrations and revenue roll in on your live analytics dashboard.' }
  ];
}
