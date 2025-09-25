using Xunit;
using FluentAssertions;
using Calendar.DataAccess;
using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calendar.Tests.DataAccess
{
    public class AppointmentAttendeeDALTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AppointmentAttendeeDAL _appointmentAttendeeDAL;

        public AppointmentAttendeeDALTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _appointmentAttendeeDAL = new AppointmentAttendeeDAL(_context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test users
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "organizer@example.com",
                    FirstName = "Organizer",
                    LastName = "User",
                    PasswordHash = "hash",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Email = "attendee1@example.com",
                    FirstName = "Attendee",
                    LastName = "One",
                    PasswordHash = "hash",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Email = "attendee2@example.com",
                    FirstName = "Attendee",
                    LastName = "Two",
                    PasswordHash = "hash",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);

            // Add test appointments
            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    OrganizerId = users[0].Id,
                    Title = "Test Meeting 1",
                    StartDateTime = DateTime.UtcNow.AddHours(1),
                    EndDateTime = DateTime.UtcNow.AddHours(2),
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Appointment
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    OrganizerId = users[1].Id,
                    Title = "Test Meeting 2",
                    StartDateTime = DateTime.UtcNow.AddHours(3),
                    EndDateTime = DateTime.UtcNow.AddHours(4),
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.Appointments.AddRange(appointments);
            _context.SaveChanges();
        }

        #region CreateAttendeesAsync Tests

        [Fact]
        public async Task CreateAttendeesAsync_WithMultipleAttendees_ShouldAddAllToDatabase()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var attendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    IsOrganizer = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    IsOrganizer = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            // Act
            var result = await _appointmentAttendeeDAL.            CreateAttendeesAsync(attendees);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1); // Only non-organizer returned based on GetAttendeesByAppointmentIdAsync logic
            
            var savedAttendees = await _context.AppointmentAttendees
                .Where(aa => aa.AppointmentId == appointmentId)
                .ToListAsync();
            
            savedAttendees.Should().HaveCount(2);
            savedAttendees.Should().Contain(aa => aa.IsOrganizer == true);
            savedAttendees.Should().Contain(aa => aa.IsOrganizer == false);
        }

        [Fact]
        public async Task CreateAttendeesAsync_WithEmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var attendees = new List<AppointmentAttendee>();

            // Act
            var result = await _appointmentAttendeeDAL.CreateAttendeesAsync(attendees);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            var count = await _context.AppointmentAttendees.CountAsync();
            count.Should().Be(0);
        }

        [Fact]
        public async Task CreateAttendeesAsync_ShouldReturnWithLoadedNavigationProperties()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var attendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    IsOrganizer = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            // Act
            var result = await _appointmentAttendeeDAL.CreateAttendeesAsync(attendees);

            // Assert
            result.Should().HaveCount(1);
            result[0].User.Should().NotBeNull();
            result[0].User.Email.Should().Be("attendee1@example.com");
            result[0].Appointment.Should().NotBeNull();
            result[0].Appointment.Title.Should().Be("Test Meeting 1");
        }

        #endregion

        #region CreateAttendeeAsync Tests

        [Fact]
        public async Task CreateAttendeeAsync_ShouldAddAttendeeToDatabase()
        {
            // Arrange
            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                IsOrganizer = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _appointmentAttendeeDAL.CreateAttendeeAsync(attendee);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(attendee.Id);
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be("attendee2@example.com");
            result.Appointment.Should().NotBeNull();
            result.Appointment.Title.Should().Be("Test Meeting 1");

            var savedAttendee = await _context.AppointmentAttendees
                .FirstOrDefaultAsync(aa => aa.Id == attendee.Id);
            savedAttendee.Should().NotBeNull();
        }

        #endregion

        #region GetAttendeesByAppointmentIdAsync Tests

        [Fact]
        public async Task GetAttendeesByAppointmentIdAsync_ShouldReturnOnlyNonOrganizers()
        {
            // Arrange
            var appointmentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            
            // Create appointment with attendees
            var appointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Test Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                IsDeleted = false
            };
            
            _context.Appointments.Add(appointment);
            
            var attendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    IsOrganizer = true
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    IsOrganizer = false
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    IsOrganizer = false
                }
            };
            
            _context.AppointmentAttendees.AddRange(attendees);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeesByAppointmentIdAsync(appointmentId);

            // Assert
            result.Should().HaveCount(2); // Only non-organizers
            result.Should().NotContain(aa => aa.IsOrganizer);
            result.Should().OnlyContain(aa => aa.User != null && aa.Appointment != null);
            result[0].User.FirstName.Should().Be("Attendee"); // Ordered by FirstName
        }

        [Fact]
        public async Task GetAttendeesByAppointmentIdAsync_WithNoAttendees_ShouldReturnEmptyList()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();

            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeesByAppointmentIdAsync(appointmentId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetAttendeesByUserIdAsync Tests

        [Fact]
        public async Task GetAttendeesByUserIdAsync_ShouldReturnAllAppointmentsForUser()
        {
            // Arrange
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Add attendee records
            var attendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    UserId = userId,
                    IsOrganizer = false
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    UserId = userId,
                    IsOrganizer = true
                }
            };
            
            _context.AppointmentAttendees.AddRange(attendees);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeesByUserIdAsync(userId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(aa => aa.UserId == userId);
            result.Should().OnlyContain(aa => !aa.Appointment.IsDeleted);
            result.Should().BeInDescendingOrder(aa => aa.Appointment.StartDateTime);
        }

        [Fact]
        public async Task GetAttendeesByUserIdAsync_ShouldExcludeDeletedAppointments()
        {
            // Arrange
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Add appointment with IsDeleted = true
            var deletedAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Deleted Meeting",
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddHours(1),
                IsDeleted = true
            };
            
            _context.Appointments.Add(deletedAppointment);
            
            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = deletedAppointment.Id,
                UserId = userId,
                IsOrganizer = false
            };
            
            _context.AppointmentAttendees.Add(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeesByUserIdAsync(userId);

            // Assert
            result.Should().NotContain(aa => aa.AppointmentId == deletedAppointment.Id);
        }

        #endregion

        #region GetAttendeeAsync Tests

        [Fact]
        public async Task GetAttendeeAsync_WithExistingAttendee_ShouldReturnAttendee()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = userId,
                IsOrganizer = false
            };
            
            _context.AppointmentAttendees.Add(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeeAsync(appointmentId, userId);

            // Assert
            result.Should().NotBeNull();
            result!.AppointmentId.Should().Be(appointmentId);
            result.UserId.Should().Be(userId);
            result.User.Should().NotBeNull();
            result.Appointment.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAttendeeAsync_WithNonExistingAttendee_ShouldReturnNull()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeeAsync(appointmentId, userId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region IsOrganizerAsync Tests

        [Fact]
        public async Task IsOrganizerAsync_WithOrganizer_ShouldReturnTrue()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = userId,
                IsOrganizer = true
            };
            
            _context.AppointmentAttendees.Add(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.IsOrganizerAsync(appointmentId, userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsOrganizerAsync_WithNonOrganizer_ShouldReturnFalse()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = userId,
                IsOrganizer = false
            };
            
            _context.AppointmentAttendees.Add(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.IsOrganizerAsync(appointmentId, userId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsOrganizerAsync_WithNonExistentAttendee_ShouldReturnFalse()
        {
            // Act
            var result = await _appointmentAttendeeDAL.IsOrganizerAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IsAttendeeAsync Tests

        [Fact]
        public async Task IsAttendeeAsync_WithExistingAttendee_ShouldReturnTrue()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = userId,
                IsOrganizer = false
            };
            
            _context.AppointmentAttendees.Add(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.IsAttendeeAsync(appointmentId, userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAttendeeAsync_WithNonExistentAttendee_ShouldReturnFalse()
        {
            // Act
            var result = await _appointmentAttendeeDAL.IsAttendeeAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region DeleteAttendeeAsync Tests

        [Fact]
        public async Task DeleteAttendeeAsync_WithNonOrganizerAttendee_ShouldDeleteSuccessfully()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = userId,
                IsOrganizer = false
            };
            
            _context.AppointmentAttendees.Add(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.DeleteAttendeeAsync(appointmentId, userId);

            // Assert
            result.Should().BeTrue();
            
            var deletedAttendee = await _context.AppointmentAttendees
                .FirstOrDefaultAsync(aa => aa.AppointmentId == appointmentId && aa.UserId == userId);
            deletedAttendee.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAttendeeAsync_WithOrganizer_ShouldReturnFalse()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            var organizer = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = userId,
                IsOrganizer = true
            };
            
            _context.AppointmentAttendees.Add(organizer);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.DeleteAttendeeAsync(appointmentId, userId);

            // Assert
            result.Should().BeFalse();
            
            var stillExists = await _context.AppointmentAttendees
                .AnyAsync(aa => aa.AppointmentId == appointmentId && aa.UserId == userId);
            stillExists.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAttendeeAsync_WithNonExistentAttendee_ShouldReturnFalse()
        {
            // Act
            var result = await _appointmentAttendeeDAL.DeleteAttendeeAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region DeleteAllAttendeesAsync Tests

        [Fact]
        public async Task DeleteAllAttendeesAsync_WithMultipleAttendees_ShouldDeleteAll()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            
            var attendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    IsOrganizer = true
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    IsOrganizer = false
                }
            };
            
            _context.AppointmentAttendees.AddRange(attendees);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.DeleteAllAttendeesAsync(appointmentId);

            // Assert
            result.Should().BeTrue();
            
            var remainingAttendees = await _context.AppointmentAttendees
                .Where(aa => aa.AppointmentId == appointmentId)
                .ToListAsync();
            remainingAttendees.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteAllAttendeesAsync_WithNoAttendees_ShouldReturnFalse()
        {
            // Act
            var result = await _appointmentAttendeeDAL.DeleteAllAttendeesAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetAttendeeCountAsync Tests

        [Fact]
        public async Task GetAttendeeCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            
            var attendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    IsOrganizer = true
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    IsOrganizer = false
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    IsOrganizer = false
                }
            };
            
            _context.AppointmentAttendees.AddRange(attendees);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeeCountAsync(appointmentId);

            // Assert
            result.Should().Be(3);
        }

        [Fact]
        public async Task GetAttendeeCountAsync_WithNoAttendees_ShouldReturnZero()
        {
            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeeCountAsync(Guid.NewGuid());

            // Assert
            result.Should().Be(0);
        }

        #endregion

        #region UpdateAttendeesAsync Tests

        [Fact]
        public async Task UpdateAttendeesAsync_ShouldAddNewAndRemoveOldAttendees()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var organizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var oldAttendeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var newAttendeeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            
            // Add existing attendees
            var existingAttendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = organizerId,
                    IsOrganizer = true
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = oldAttendeeId,
                    IsOrganizer = false
                }
            };
            
            _context.AppointmentAttendees.AddRange(existingAttendees);
            await _context.SaveChangesAsync();

            // Act - Keep organizer, remove old attendee, add new attendee
            var newAttendeeIds = new List<Guid> { organizerId, newAttendeeId };
            var result = await _appointmentAttendeeDAL.UpdateAttendeesAsync(appointmentId, newAttendeeIds, organizerId);

            // Assert
            result.Should().HaveCount(1); // Returns only non-organizers
            
            var allAttendees = await _context.AppointmentAttendees
                .Where(aa => aa.AppointmentId == appointmentId)
                .ToListAsync();
            
            allAttendees.Should().HaveCount(2);
            allAttendees.Should().Contain(aa => aa.UserId == organizerId && aa.IsOrganizer);
            allAttendees.Should().Contain(aa => aa.UserId == newAttendeeId && !aa.IsOrganizer);
            allAttendees.Should().NotContain(aa => aa.UserId == oldAttendeeId);
        }

        [Fact]
        public async Task UpdateAttendeesAsync_ShouldNotRemoveOrganizer()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var organizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var attendeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            var existingAttendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = organizerId,
                    IsOrganizer = true
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = attendeeId,
                    IsOrganizer = false
                }
            };
            
            _context.AppointmentAttendees.AddRange(existingAttendees);
            await _context.SaveChangesAsync();

            // Act - Try to update without organizer in the list
            var newAttendeeIds = new List<Guid> { attendeeId };
            await _appointmentAttendeeDAL.UpdateAttendeesAsync(appointmentId, newAttendeeIds, organizerId);

            // Assert - Organizer should still exist
            var organizer = await _context.AppointmentAttendees
                .FirstOrDefaultAsync(aa => aa.AppointmentId == appointmentId && aa.UserId == organizerId);
            organizer.Should().NotBeNull();
            organizer!.IsOrganizer.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAttendeesAsync_ShouldUpdateTimestamps()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var organizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var attendeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            var originalTime = DateTime.UtcNow;
            var existingAttendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = attendeeId,
                IsOrganizer = false,
                CreatedAt = originalTime,
                UpdatedAt = originalTime
            };
            
            _context.AppointmentAttendees.Add(existingAttendee);
            await _context.SaveChangesAsync();

            // Act - Keep same attendee
            var newAttendeeIds = new List<Guid> { attendeeId };
            await _appointmentAttendeeDAL.UpdateAttendeesAsync(appointmentId, newAttendeeIds, organizerId);

            // Assert
            var updatedAttendee = await _context.AppointmentAttendees
                .FirstOrDefaultAsync(aa => aa.AppointmentId == appointmentId && aa.UserId == attendeeId);
            
            updatedAttendee.Should().NotBeNull();
            updatedAttendee!.UpdatedAt.Should().BeAfter(originalTime);
            updatedAttendee.CreatedAt.Should().Be(originalTime);
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_WithExistingAttendee_ShouldReturnTrue()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = userId,
                IsOrganizer = false
            };
            
            _context.AppointmentAttendees.Add(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.ExistsAsync(appointmentId, userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistentAttendee_ShouldReturnFalse()
        {
            // Act
            var result = await _appointmentAttendeeDAL.ExistsAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task UpdateAttendeesAsync_WithEmptyNewList_ShouldRemoveAllNonOrganizerAttendees()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var organizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            var existingAttendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = organizerId,
                    IsOrganizer = true
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    IsOrganizer = false
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    IsOrganizer = false
                }
            };
            
            _context.AppointmentAttendees.AddRange(existingAttendees);
            await _context.SaveChangesAsync();

            // Act - Empty list should remove all non-organizer attendees
            var result = await _appointmentAttendeeDAL.UpdateAttendeesAsync(appointmentId, new List<Guid>(), organizerId);

            // Assert
            result.Should().BeEmpty(); // No non-organizers left
            
            var remainingAttendees = await _context.AppointmentAttendees
                .Where(aa => aa.AppointmentId == appointmentId)
                .ToListAsync();
            
            remainingAttendees.Should().HaveCount(1);
            remainingAttendees[0].IsOrganizer.Should().BeTrue();
            remainingAttendees[0].UserId.Should().Be(organizerId);
        }

        [Fact]
        public async Task UpdateAttendeesAsync_WithDuplicateUserIds_ShouldHandleGracefully()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var organizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var attendeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            // Act - Pass duplicate user IDs
            var newAttendeeIds = new List<Guid> { attendeeId, attendeeId, attendeeId };
            var result = await _appointmentAttendeeDAL.UpdateAttendeesAsync(appointmentId, newAttendeeIds, organizerId);

            // Assert
            var allAttendees = await _context.AppointmentAttendees
                .Where(aa => aa.AppointmentId == appointmentId)
                .ToListAsync();
            
            // Should only create one attendee for the duplicate ID
            allAttendees.Count(aa => aa.UserId == attendeeId).Should().Be(1);
        }

        [Fact]
        public async Task GetAttendeesByAppointmentIdAsync_ShouldOrderByOrganizerThenName()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var appointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Test Meeting",
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddHours(1),
                IsDeleted = false
            };
            
            _context.Appointments.Add(appointment);
            
            // Create users with different names for ordering test
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "zach@example.com",
                    FirstName = "Zach",
                    LastName = "Anderson",
                    PasswordHash = "hash",
                    IsActive = true
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "alice@example.com",
                    FirstName = "Alice",
                    LastName = "Brown",
                    PasswordHash = "hash",
                    IsActive = true
                }
            };
            
            _context.Users.AddRange(users);
            
            var attendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = users[0].Id, // Zach
                    IsOrganizer = false
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = users[1].Id, // Alice
                    IsOrganizer = false
                }
            };
            
            _context.AppointmentAttendees.AddRange(attendees);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeesByAppointmentIdAsync(appointmentId);

            // Assert
            result.Should().HaveCount(2);
            result[0].User.FirstName.Should().Be("Alice"); // Alphabetically first
            result[1].User.FirstName.Should().Be("Zach");
        }

        [Fact]
        public async Task CreateAttendeesAsync_WithLargeNumberOfAttendees_ShouldHandleEfficiently()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var attendees = new List<AppointmentAttendee>();
            
            // Create 100 attendees
            for (int i = 0; i < 100; i++)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = $"user{i}@example.com",
                    FirstName = $"User{i}",
                    LastName = "Test",
                    PasswordHash = "hash",
                    IsActive = true
                };
                
                _context.Users.Add(user);
                
                attendees.Add(new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = user.Id,
                    IsOrganizer = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.CreateAttendeesAsync(attendees);

            // Assert
            result.Should().HaveCount(100);
            
            var savedCount = await _context.AppointmentAttendees
                .Where(aa => aa.AppointmentId == appointmentId)
                .CountAsync();
            savedCount.Should().Be(100);
        }

        [Fact]
        public async Task DeleteAttendeeAsync_ShouldOnlyDeleteSpecificAttendee()
        {
            // Arrange
            var appointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var userId2 = Guid.Parse("33333333-3333-3333-3333-333333333333");
            
            var attendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = userId1,
                    IsOrganizer = false
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = userId2,
                    IsOrganizer = false
                }
            };
            
            _context.AppointmentAttendees.AddRange(attendees);
            await _context.SaveChangesAsync();

            // Act - Delete only first attendee
            var result = await _appointmentAttendeeDAL.DeleteAttendeeAsync(appointmentId, userId1);

            // Assert
            result.Should().BeTrue();
            
            var remainingAttendees = await _context.AppointmentAttendees
                .Where(aa => aa.AppointmentId == appointmentId)
                .ToListAsync();
            
            remainingAttendees.Should().HaveCount(1);
            remainingAttendees[0].UserId.Should().Be(userId2);
        }

        [Fact]
        public async Task GetAttendeesByUserIdAsync_ShouldIncludeOrganizerInformation()
        {
            // Arrange
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var organizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                UserId = userId,
                IsOrganizer = false
            };
            
            _context.AppointmentAttendees.Add(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentAttendeeDAL.GetAttendeesByUserIdAsync(userId);

            // Assert
            result.Should().HaveCount(1);
            result[0].Appointment.Should().NotBeNull();
            result[0].Appointment.Organizer.Should().NotBeNull();
            result[0].Appointment.Organizer.Id.Should().Be(organizerId);
            result[0].Appointment.Organizer.Email.Should().Be("organizer@example.com");
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}


