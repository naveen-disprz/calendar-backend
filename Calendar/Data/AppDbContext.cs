using Calendar.Models;
using Microsoft.EntityFrameworkCore;
using Type = Calendar.Models.Type;

namespace Calendar.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<RecurrenceRule> RecurrenceRules { get; set; }
    public DbSet<Type> Types { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Participant>()
            .HasKey(ap => new { ap.AppointmentId, ap.UserId });
        
        modelBuilder.Entity<Participant>()
            .HasOne(p => p.Appointment)
            .WithMany(a => a.Participants)
            .HasForeignKey(p => p.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete

        modelBuilder.Entity<Participant>()
            .HasOne(p => p.User)
            .WithMany(u => u.Participations)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}