using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;

namespace SmartEvents.API.Infrastructure.Data;

public static class DbSeeder
{
    // All seeded accounts share this password
    public const string DefaultPassword = "Seed1234!";

    public static async Task SeedAsync(SmartEventsDbContext db)
    {
        // Idempotent — bail out if any user already exists
        if (await db.Users.AnyAsync()) return;

        /* ── Companies ────────────────────────────────────────────── */
        var techCo = new Company
        {
            Id          = Guid.NewGuid(),
            Name        = "TechEvents Co.",
            Slug        = "techevents-co",
            Description = "Malawi's leading technology event organiser.",
            Website     = "https://techevents.co",
            Email       = "hello@techevents.co",
            Phone       = "+265 999 100 001",
            Address     = "Area 3, Lilongwe, Malawi",
            IsActive    = true
        };

        var afrikaFest = new Company
        {
            Id          = Guid.NewGuid(),
            Name        = "Afrika Fest Productions",
            Slug        = "afrika-fest",
            Description = "Celebrating African culture through music and art.",
            Website     = "https://afrikafest.co",
            Email       = "info@afrikafest.co",
            Phone       = "+265 888 200 002",
            Address     = "Kamuzu Parade, Blantyre, Malawi",
            IsActive    = true
        };

        db.Companies.AddRange(techCo, afrikaFest);

        /* ── Users ────────────────────────────────────────────────── */
        var hash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);

        var superAdmin = new User
        {
            Id            = Guid.NewGuid(),
            FirstName     = "Super",
            LastName      = "Admin",
            Email         = "superadmin@smartevents.com",
            PasswordHash  = hash,
            Role          = UserRole.SuperAdmin,
            IsActive      = true,
            IsEmailVerified = true
        };

        var companyAdmin1 = new User
        {
            Id            = Guid.NewGuid(),
            FirstName     = "Grace",
            LastName      = "Banda",
            Email         = "admin@techevents.co",
            PasswordHash  = hash,
            Role          = UserRole.CompanyAdmin,
            CompanyId     = techCo.Id,
            IsActive      = true,
            IsEmailVerified = true
        };

        var organizer1 = new User
        {
            Id            = Guid.NewGuid(),
            FirstName     = "Chisomo",
            LastName      = "Phiri",
            Email         = "organizer@techevents.co",
            PasswordHash  = hash,
            Role          = UserRole.Organizer,
            CompanyId     = techCo.Id,
            IsActive      = true,
            IsEmailVerified = true
        };

        var companyAdmin2 = new User
        {
            Id            = Guid.NewGuid(),
            FirstName     = "Thandiwe",
            LastName      = "Mwale",
            Email         = "admin@afrikafest.co",
            PasswordHash  = hash,
            Role          = UserRole.CompanyAdmin,
            CompanyId     = afrikaFest.Id,
            IsActive      = true,
            IsEmailVerified = true
        };

        var organizer2 = new User
        {
            Id            = Guid.NewGuid(),
            FirstName     = "Kondwani",
            LastName      = "Chirwa",
            Email         = "organizer@afrikafest.co",
            PasswordHash  = hash,
            Role          = UserRole.Organizer,
            CompanyId     = afrikaFest.Id,
            IsActive      = true,
            IsEmailVerified = true
        };

        var attendee = new User
        {
            Id            = Guid.NewGuid(),
            FirstName     = "John",
            LastName      = "Doe",
            Email         = "attendee@example.com",
            PasswordHash  = hash,
            Role          = UserRole.Attendee,
            IsActive      = true,
            IsEmailVerified = true
        };

        db.Users.AddRange(superAdmin, companyAdmin1, organizer1, companyAdmin2, organizer2, attendee);

        /* ── Venues ───────────────────────────────────────────────── */
        var bicc = new Venue
        {
            Id          = Guid.NewGuid(),
            Name        = "BICC — Bingu International Convention Centre",
            Description = "State-of-the-art convention facility seating up to 5 000 delegates.",
            Address     = "Plot 15/272, Convention Drive",
            City        = "Lilongwe",
            Country     = "Malawi",
            Latitude    = -13.9626,
            Longitude   = 33.7741,
            Capacity    = 5000,
            Type        = VenueType.Indoor,
            Amenities   = "Wi-Fi, AV Equipment, Catering, Parking, AC",
            PricePerDay = 450000,
            CompanyId   = techCo.Id,
            IsAvailable = true
        };

