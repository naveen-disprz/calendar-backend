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
    public class AppointmentDALTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AppointmentDAL _appointmentDAL;
        private readonly Guid _userId1;
        private readonly Guid _userId2;
        private readonly Guid _appointmentTypeId;

        public AppointmentDALTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _appointmentDAL = new AppointmentDAL(_context);

            // Setup test data
            _userId1 = Guid.NewGuid();
            _userId2 = Guid.NewGuid();
            _appointmentTypeId = Guid.NewGuid();

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Add test users
            var user1 = new User
            {
                Id = _userId1,
                Email = "user1@example.com",
                FirstName = "User",
                LastName = "One",
                PasswordHash = "hash1",
                IsActive = true
            };

            var user2 = new User
            {
                Id = _userId2,
                Email = "user2@example.com",
                FirstName = "User",
                LastName = "Two",
                PasswordHash = "hash2",
                IsActive = true
            };

            // Add test appointment type
            var appointmentType = new AppointmentType
            {
                Id = _appointmentTypeId,
                Name = "Meeting",
                Color = "#FF0000"
            };

            await _context.Users.AddRangeAsync(user1, user2);
            await _context.AppointmentTypes.AddAsync(appointmentType);
            await _context.SaveChangesAsync();
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldAddAppointmentToDatabase()
        {
            // Arrange
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "New Meeting",
                Description = "Test meeting description",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Location = "Conference Room A",
                AppointmentTypeId = _appointmentTypeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _appointmentDAL.CreateAsync(appointment);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(appointment.Id);
            result.Title.Should().Be(appointment.Title);
            result.Description.Should().Be(appointment.Description);

            // Verify saved to database
            var savedAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointment.Id);
            savedAppointment.Should().NotBeNull();
            savedAppointment!.Title.Should().Be(appointment.Title);
            savedAppointment.OrganizerId.Should().Be(_userId1);
            savedAppointment.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_WithRecurrenceRule_ShouldSaveWithRecurrence()
        {
            // Arrange
            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "WEEKLY",
                EndDate = DateTime.UtcNow.AddMonths(3)
            };

            await _context.RecurrenceRules.AddAsync(recurrenceRule);
            await _context.SaveChangesAsync();

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Recurring Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                RecurrenceRuleId = recurrenceRule.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _appointmentDAL.CreateAsync(appointment);

            // Assert
            result.Should().NotBeNull();
            result.RecurrenceRuleId.Should().Be(recurrenceRule.Id);

            var savedAppointment = await _context.Appointments
                .Include(a => a.RecurrenceRule)
                .FirstOrDefaultAsync(a => a.Id == appointment.Id);
            savedAppointment!.RecurrenceRuleId.Should().Be(recurrenceRule.Id);
            savedAppointment.IsRecurring.Should().BeTrue();
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldUpdateExistingAppointment()
        {
            // Arrange
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Original Title",
                Description = "Original Description",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Location = "Room A",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();
            _context.Entry(appointment).State = EntityState.Detached;

            // Modify the appointment
            appointment.Title = "Updated Title";
            appointment.Description = "Updated Description";
            appointment.Location = "Room B";
            appointment.UpdatedAt = DateTime.UtcNow;

            // Act
            var result = await _appointmentDAL.UpdateAsync(appointment);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");
            result.Description.Should().Be("Updated Description");
            result.Location.Should().Be("Room B");

            // Verify in database
            var updatedAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointment.Id);
            updatedAppointment!.Title.Should().Be("Updated Title");
            updatedAppointment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        #endregion

        #region GetConflictingAppointmentsAsync Tests

        [Fact]
        public async Task GetConflictingAppointmentsAsync_ShouldReturnOverlappingAppointments()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(2);
            
            // Create appointments
            var appointment1 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Appointment 1",
                StartDateTime = baseTime,
                EndDateTime = baseTime.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            var appointment2 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Appointment 2 (Overlapping)",
                StartDateTime = baseTime.AddMinutes(30),
                EndDateTime = baseTime.AddHours(1).AddMinutes(30),
                CreatedAt = DateTime.UtcNow
            };

            var appointment3 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Appointment 3 (Non-overlapping)",
                StartDateTime = baseTime.AddHours(2),
                EndDateTime = baseTime.AddHours(3),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(appointment1, appointment2, appointment3);
            await _context.SaveChangesAsync();

            // Act
            var conflicts = await _appointmentDAL.GetConflictingAppointmentsAsync(
                baseTime.AddMinutes(45),
                baseTime.AddHours(1).AddMinutes(15),
                _userId1);

            // Assert
            conflicts.Should().HaveCount(2);
            conflicts.Should().Contain(a => a.Title == "Appointment 1");
            conflicts.Should().Contain(a => a.Title == "Appointment 2 (Overlapping)");
            conflicts.Should().NotContain(a => a.Title == "Appointment 3 (Non-overlapping)");
        }

        [Fact]
        public async Task GetConflictingAppointmentsAsync_ShouldExcludeSpecifiedAppointment()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(2);
            var excludeId = Guid.NewGuid();

            var appointment1 = new Appointment
            {
                Id = excludeId,
                OrganizerId = _userId1,
                Title = "Excluded Appointment",
                StartDateTime = baseTime,
                EndDateTime = baseTime.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            var appointment2 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Included Appointment",
                StartDateTime = baseTime.AddMinutes(30),
                EndDateTime = baseTime.AddHours(1).AddMinutes(30),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(appointment1, appointment2);
            await _context.SaveChangesAsync();

            // Act
            var conflicts = await _appointmentDAL.GetConflictingAppointmentsAsync(
                baseTime,
                baseTime.AddHours(1),
                _userId1,
                excludeId);

            // Assert
            conflicts.Should().HaveCount(1);
            conflicts.Should().Contain(a => a.Title == "Included Appointment");
            conflicts.Should().NotContain(a => a.Id == excludeId);
        }

        [Fact]
        public async Task GetConflictingAppointmentsAsync_ShouldIncludeAppointmentsWhereUserIsAttendee()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(2);
            
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId2, // Different organizer
                Title = "Meeting as Attendee",
                StartDateTime = baseTime,
                EndDateTime = baseTime.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                UserId = _userId1, // User is attendee
                IsOrganizer = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.AppointmentAttendees.AddAsync(attendee);
            await _context.SaveChangesAsync();

            // Act
            var conflicts = await _appointmentDAL.GetConflictingAppointmentsAsync(
                baseTime,
                baseTime.AddHours(1),
                _userId1);

            // Assert
            conflicts.Should().HaveCount(1);
            conflicts.First().Title.Should().Be("Meeting as Attendee");
        }

        [Fact]
        public async Task GetConflictingAppointmentsAsync_ShouldExcludeDeletedAppointments()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(2);
            
            var activeAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Active Appointment",
                StartDateTime = baseTime,
                EndDateTime = baseTime.AddHours(1),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var deletedAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Deleted Appointment",
                StartDateTime = baseTime.AddMinutes(30),
                EndDateTime = baseTime.AddHours(1).AddMinutes(30),
                IsDeleted = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(activeAppointment, deletedAppointment);
            await _context.SaveChangesAsync();

            // Act
            var conflicts = await _appointmentDAL.GetConflictingAppointmentsAsync(
                baseTime,
                baseTime.AddHours(2),
                _userId1);

            // Assert
            conflicts.Should().HaveCount(1);
            conflicts.Should().Contain(a => a.Title == "Active Appointment");
            conflicts.Should().NotContain(a => a.IsDeleted);
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_WithExistingAppointment_ShouldReturnTrue()
        {
            // Arrange
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Existing Appointment",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            // Act
            var exists = await _appointmentDAL.ExistsAsync(appointment.Id);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistentAppointment_ShouldReturnFalse()
        {
            // Act
            var exists = await _appointmentDAL.ExistsAsync(Guid.NewGuid());

            // Assert
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsAsync_WithDeletedAppointment_ShouldReturnFalse()
        {
            // Arrange
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Deleted Appointment",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                IsDeleted = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            // Act
            var exists = await _appointmentDAL.ExistsAsync(appointment.Id);

            // Assert
            exists.Should().BeFalse();
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithExistingAppointment_ShouldReturnWithIncludes()
        {
            // Arrange
            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "WEEKLY",
                EndDate = DateTime.UtcNow.AddMonths(3)
            };

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Complete Appointment",
                Description = "With all relationships",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Location = "Room A",
                AppointmentTypeId = _appointmentTypeId,
                RecurrenceRuleId = recurrenceRule.Id,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                UserId = _userId2,
                IsOrganizer = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.RecurrenceRules.AddAsync(recurrenceRule);
            await _context.Appointments.AddAsync(appointment);
            await _context.AppointmentAttendees.AddAsync(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetByIdAsync(appointment.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(appointment.Id);
            result.Title.Should().Be("Complete Appointment");
            
            // Verify includes
            result.Organizer.Should().NotBeNull();
            result.Organizer.Id.Should().Be(_userId1);
            
            result.AppointmentType.Should().NotBeNull();
            result.AppointmentType!.Name.Should().Be("Meeting");
            
            result.RecurrenceRule.Should().NotBeNull();
            result.RecurrenceRule!.Frequency.Should().Be("WEEKLY");
            
            result.Attendees.Should().HaveCount(1);
            result.Attendees.First().User.Should().NotBeNull();
            result.Attendees.First().User.Id.Should().Be(_userId2);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentAppointment_ShouldReturnNull()
        {
            // Act
            var result = await _appointmentDAL.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithDeletedAppointment_ShouldReturnNull()
        {
            // Arrange
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Deleted Appointment",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                IsDeleted = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetByIdAsync(appointment.Id);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetAppointmentsByDateRangeAsync Tests

        [Fact]
        public async Task GetAppointmentsByDateRangeAsync_ShouldReturnAppointmentsInRange()
        {
            // Arrange
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);

            var appointment1 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Appointment in Range",
                StartDateTime = fromDate.AddDays(1).AddHours(10),
                EndDateTime = fromDate.AddDays(1).AddHours(11),
                CreatedAt = DateTime.UtcNow
            };

            var appointment2 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Appointment Before Range",
                StartDateTime = fromDate.AddDays(-2).AddHours(10),
                EndDateTime = fromDate.AddDays(-2).AddHours(11),
                CreatedAt = DateTime.UtcNow
            };

            var appointment3 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Appointment After Range",
                StartDateTime = toDate.AddDays(2).AddHours(10),
                EndDateTime = toDate.AddDays(2).AddHours(11),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(appointment1, appointment2, appointment3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetAppointmentsByDateRangeAsync(
                _userId1, fromDate, toDate, null, true);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(a => a.Title == "Appointment in Range");
            result.Should().NotContain(a => a.Title == "Appointment Before Range");
            result.Should().NotContain(a => a.Title == "Appointment After Range");
        }

        [Fact]
        public async Task GetAppointmentsByDateRangeAsync_ShouldIncludeRecurringAppointments()
        {
            // Arrange
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);

            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                EndDate = toDate.AddDays(30)
            };

            var recurringAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Daily Meeting",
                StartDateTime = fromDate.AddDays(-10).AddHours(10), // Started before range
                EndDateTime = fromDate.AddDays(-10).AddHours(11),
                RecurrenceRuleId = recurrenceRule.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _context.RecurrenceRules.AddAsync(recurrenceRule);
            await _context.Appointments.AddAsync(recurringAppointment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetAppointmentsByDateRangeAsync(
                _userId1, fromDate, toDate, null, true);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Daily Meeting");
            result.First().IsRecurring.Should().BeTrue();
        }

        [Fact]
        public async Task GetAppointmentsByDateRangeAsync_WithIncludeRecurringFalse_ShouldExcludeRecurring()
        {
            // Arrange
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);

            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                EndDate = toDate
            };

            var regularAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Regular Meeting",
                StartDateTime = fromDate.AddDays(1).AddHours(10),
                EndDateTime = fromDate.AddDays(1).AddHours(11),
                CreatedAt = DateTime.UtcNow
            };

            var recurringAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Recurring Meeting",
                StartDateTime = fromDate.AddDays(1).AddHours(14),
                EndDateTime = fromDate.AddDays(1).AddHours(15),
                RecurrenceRuleId = recurrenceRule.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _context.RecurrenceRules.AddAsync(recurrenceRule);
            await _context.Appointments.AddRangeAsync(regularAppointment, recurringAppointment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetAppointmentsByDateRangeAsync(
                _userId1, fromDate, toDate, null, false);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(a => a.Title == "Regular Meeting");
            result.Should().NotContain(a => a.Title == "Recurring Meeting");
        }

        [Fact]
        public async Task GetAppointmentsByDateRangeAsync_WithAppointmentTypeFilter_ShouldFilterByType()
        {
            // Arrange
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);
            var otherTypeId = Guid.NewGuid();

            var otherType = new AppointmentType
            {
                Id = otherTypeId,
                Name = "Personal",
                Color = "#00FF00"
            };

            await _context.AppointmentTypes.AddAsync(otherType);

            var appointment1 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Meeting Type Appointment",
                StartDateTime = fromDate.AddDays(1).AddHours(10),
                EndDateTime = fromDate.AddDays(1).AddHours(11),
                AppointmentTypeId = _appointmentTypeId,
                CreatedAt = DateTime.UtcNow
            };

            var appointment2 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Personal Type Appointment",
                StartDateTime = fromDate.AddDays(1).AddHours(14),
                EndDateTime = fromDate.AddDays(1).AddHours(15),
                AppointmentTypeId = otherTypeId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(appointment1, appointment2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetAppointmentsByDateRangeAsync(
                _userId1, fromDate, toDate, _appointmentTypeId, true);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(a => a.Title == "Meeting Type Appointment");
            result.Should().NotContain(a => a.Title == "Personal Type Appointment");
        }

        [Fact]
        public async Task GetAppointmentsByDateRangeAsync_ShouldIncludeAppointmentsWhereUserIsAttendee()
        {
            // Arrange
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId2, // Different organizer
                Title = "Meeting as Attendee",
                StartDateTime = fromDate.AddDays(1).AddHours(10),
                EndDateTime = fromDate.AddDays(1).AddHours(11),
                CreatedAt = DateTime.UtcNow
            };

            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                UserId = _userId1, // User is attendee
                IsOrganizer = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.AppointmentAttendees.AddAsync(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetAppointmentsByDateRangeAsync(
                _userId1, fromDate, toDate, null, true);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Meeting as Attendee");
        }

        [Fact]
        public async Task GetAppointmentsByDateRangeAsync_ShouldHandleAppointmentsSpanningDateRange()
        {
            // Arrange
            var fromDate = DateTime.Today.AddHours(12); // Noon today
            var toDate = DateTime.Today.AddDays(2).AddHours(12); // Noon in 2 days

            // Appointment starts before range, ends within range
            var appointment1 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Spans Start",
                StartDateTime = fromDate.AddHours(-2),
                EndDateTime = fromDate.AddHours(2),
                CreatedAt = DateTime.UtcNow
            };

            // Appointment starts within range, ends after range
            var appointment2 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Spans End",
                StartDateTime = toDate.AddHours(-2),
                EndDateTime = toDate.AddHours(2),
                CreatedAt = DateTime.UtcNow
            };

            // Appointment completely spans the range
            var appointment3 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Spans Entire Range",
                StartDateTime = fromDate.AddDays(-1),
                EndDateTime = toDate.AddDays(1),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(appointment1, appointment2, appointment3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetAppointmentsByDateRangeAsync(
                _userId1, fromDate, toDate, null, true);

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(a => a.Title == "Spans Start");
            result.Should().Contain(a => a.Title == "Spans End");
            result.Should().Contain(a => a.Title == "Spans Entire Range");
        }

        [Fact]
        public async Task GetAppointmentsByDateRangeAsync_ShouldOrderByStartDateTime()
        {
            // Arrange
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);

            var appointment1 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Third",
                StartDateTime = fromDate.AddDays(3).AddHours(10),
                EndDateTime = fromDate.AddDays(3).AddHours(11),
                CreatedAt = DateTime.UtcNow
            };

            var appointment2 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "First",
                StartDateTime = fromDate.AddDays(1).AddHours(10),
                EndDateTime = fromDate.AddDays(1).AddHours(11),
                CreatedAt = DateTime.UtcNow
            };

            var appointment3 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Second",
                StartDateTime = fromDate.AddDays(2).AddHours(10),
                EndDateTime = fromDate.AddDays(2).AddHours(11),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(appointment1, appointment2, appointment3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetAppointmentsByDateRangeAsync(
                _userId1, fromDate, toDate, null, true);

            // Assert
            result.Should().HaveCount(3);
            result[0].Title.Should().Be("First");
            result[1].Title.Should().Be("Second");
            result[2].Title.Should().Be("Third");
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithExistingAppointment_ShouldSoftDelete()
        {
            // Arrange
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "To Be Deleted",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            var beforeDelete = DateTime.UtcNow;

            // Act
            var result = await _appointmentDAL.DeleteAsync(appointment.Id);

            // Assert
            result.Should().BeTrue();

            // Verify soft delete
            var deletedAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointment.Id);
            
            deletedAppointment.Should().NotBeNull();
            deletedAppointment!.IsDeleted.Should().BeTrue();
            deletedAppointment.UpdatedAt.Should().BeAfter(beforeDelete);
            deletedAppointment.Title.Should().Be("To Be Deleted"); // Data still exists
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentAppointment_ShouldReturnFalse()
        {
            // Act
            var result = await _appointmentDAL.DeleteAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_WithAlreadyDeletedAppointment_ShouldReturnFalse()
        {
            // Arrange
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Already Deleted",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                IsDeleted = true, // Already deleted
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.DeleteAsync(appointment.Id);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetPotentialConflictingAppointmentsAsync Tests

        [Fact]
        public async Task GetPotentialConflictingAppointmentsAsync_ShouldReturnDirectConflicts()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(2);

            var conflictingAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Direct Conflict",
                StartDateTime = baseTime.AddMinutes(30),
                EndDateTime = baseTime.AddHours(1).AddMinutes(30),
                CreatedAt = DateTime.UtcNow
            };

            var nonConflictingAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "No Conflict",
                StartDateTime = baseTime.AddHours(3),
                EndDateTime = baseTime.AddHours(4),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(conflictingAppointment, nonConflictingAppointment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetPotentialConflictingAppointmentsAsync(
                baseTime, baseTime.AddHours(2), _userId1, null);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(a => a.Title == "Direct Conflict");
            result.Should().NotContain(a => a.Title == "No Conflict");
        }

        [Fact]
        public async Task GetPotentialConflictingAppointmentsAsync_ShouldIncludeRecurringAppointments()
        {
            // Arrange
            var checkDate = DateTime.Today.AddDays(10);
            var checkStart = checkDate.AddHours(10);
            var checkEnd = checkDate.AddHours(11);

            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                EndDate = checkDate.AddDays(20) // Ends after check date
            };

            var recurringAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Daily Recurring",
                StartDateTime = DateTime.Today.AddHours(10), // Started before check date
                EndDateTime = DateTime.Today.AddHours(11),
                RecurrenceRuleId = recurrenceRule.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _context.RecurrenceRules.AddAsync(recurrenceRule);
            await _context.Appointments.AddAsync(recurringAppointment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetPotentialConflictingAppointmentsAsync(
                checkStart, checkEnd, _userId1, null);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Daily Recurring");
            result.First().IsRecurring.Should().BeTrue();
        }

        [Fact]
        public async Task GetPotentialConflictingAppointmentsAsync_ShouldExcludeRecurringThatEndedBefore()
        {
            // Arrange
            var checkDate = DateTime.Today.AddDays(30);
            var checkStart = checkDate.AddHours(10);
            var checkEnd = checkDate.AddHours(11);

            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                EndDate = DateTime.Today.AddDays(10) // Ends before check date
            };

            var recurringAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Ended Recurring",
                StartDateTime = DateTime.Today.AddHours(10),
                EndDateTime = DateTime.Today.AddHours(11),
                RecurrenceRuleId = recurrenceRule.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _context.RecurrenceRules.AddAsync(recurrenceRule);
            await _context.Appointments.AddAsync(recurringAppointment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetPotentialConflictingAppointmentsAsync(
                checkStart, checkEnd, _userId1, null);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPotentialConflictingAppointmentsAsync_ShouldIncludeAppointmentsWhereUserIsAttendee()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(2);

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId2, // Different organizer
                Title = "As Attendee",
                StartDateTime = baseTime,
                EndDateTime = baseTime.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            var attendee = new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                UserId = _userId1, // User is attendee
                IsOrganizer = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.AppointmentAttendees.AddAsync(attendee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetPotentialConflictingAppointmentsAsync(
                baseTime, baseTime.AddHours(1), _userId1, null);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("As Attendee");
        }

        [Fact]
        public async Task GetPotentialConflictingAppointmentsAsync_ShouldExcludeSpecifiedAppointment()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(2);
            var excludeId = Guid.NewGuid();

            var appointment1 = new Appointment
            {
                Id = excludeId,
                OrganizerId = _userId1,
                Title = "Excluded",
                StartDateTime = baseTime,
                EndDateTime = baseTime.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            var appointment2 = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Included",
                StartDateTime = baseTime.AddMinutes(30),
                EndDateTime = baseTime.AddHours(1).AddMinutes(30),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(appointment1, appointment2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetPotentialConflictingAppointmentsAsync(
                baseTime, baseTime.AddHours(2), _userId1, excludeId);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(a => a.Title == "Included");
            result.Should().NotContain(a => a.Id == excludeId);
        }

        [Fact]
        public async Task GetPotentialConflictingAppointmentsAsync_ShouldExcludeDeletedAppointments()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(2);

            var activeAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Active",
                StartDateTime = baseTime,
                EndDateTime = baseTime.AddHours(1),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var deletedAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = _userId1,
                Title = "Deleted",
                StartDateTime = baseTime.AddMinutes(30),
                EndDateTime = baseTime.AddHours(1).AddMinutes(30),
                IsDeleted = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddRangeAsync(activeAppointment, deletedAppointment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _appointmentDAL.GetPotentialConflictingAppointmentsAsync(
                baseTime, baseTime.AddHours(2), _userId1, null);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(a => a.Title == "Active");
            result.Should().NotContain(a => a.IsDeleted);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
