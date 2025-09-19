using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<AppointmentType> AppointmentTypes { get; set; }
    public DbSet<RecurrenceRule> RecurrenceRules { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AppointmentAttendee> AppointmentAttendees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Configure AppointmentType entity
            modelBuilder.Entity<AppointmentType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Color).HasDefaultValue("#2196f3");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_AppointmentTypes_Color", 
                    "Color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'"));
            });

            // Configure RecurrenceRule entity
            modelBuilder.Entity<RecurrenceRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_RecurrenceRules_Frequency", 
                    "Frequency IN ('DAILY', 'WEEKLY', 'MONTHLY')"));
            });

            // Configure Appointment entity
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);

                // Foreign Key Relationships
                entity.HasOne(d => d.Organizer)
                    .WithMany(p => p.OrganizedAppointments)
                    .HasForeignKey(d => d.OrganizerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.AppointmentType)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(d => d.AppointmentTypeId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.RecurrenceRule)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(d => d.RecurrenceRuleId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.OrganizerId).HasDatabaseName("IX_Appointments_OrganizerId");
                entity.HasIndex(e => e.StartDateTime).HasDatabaseName("IX_Appointments_StartDateTime");
                entity.HasIndex(e => new { e.OrganizerId, e.StartDateTime }).HasDatabaseName("IX_Appointments_OrganizerStart");

                // Check Constraints
                // entity.ToTable(t => t.HasCheckConstraint(
                //     "CK_Appointments_DateTime", 
                //     "StartDateTime < EndDateTime"));
            });

            // Configure AppointmentAttendee entity
            modelBuilder.Entity<AppointmentAttendee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.IsOrganizer).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Foreign Key Relationships
                entity.HasOne(d => d.Appointment)
                    .WithMany(p => p.Attendees)
                    .HasForeignKey(d => d.AppointmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AttendeeAppointments)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_AppointmentAttendees_UserId");
                entity.HasIndex(e => e.AppointmentId).HasDatabaseName("IX_AppointmentAttendees_AppointmentId");

                // Unique Constraint
                entity.HasIndex(e => new { e.AppointmentId, e.UserId })
                    .IsUnique()
                    .HasDatabaseName("UQ_AppointmentAttendees_Unique");
            });

            // Seed Data with static values
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // ✅ Use static datetime values
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var appointmentTypes = new[]
            {
                new AppointmentType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Meeting",
                    Color = "#90caf9", // Soft Blue
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new AppointmentType
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Personal",
                    Color = "#a5d6a7", // Soft Green
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new AppointmentType
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Work",
                    Color = "#ffcc80", // Soft Orange
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new AppointmentType
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Health",
                    Color = "#f48fb1", // Soft Pink
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new AppointmentType
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "Travel",
                    Color = "#80deea", // Soft Cyan
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new AppointmentType
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Name = "Study",
                    Color = "#b39ddb", // Soft Purple
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new AppointmentType
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Name = "Family",
                    Color = "#ffe082", // Soft Yellow
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new AppointmentType
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Name = "Errands",
                    Color = "#bcaaa4", // Soft Brown/Neutral
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                }
            };


            modelBuilder.Entity<AppointmentType>().HasData(appointmentTypes);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is User user)
                {
                    if (entry.State == EntityState.Added)
                        user.CreatedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is Appointment appointment)
                {
                    if (entry.State == EntityState.Added)
                        appointment.CreatedAt = DateTime.UtcNow;
                    appointment.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is AppointmentType appointmentType)
                {
                    if (entry.State == EntityState.Added)
                        appointmentType.CreatedAt = DateTime.UtcNow;
                    appointmentType.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is RecurrenceRule recurrenceRule)
                {
                    if (entry.State == EntityState.Added)
                        recurrenceRule.CreatedAt = DateTime.UtcNow;
                    recurrenceRule.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is AppointmentAttendee attendee)
                {
                    if (entry.State == EntityState.Added)
                        attendee.CreatedAt = DateTime.UtcNow;
                    attendee.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
}