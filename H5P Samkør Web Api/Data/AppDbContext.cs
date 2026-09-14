using Microsoft.EntityFrameworkCore;
using H5P_Samkør_Web_Api.Models;

namespace H5P_Samkør_Web_Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Rating> Ratings => Set<Rating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---- User ----
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);
        });

        // ---- Vehicle ----
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasOne(v => v.Owner)
                .WithMany(u => u.Vehicles)
                .HasForeignKey(v => v.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Trip ----
        modelBuilder.Entity<Trip>(e =>
        {
            e.Property(t => t.PricePerSeat).HasColumnType("decimal(10,2)");
            e.Property(t => t.RowVersion).IsRowVersion();

            e.HasOne(t => t.Driver)
                .WithMany(u => u.TripsAsDriver)
                .HasForeignKey(t => t.DriverId)
                // Restrict, ikke Cascade: SQL Server tillader ikke flere
                // cascade-veje til samme User-tabel (fra Driver, Passenger,
                // Sender, Rater og Ratee samtidig)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Vehicle)
                .WithMany(v => v.Trips)
                .HasForeignKey(t => t.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ---- Booking ----
        modelBuilder.Entity<Booking>(e =>
        {
            e.HasOne(b => b.Trip)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(b => b.Passenger)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Message ----
        modelBuilder.Entity<Message>(e =>
        {
            e.HasOne(m => m.Trip)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Rating ----
        modelBuilder.Entity<Rating>(e =>
        {
            e.Property(r => r.Stars).IsRequired();

            // Sikrer at en bruger kun kan give én bedømmelse
            // pr. medrejsende pr. tur
            e.HasIndex(r => new { r.TripId, r.RaterId, r.RateeId }).IsUnique();

            e.HasOne(r => r.Trip)
                .WithMany()
                .HasForeignKey(r => r.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Rater)
                .WithMany(u => u.RatingsGiven)
                .HasForeignKey(r => r.RaterId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Ratee)
                .WithMany(u => u.RatingsReceived)
                .HasForeignKey(r => r.RateeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
