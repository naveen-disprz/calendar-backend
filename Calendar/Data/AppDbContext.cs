using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AppointmentParticipant> AppointmentParticipants { get; set; }
    public DbSet<RecurrenceRule> RecurrenceRules { get; set; }
    public DbSet<AppointmentType> AppointmentTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppointmentParticipant>()
            .HasKey(ap => new { ap.AppointmentId, ap.UserId });

        modelBuilder.Entity<AppointmentParticipant>()
            .HasOne<Appointment>() // specify the target entity
            .WithMany() // no navigation on the other side
            .HasForeignKey(ap => ap.AppointmentId) // set FK
            .OnDelete(DeleteBehavior.Cascade); // optional, you can change to Restrict/NoAction
    }
}