        var sunbird = new Venue
        {
            Id          = Guid.NewGuid(),
            Name        = "Sunbird Lilongwe Hotel — Conference Centre",
            Description = "Premium hotel conference rooms for mid-size corporate events.",
            Address     = "Private Bag 340, Convention Square",
            City        = "Lilongwe",
            Country     = "Malawi",
            Latitude    = -13.9728,
            Longitude   = 33.7740,
            Capacity    = 800,
            Type        = VenueType.Indoor,
            Amenities   = "Wi-Fi, AV Equipment, Catering, Accommodation",
            PricePerDay = 220000,
            CompanyId   = techCo.Id,
            IsAvailable = true
        };

        var amphitheatre = new Venue
        {
            Id          = Guid.NewGuid(),
            Name        = "Area 18 Open-Air Amphitheatre",
            Description = "Scenic outdoor amphitheatre ideal for concerts and cultural festivals.",
            Address     = "Area 18, Off Mchinji Road",
            City        = "Lilongwe",
            Country     = "Malawi",
            Capacity    = 10000,
            Type        = VenueType.Outdoor,
            Amenities   = "Stage, Sound System, Generator, Parking",
            PricePerDay = 28000000,
            CompanyId   = afrikaFest.Id,
            IsAvailable = true
        };

        var blantyreSports = new Venue
        {
            Id          = Guid.NewGuid(),
            Name        = "Blantyre Sports Club",
            Description = "Versatile clubhouse and grounds for sports, networking and social events.",
            Address     = "Kidney Crescent, Blantyre",
            City        = "Blantyre",
            Country     = "Malawi",
            Capacity    = 1500,
            Type        = VenueType.Outdoor,
            Amenities   = "Bar, Catering, Parking, PA System",
            PricePerDay = 110000,
            CompanyId   = afrikaFest.Id,
            IsAvailable = true
        };

        var virtualStudio = new Venue
        {
            Id          = Guid.NewGuid(),
            Name        = "SmartEvents Virtual Studio",
            Description = "Live-streaming studio for webinars and hybrid events.",
            Address     = "Online",
            City        = "Lilongwe",
            Country     = "Malawi",
            Capacity    = 50000,
            Type        = VenueType.Virtual,
            Amenities   = "Live Stream, Recording, Chat, Q&A",
            PricePerDay = 28000,
            CompanyId   = techCo.Id,
            IsAvailable = true
        };

        db.Venues.AddRange(bicc, sunbird, amphitheatre, blantyreSports, virtualStudio);

        /* ── Events ───────────────────────────────────────────────── */
        var now = DateTime.UtcNow;

        var devSummit = new Event
        {
            Id           = Guid.NewGuid(),
            Title        = "Dev Summit Malawi 2026",
            Slug         = "dev-summit-malawi-2026",
            Description  = "Malawi's premier software development conference featuring talks on cloud, AI, mobile, and open-source. Join 400+ developers for two days of learning and networking.",
            Category     = EventCategory.Conference,
            Status       = EventStatus.Published,
            StartDate    = now.AddDays(30),
            EndDate      = now.AddDays(31),
            Timezone     = "Africa/Blantyre",
            MaxAttendees = 400,
            IsTicketed   = true,
            TicketPrice  = 8500,
            WaitlistEnabled = true,
            IsPublic     = true,
            Tags         = "tech,software,cloud,ai",
            CompanyId    = techCo.Id,
            VenueId      = bicc.Id,
            OrganizerId  = organizer1.Id
        };

        var angularWorkshop = new Event
        {
            Id           = Guid.NewGuid(),
            Title        = "Angular 18 Hands-On Workshop",
            Slug         = "angular-18-workshop",
            Description  = "A full-day practical workshop covering Angular 18 signals, standalone components, and building production-ready SPAs. Bring your laptop!",
            Category     = EventCategory.Workshop,
            Status       = EventStatus.Published,
            StartDate    = now.AddDays(14),
            EndDate      = now.AddDays(14).AddHours(8),
            Timezone     = "Africa/Blantyre",
            MaxAttendees = 60,
            IsTicketed   = false,
            WaitlistEnabled = true,
            IsPublic     = true,
            Tags         = "angular,frontend,javascript",
            CompanyId    = techCo.Id,
            VenueId      = sunbird.Id,
            OrganizerId  = organizer1.Id
        };

