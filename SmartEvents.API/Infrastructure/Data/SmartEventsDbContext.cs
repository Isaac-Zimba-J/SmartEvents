using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Domain.Entities;

namespace SmartEvents.API.Infrastructure.Data;

public class SmartEventsDbContext(DbContextOptions<SmartEventsDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company
        modelBuilder.Entity<Company>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(200).IsRequired();
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Venue
        modelBuilder.Entity<Venue>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Name).HasMaxLength(200).IsRequired();
            e.Property(v => v.PricePerDay).HasColumnType("decimal(18,2)");
            e.HasOne(v => v.Company)
                .WithMany(c => c.Venues)
                .HasForeignKey(v => v.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Event
        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.HasIndex(ev => ev.Slug).IsUnique();
            e.Property(ev => ev.Title).HasMaxLength(300).IsRequired();
            e.Property(ev => ev.Slug).HasMaxLength(300).IsRequired();
            e.Property(ev => ev.TicketPrice).HasColumnType("decimal(18,2)");
            e.HasOne(ev => ev.Company)
                .WithMany(c => c.Events)
                .HasForeignKey(ev => ev.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ev => ev.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(ev => ev.VenueId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(ev => ev.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(ev => ev.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Registration
        modelBuilder.Entity<Registration>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.EventId, r.UserId }).IsUnique();
            e.HasOne(r => r.Event)
                .WithMany(ev => ev.Registrations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Ticket
        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.TicketNumber).IsUnique();
            e.HasOne(t => t.Registration)
                .WithOne(r => r.Ticket)
                .HasForeignKey<Ticket>(t => t.RegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Event)
                .WithMany(ev => ev.Tickets)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Payment
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Registration)
                .WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.RegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
