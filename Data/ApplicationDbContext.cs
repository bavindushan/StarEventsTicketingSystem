using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StarEventsTicketingSystem.Models;

namespace StarEventsTicketingSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<LoyaltyPoints> LoyaltyPoints { get; set; }
        public DbSet<BookingHistory> BookingHistories { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Reports> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision
            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Discount>()
                .Property(d => d.DiscountPercentage)
                .HasColumnType("decimal(18,2)");

            // ApplicationUser 1:1 LoyaltyPoints
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.LoyaltyPoints)
                .WithOne(lp => lp.User)
                .HasForeignKey<LoyaltyPoints>(lp => lp.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser 1:M Events (Organizer)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Events)
                .WithOne(e => e.Organizer)
                .HasForeignKey(e => e.OrganizerID)
                .OnDelete(DeleteBehavior.Restrict);

            // Venue 1:M Events
            modelBuilder.Entity<Venue>()
                .HasMany(v => v.Events)
                .WithOne(e => e.Venue)
                .HasForeignKey(e => e.VenueID)
                .OnDelete(DeleteBehavior.SetNull);

            // Event 1:M Bookings
            modelBuilder.Entity<Event>()
                .HasMany(e => e.Bookings)
                .WithOne(b => b.Event)
                .HasForeignKey(b => b.EventID)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking 1:M Tickets
            modelBuilder.Entity<Booking>()
                .HasMany(b => b.Tickets)
                .WithOne(t => t.Booking)
                .HasForeignKey(t => t.BookingID)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking 1:1 Payment
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingID)
                .OnDelete(DeleteBehavior.Cascade);

            // Ticket 1:M BookingHistories
            modelBuilder.Entity<Ticket>()
                .HasMany(t => t.BookingHistories)
                .WithOne(bh => bh.Ticket)
                .HasForeignKey(bh => bh.TicketID)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser 1:M BookingHistories
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.BookingHistories)
                .WithOne(bh => bh.User)
                .HasForeignKey(bh => bh.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser 1:M AuditLogs
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.AuditLogs)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking → ApplicationUser
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