        var afrikaFestConcert = new Event
        {
            Id           = Guid.NewGuid(),
            Title        = "AfrikaFest Music Night 2026",
            Slug         = "afrikafest-music-night-2026",
            Description  = "A spectacular evening of Afrobeats, Gospel, and traditional Malawian music under the stars. Featuring top artists from across the continent.",
            Category     = EventCategory.Concert,
            Status       = EventStatus.Published,
            StartDate    = now.AddDays(21),
            EndDate      = now.AddDays(21).AddHours(6),
            Timezone     = "Africa/Blantyre",
            MaxAttendees = 8000,
            IsTicketed   = true,
            TicketPrice  = 4500,
            WaitlistEnabled = false,
            IsPublic     = true,
            Tags         = "music,afrobeats,culture,festival",
            CompanyId    = afrikaFest.Id,
            VenueId      = amphitheatre.Id,
            OrganizerId  = organizer2.Id
        };

        var networkingBrunch = new Event
        {
            Id           = Guid.NewGuid(),
            Title        = "Blantyre Tech Networking Brunch",
            Slug         = "blantyre-tech-networking-brunch",
            Description  = "A relaxed Sunday brunch for Blantyre's tech community. Meet founders, developers, and investors over great food and conversation.",
            Category     = EventCategory.Networking,
            Status       = EventStatus.Draft,
            StartDate    = now.AddDays(45),
            EndDate      = now.AddDays(45).AddHours(3),
            Timezone     = "Africa/Blantyre",
            MaxAttendees = 80,
            IsTicketed   = false,
            WaitlistEnabled = true,
            IsPublic     = true,
            Tags         = "networking,startups,blantyre",
            CompanyId    = techCo.Id,
            VenueId      = blantyreSports.Id,
            OrganizerId  = organizer1.Id
        };

        var aiWebinar = new Event
        {
            Id           = Guid.NewGuid(),
            Title        = "AI in Africa: Building for the Next Billion",
            Slug         = "ai-in-africa-webinar-2026",
            Description  = "A free online panel with AI practitioners from across Africa discussing real-world machine learning applications, data challenges, and the road ahead.",
            Category     = EventCategory.Webinar,
            Status       = EventStatus.Published,
            StartDate    = now.AddDays(7),
            EndDate      = now.AddDays(7).AddHours(2),
            Timezone     = "Africa/Blantyre",
            MaxAttendees = 5000,
            IsTicketed   = false,
            WaitlistEnabled = false,
            IsPublic     = true,
            Tags         = "ai,machinelearning,africa,webinar",
            CompanyId    = techCo.Id,
            VenueId      = virtualStudio.Id,
            OrganizerId  = organizer1.Id
        };

        var startupExpo = new Event
        {
            Id           = Guid.NewGuid(),
            Title        = "Malawi Startup Exhibition 2026",
            Slug         = "malawi-startup-exhibition-2026",
            Description  = "Showcase your startup to investors, corporates, and the public. Interactive demo booths, pitch competition, and keynotes from leading African entrepreneurs.",
            Category     = EventCategory.Exhibition,
            Status       = EventStatus.Published,
            StartDate    = now.AddDays(60),
            EndDate      = now.AddDays(61),
            Timezone     = "Africa/Blantyre",
            MaxAttendees = 1000,
            IsTicketed   = true,
            TicketPrice  = 2500,
            WaitlistEnabled = true,
            IsPublic     = true,
            Tags         = "startups,innovation,entrepreneurs,exhibition",
            CompanyId    = afrikaFest.Id,
            VenueId      = bicc.Id,
            OrganizerId  = organizer2.Id
        };

        db.Events.AddRange(devSummit, angularWorkshop, afrikaFestConcert, networkingBrunch, aiWebinar, startupExpo);

        await db.SaveChangesAsync();
    }
}
