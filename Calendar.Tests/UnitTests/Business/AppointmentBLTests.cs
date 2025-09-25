using Xunit;
using Moq;
using FluentAssertions;
using Calendar.Business;
using Calendar.DataAccess;
using Calendar.DTOs;
using Calendar.Models;
using Calendar.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Calendar.Tests.Business
{
    public class AppointmentBLTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Mock<IAppointmentDAL> _mockAppointmentDAL;
        private readonly Mock<IUserDAL> _mockUserDAL;
        private readonly Mock<IAppointmentTypeDAL> _mockAppointmentTypeDAL;
        private readonly Mock<IRecurrenceRuleDAL> _mockRecurrenceRuleDAL;
        private readonly Mock<IAppointmentAttendeeDAL> _mockAppointmentAttendeeDAL;
        private readonly Mock<ILogger<AppointmentBL>> _mockLogger;
        private readonly AppointmentBL _appointmentBL;

        public AppointmentBLTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _mockAppointmentDAL = new Mock<IAppointmentDAL>();
            _mockUserDAL = new Mock<IUserDAL>();
            _mockAppointmentTypeDAL = new Mock<IAppointmentTypeDAL>();
            _mockRecurrenceRuleDAL = new Mock<IRecurrenceRuleDAL>();
            _mockAppointmentAttendeeDAL = new Mock<IAppointmentAttendeeDAL>();
            _mockLogger = new Mock<ILogger<AppointmentBL>>();

            _appointmentBL = new AppointmentBL(
                _mockAppointmentDAL.Object,
                _mockAppointmentAttendeeDAL.Object,
                _mockUserDAL.Object,
                _mockRecurrenceRuleDAL.Object,
                _mockAppointmentTypeDAL.Object,
                _mockLogger.Object
            );
        }

        #region CreateAppointmentAsync Tests

        [Fact]
        public async Task CreateAppointmentAsync_WithValidRequest_ShouldCreateAppointment()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentTypeId = Guid.NewGuid();
            var attendeeId1 = Guid.NewGuid();
            var attendeeId2 = Guid.NewGuid();

            var request = new CreateAppointmentRequestDto
            {
                Title = "  Team Meeting  ", // Test trimming
                Description = "  Weekly sync  ", // Test trimming
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Location = "  Room A  ", // Test trimming
                AppointmentTypeId = appointmentTypeId,
                AttendeeIds = new List<Guid> { attendeeId1, attendeeId2 }
            };

            var organizer = new User
            {
                Id = userId,
                Email = "organizer@example.com",
                FirstName = "John",
                LastName = "Doe",
                IsActive = true
            };

            var appointmentType = new AppointmentType
            {
                Id = appointmentTypeId,
                Name = "Meeting",
                Color = "#FF0000"
            };

            var attendees = new List<User>
            {
                new User { Id = attendeeId1, Email = "attendee1@example.com", FirstName = "Jane", LastName = "Smith" },
                new User { Id = attendeeId2, Email = "attendee2@example.com", FirstName = "Bob", LastName = "Johnson" }
            };

            var createdAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = userId,
                Title = "Team Meeting",
                Description = "Weekly sync",
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Location = "Room A",
                AppointmentTypeId = appointmentTypeId,
                AppointmentType = appointmentType,
                Organizer = organizer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(organizer);
            _mockAppointmentTypeDAL.Setup(x => x.GetByIdAsync(appointmentTypeId)).ReturnsAsync(appointmentType);
            _mockUserDAL.Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(attendees);
            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), userId, null))
                .ReturnsAsync(new List<Appointment>());
            _mockAppointmentDAL.Setup(x => x.CreateAsync(It.IsAny<Appointment>()))
                .ReturnsAsync(createdAppointment);

            // Act
            var result = await _appointmentBL.CreateAppointmentAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdAppointment.Id);
            result.Title.Should().Be("Team Meeting");
            result.Description.Should().Be("Weekly sync");
            result.Location.Should().Be("Room A");
            result.OrganizerId.Should().Be(userId);
            result.AppointmentType.Should().NotBeNull();
            result.AppointmentType.Name.Should().Be("Meeting");

            // Verify method calls
            _mockAppointmentDAL.Verify(x => x.CreateAsync(It.Is<Appointment>(a =>
                a.Title == "Team Meeting" &&
                a.Description == "Weekly sync" &&
                a.Location == "Room A" &&
                a.OrganizerId == userId
            )), Times.Once);

            _mockAppointmentAttendeeDAL.Verify(x => x.CreateAttendeesAsync(It.Is<List<AppointmentAttendee>>(list =>
                list.Count == 3 && // organizer + 2 attendees
                list.Any(a => a.UserId == userId && a.IsOrganizer) &&
                list.Any(a => a.UserId == attendeeId1 && !a.IsOrganizer) &&
                list.Any(a => a.UserId == attendeeId2 && !a.IsOrganizer)
            )), Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Appointment created successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithInactiveUser_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    _appointmentBL.CreateAppointmentAsync(request, userId));

            exception.Message.Should().Be("User not found or inactive.");

            // Verify no appointment was created
            _mockAppointmentDAL.Verify(x => x.CreateAsync(It.IsAny<Appointment>()), Times.Never);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithInvalidAppointmentType_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var invalidTypeId = Guid.NewGuid();

            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                AppointmentTypeId = invalidTypeId
            };

            var organizer = new User { Id = userId, IsActive = true };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(organizer);
            _mockAppointmentTypeDAL.Setup(x => x.GetByIdAsync(invalidTypeId)).ReturnsAsync((AppointmentType)null);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    _appointmentBL.CreateAppointmentAsync(request, userId));

            exception.Message.Should().Be("Invalid appointment type.");
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithInvalidAttendees_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var validAttendeeId = Guid.NewGuid();
            var invalidAttendeeId = Guid.NewGuid();

            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                AttendeeIds = new List<Guid> { validAttendeeId, invalidAttendeeId }
            };

            var organizer = new User { Id = userId, IsActive = true };
            var attendees = new List<User>
            {
                new User { Id = validAttendeeId, Email = "valid@example.com" }
                // invalidAttendeeId is missing
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(organizer);
            _mockUserDAL.Setup(x => x.GetByIdsAsync(request.AttendeeIds)).ReturnsAsync(attendees);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    _appointmentBL.CreateAppointmentAsync(request, userId));

            exception.Message.Should().Contain($"Invalid attendee IDs: {invalidAttendeeId}");
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithConflictingAppointments_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateAppointmentRequestDto
            {
                Title = "New Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            var organizer = new User { Id = userId, IsActive = true };
            var conflictingAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = "Existing Meeting",
                StartDateTime = request.StartDateTime.AddMinutes(-30),
                EndDateTime = request.EndDateTime.AddMinutes(-30),
                OrganizerId = userId
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(organizer);
            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    request.StartDateTime, request.EndDateTime, userId, null))
                .ReturnsAsync(new List<Appointment> { conflictingAppointment });

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _appointmentBL.CreateAppointmentAsync(request, userId));

            exception.Message.Should().Contain("Appointment conflicts with existing appointments: Existing Meeting");
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithWeekRecurrence_ShouldCreateRecurringAppointment()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateAppointmentRequestDto
            {
                Title = "Weekly Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Recurrence = new RecurrenceRequestDto
                {
                    Frequency = "WEEKLY",
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday },
                    EndDate = DateTime.UtcNow.AddMonths(3)
                }
            };

            var organizer = new User { Id = userId, IsActive = true };
            var createdAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                OrganizerId = userId,
                Organizer = organizer,
                RecurrenceRule = new RecurrenceRule
                {
                    Id = Guid.NewGuid(),
                    Frequency = "WEEKLY",
                    DaysOfWeekMask = 1 + 2 + 4 + 8 + 16 + 32 + 64,
                    EndDate = request.Recurrence.EndDate
                }
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(organizer);
            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), userId, null))
                .ReturnsAsync(new List<Appointment>());
            _mockAppointmentDAL.Setup(x => x.CreateAsync(It.IsAny<Appointment>()))
                .ReturnsAsync(createdAppointment);

            // Act
            var result = await _appointmentBL.CreateAppointmentAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsRecurring.Should().BeFalse();
            result.Recurrence.Should().NotBeNull();
            result.Recurrence.Frequency.Should().Be("WEEKLY");
            result.Recurrence.DaysOfWeek.Should().Contain(DayOfWeek.Sunday);
            result.Recurrence.DaysOfWeek.Should().Contain(DayOfWeek.Monday);
            result.Recurrence.DaysOfWeek.Should().Contain(DayOfWeek.Tuesday);
            result.Recurrence.DaysOfWeek.Should().Contain(DayOfWeek.Wednesday);
            result.Recurrence.DaysOfWeek.Should().Contain(DayOfWeek.Thursday);
            result.Recurrence.DaysOfWeek.Should().Contain(DayOfWeek.Friday);
            result.Recurrence.DaysOfWeek.Should().Contain(DayOfWeek.Saturday);
            result.Recurrence.EndDate.Should().Be(request.Recurrence.EndDate);

            // Verify appointment was created with recurrence rule
            _mockAppointmentDAL.Verify(x => x.CreateAsync(It.Is<Appointment>(a =>
                a.RecurrenceRule != null &&
                a.RecurrenceRule.Frequency == "WEEKLY"
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithMonthRecurrence_ShouldCreateRecurringAppointment()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateAppointmentRequestDto
            {
                Title = "Weekly Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Recurrence = new RecurrenceRequestDto
                {
                    Frequency = "WEEKLY",
                    DaysOfMonth = [1, 15, 31],
                    EndDate = DateTime.UtcNow.AddMonths(3)
                }
            };

            var organizer = new User { Id = userId, IsActive = true };
            var createdAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                OrganizerId = userId,
                Organizer = organizer,
                RecurrenceRule = new RecurrenceRule
                {
                    Id = Guid.NewGuid(),
                    Frequency = "MONTHLY",
                    EndDate = request.Recurrence.EndDate
                }
            };
            createdAppointment.RecurrenceRule.SetDaysOfMonth(request.Recurrence.DaysOfMonth);
            // _testOutputHelper.WriteLine(createdAppointment.RecurrenceRule.GetDaysOfMonth());

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(organizer);
            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), userId, null))
                .ReturnsAsync(new List<Appointment>());
            _mockAppointmentDAL.Setup(x => x.CreateAsync(It.IsAny<Appointment>()))
                .ReturnsAsync(createdAppointment);

            // Act
            var result = await _appointmentBL.CreateAppointmentAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsRecurring.Should().BeFalse();
            result.Recurrence.Should().NotBeNull();
            result.Recurrence.Frequency.Should().Be("MONTHLY");
            result.Recurrence.DaysOfMonth.Should().Contain(1);
            result.Recurrence.DaysOfMonth.Should().Contain(15);
            result.Recurrence.DaysOfMonth.Should().Contain(31);
            result.Recurrence.EndDate.Should().Be(request.Recurrence.EndDate);


            // Verify appointment was created with recurrence rule
            _mockAppointmentDAL.Verify(x => x.CreateAsync(It.Is<Appointment>(a =>
                a.RecurrenceRule != null &&
                a.RecurrenceRule.Frequency == "WEEKLY"
            )), Times.Once);
        }
        
        [Fact]
        public async Task CreateAppointmentAsync_WithInvalidRecurrence_ShouldCreateThrowError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateAppointmentRequestDto
            {
                Title = "Weekly Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Recurrence = new RecurrenceRequestDto
                {
                    Frequency = "WEEKLY",
                    DaysOfMonth = [1, 15, 31],
                    EndDate = DateTime.UtcNow.AddMonths(3)
                }
            };

            var organizer = new User { Id = userId, IsActive = true };
            var createdAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                OrganizerId = userId,
                Organizer = organizer,
                RecurrenceRule = new RecurrenceRule
                {
                    Id = Guid.NewGuid(),
                    Frequency = "kjhgv",
                    EndDate = request.Recurrence.EndDate
                }
            };
            createdAppointment.RecurrenceRule.SetDaysOfMonth(request.Recurrence.DaysOfMonth);
            // _testOutputHelper.WriteLine(createdAppointment.RecurrenceRule.GetDaysOfMonth());

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(organizer);
            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), userId, null))
                .ReturnsAsync(new List<Appointment>());
            _mockAppointmentDAL.Setup(x => x.CreateAsync(It.IsAny<Appointment>()))
                .ReturnsAsync(createdAppointment);

            // Act
            var result = await _appointmentBL.CreateAppointmentAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsRecurring.Should().BeFalse();
            result.Recurrence.Should().NotBeNull();
            result.Recurrence.Frequency.Should().Be("kjhgv");
            result.Recurrence.EndDate.Should().Be(request.Recurrence.EndDate);


            // Verify appointment was created with recurrence rule
            _mockAppointmentDAL.Verify(x => x.CreateAsync(It.Is<Appointment>(a =>
                a.RecurrenceRule != null &&
                a.RecurrenceRule.Frequency == "WEEKLY"
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithException_ShouldLogAndRethrow()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
            };

            var exception = new Exception("Database error");
            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _appointmentBL.CreateAppointmentAsync(request, userId));

            // Verify error logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error creating appointment")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region UpdateAppointmentAsync Tests

        [Fact]
        public async Task UpdateAppointmentAsync_WithValidRequest_ShouldUpdateAppointment()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            var appointmentTypeId = Guid.NewGuid();

            var request = new UpdateAppointmentRequestDto
            {
                Title = "  Updated Meeting  ", // Test trimming
                Description = "  Updated description  ",
                StartDateTime = DateTime.UtcNow.AddHours(2),
                EndDateTime = DateTime.UtcNow.AddHours(3),
                Location = "  Room B  ",
                AppointmentTypeId = appointmentTypeId,
                AttendeeIds = new List<Guid> { Guid.NewGuid() }
            };

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = userId,
                Title = "Original Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var appointmentType = new AppointmentType
            {
                Id = appointmentTypeId,
                Name = "Updated Type",
                Color = "#00FF00"
            };

            var updatedAppointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = userId,
                Title = "Updated Meeting",
                Description = "Updated description",
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Location = "Room B",
                AppointmentType = appointmentType,
                UpdatedAt = DateTime.UtcNow
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId)).ReturnsAsync(existingAppointment);
            _mockAppointmentAttendeeDAL.Setup(x => x.IsOrganizerAsync(appointmentId, userId)).ReturnsAsync(true);
            _mockAppointmentTypeDAL.Setup(x => x.GetByIdAsync(appointmentTypeId)).ReturnsAsync(appointmentType);
            _mockUserDAL.Setup(x => x.GetByIdsAsync(request.AttendeeIds)).ReturnsAsync(new List<User>
            {
                new User { Id = request.AttendeeIds.First() }
            });
            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), userId, appointmentId))
                .ReturnsAsync(new List<Appointment>());
            _mockAppointmentDAL.Setup(x => x.UpdateAsync(It.IsAny<Appointment>())).ReturnsAsync(updatedAppointment);
            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ReturnsAsync(updatedAppointment); // Second call for complete appointment

            // Act
            var result = await _appointmentBL.UpdateAppointmentAsync(appointmentId, request, userId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(appointmentId);
            result.Title.Should().Be("Updated Meeting");
            result.Description.Should().Be("Updated description");
            result.Location.Should().Be("Room B");

            // Verify update was called with trimmed values
            _mockAppointmentDAL.Verify(x => x.UpdateAsync(It.Is<Appointment>(a =>
                a.Title == "Updated Meeting" &&
                a.Description == "Updated description" &&
                a.Location == "Room B" &&
                a.UpdatedAt > existingAppointment.CreatedAt
            )), Times.Once);

            // Verify attendees were updated
            _mockAppointmentAttendeeDAL.Verify(x => x.UpdateAttendeesAsync(
                appointmentId,
                It.Is<List<Guid>>(list => list.Contains(userId)), // Organizer should be included
                userId
            ), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_WithNonExistentAppointment_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            var request = new UpdateAppointmentRequestDto
            {
                Title = "Updated Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId)).ReturnsAsync((Appointment)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _appointmentBL.UpdateAppointmentAsync(appointmentId, request, userId));

            exception.Message.Should().Be("Appointment not found.");
        }

        [Fact]
        public async Task UpdateAppointmentAsync_WithNonOrganizer_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            var request = new UpdateAppointmentRequestDto
            {
                Title = "Updated Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = Guid.NewGuid(), // Different user
                Title = "Original Meeting"
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId)).ReturnsAsync(existingAppointment);
            _mockAppointmentAttendeeDAL.Setup(x => x.IsOrganizerAsync(appointmentId, userId)).ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _appointmentBL.UpdateAppointmentAsync(appointmentId, request, userId));

            exception.Message.Should().Be("Only the organizer can update the appointment.");
        }

        [Fact]
        public async Task UpdateAppointmentAsync_WithRecurrenceUpdate_ShouldUpdateRecurrence()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            var existingRuleId = Guid.NewGuid();

            var request = new UpdateAppointmentRequestDto
            {
                Title = "Updated Recurring Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Recurrence = new RecurrenceRequestDto
                {
                    Frequency = "MONTHLY",
                    DaysOfMonth = new List<int> { 1, 15 },
                    EndDate = DateTime.UtcNow.AddMonths(6)
                }
            };

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = userId,
                Title = "Original Meeting",
                RecurrenceRuleId = existingRuleId,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddHours(1)
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ReturnsAsync(existingAppointment);
            _mockAppointmentAttendeeDAL.Setup(x => x.IsOrganizerAsync(appointmentId, userId))
                .ReturnsAsync(true);
            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), userId, appointmentId))
                .ReturnsAsync(new List<Appointment>());
            _mockRecurrenceRuleDAL.Setup(x => x.UpdateAsync(existingRuleId, It.IsAny<RecurrenceRule>()))
                .Returns(Task.FromResult((RecurrenceRule?)null));
            _mockAppointmentDAL.Setup(x => x.UpdateAsync(It.IsAny<Appointment>()))
                .ReturnsAsync(existingAppointment);

            // Act
            await _appointmentBL.UpdateAppointmentAsync(appointmentId, request, userId);

            // Assert
            _mockRecurrenceRuleDAL.Verify(x => x.UpdateAsync(
                existingRuleId,
                It.Is<RecurrenceRule>(r => r.Frequency == "MONTHLY")
            ), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_RemoveRecurrence_ShouldDeleteRecurrenceRule()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            var existingRuleId = Guid.NewGuid();

            var request = new UpdateAppointmentRequestDto
            {
                Title = "Non-recurring Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Recurrence = null // Remove recurrence
            };

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = userId,
                Title = "Recurring Meeting",
                RecurrenceRuleId = existingRuleId,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddHours(1)
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ReturnsAsync(existingAppointment);
            _mockAppointmentAttendeeDAL.Setup(x => x.IsOrganizerAsync(appointmentId, userId))
                .ReturnsAsync(true);
            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), userId, appointmentId))
                .ReturnsAsync(new List<Appointment>());
            _mockRecurrenceRuleDAL.Setup(x => x.DeleteAsync(existingRuleId))
                .Returns(Task.FromResult(true));
            _mockAppointmentDAL.Setup(x => x.UpdateAsync(It.IsAny<Appointment>()))
                .ReturnsAsync(existingAppointment);

            // Act
            await _appointmentBL.UpdateAppointmentAsync(appointmentId, request, userId);

            // Assert
            _mockRecurrenceRuleDAL.Verify(x => x.DeleteAsync(existingRuleId
            ), Times.Once);

            _mockAppointmentDAL.Verify(x => x.UpdateAsync(It.Is<Appointment>(a =>
                a.RecurrenceRuleId == null
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_AddRecurrenceToNonRecurring_ShouldCreateRecurrenceRule()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();

            var request = new UpdateAppointmentRequestDto
            {
                Title = "Now Recurring Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Recurrence = new RecurrenceRequestDto
                {
                    Frequency = "DAILY",
                    EndDate = DateTime.UtcNow.AddMonths(1)
                }
            };

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = userId,
                Title = "Non-recurring Meeting",
                RecurrenceRuleId = null, // No recurrence
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddHours(1)
            };

            var newRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                EndDate = request.Recurrence.EndDate
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ReturnsAsync(existingAppointment);
            _mockAppointmentAttendeeDAL.Setup(x => x.IsOrganizerAsync(appointmentId, userId))
                .ReturnsAsync(true);
            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), userId, appointmentId))
                .ReturnsAsync(new List<Appointment>());
            _mockRecurrenceRuleDAL.Setup(x => x.CreateAsync(It.IsAny<RecurrenceRule>()))
                .ReturnsAsync(newRule);
            _mockAppointmentDAL.Setup(x => x.UpdateAsync(It.IsAny<Appointment>()))
                .ReturnsAsync(existingAppointment);

            // Act
            await _appointmentBL.UpdateAppointmentAsync(appointmentId, request, userId);

            // Assert
            _mockRecurrenceRuleDAL.Verify(x => x.CreateAsync(
                It.Is<RecurrenceRule>(r => r.Frequency == "DAILY")
            ), Times.Once);

            _mockAppointmentDAL.Verify(x => x.UpdateAsync(It.Is<Appointment>(a =>
                a.RecurrenceRule != null
            )), Times.Once);
        }

        #endregion

        #region GetAppointmentsAsync Tests

        [Fact]
        public async Task GetAppointmentsAsync_WithValidRequest_ShouldReturnAppointments()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);
            var appointmentTypeId = Guid.NewGuid();

            var user = new User { Id = userId, IsActive = true };
            var appointmentType = new AppointmentType { Id = appointmentTypeId, Name = "Meeting" };

            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    Title = "Regular Meeting",
                    StartDateTime = fromDate.AddHours(10),
                    EndDateTime = fromDate.AddHours(11),
                    OrganizerId = userId,
                    Organizer = user,
                    AppointmentType = appointmentType
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    Title = "Recurring Meeting",
                    StartDateTime = fromDate.AddHours(14),
                    EndDateTime = fromDate.AddHours(15),
                    OrganizerId = userId,
                    Organizer = user,
                    RecurrenceRuleId = Guid.NewGuid(),
                    RecurrenceRule = new RecurrenceRule
                    {
                        Frequency = "DAILY",
                        EndDate = toDate
                    }
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    Title = "Recurring Meeting",
                    StartDateTime = fromDate.AddHours(17),
                    EndDateTime = fromDate.AddHours(18),
                    OrganizerId = userId,
                    Organizer = user,
                    RecurrenceRuleId = Guid.NewGuid(),
                    RecurrenceRule = new RecurrenceRule
                    {
                        Frequency = "WEEKLY",
                        EndDate = toDate,
                        DaysOfMonthMask = 7
                    }
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    Title = "Recurring Meeting",
                    StartDateTime = fromDate.AddHours(19),
                    EndDateTime = fromDate.AddHours(20),
                    OrganizerId = userId,
                    Organizer = user,
                    RecurrenceRuleId = Guid.NewGuid(),
                    RecurrenceRule = new RecurrenceRule
                    {
                        Frequency = "MONTHLY",
                        EndDate = toDate,
                        DaysOfMonthMask = 7
                    }
                }
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentTypeDAL.Setup(x => x.GetByIdAsync(appointmentTypeId)).ReturnsAsync(appointmentType);
            _mockAppointmentDAL.Setup(x => x.GetAppointmentsByDateRangeAsync(
                    userId, fromDate, toDate, appointmentTypeId, true))
                .ReturnsAsync(appointments);

            // Act
            var result = await _appointmentBL.GetAppointmentsAsync(userId, fromDate, toDate, appointmentTypeId, true);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().Contain(a => a.Title == "Regular Meeting" && !a.IsRecurring);
            result.Where(a => a.Title == "Recurring Meeting").Should().NotBeEmpty(); // Should have instances

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) =>
                        o.ToString().Contains($"Retrieved") && o.ToString().Contains("appointments")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAppointmentsAsync_WithInvalidUser_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _appointmentBL.GetAppointmentsAsync(userId, fromDate, toDate, null, true));

            exception.Message.Should().Be("User not found or inactive.");
        }

        [Fact]
        public async Task GetAppointmentsAsync_WithInvalidAppointmentType_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var invalidTypeId = Guid.NewGuid();
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);

            var user = new User { Id = userId, IsActive = true };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentTypeDAL.Setup(x => x.GetByIdAsync(invalidTypeId)).ReturnsAsync((AppointmentType)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _appointmentBL.GetAppointmentsAsync(userId, fromDate, toDate, invalidTypeId, true));

            exception.Message.Should().Be("Invalid appointment type.");
        }

        [Fact]
        public async Task GetAppointmentsAsync_WithRecurringAppointments_ShouldExpandInstances()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);

            var user = new User { Id = userId, IsActive = true };
            var recurringAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = "Daily Meeting",
                StartDateTime = fromDate.AddHours(10),
                EndDateTime = fromDate.AddHours(11),
                OrganizerId = userId,
                Organizer = user,
                RecurrenceRuleId = Guid.NewGuid(),
                RecurrenceRule = new RecurrenceRule
                {
                    Frequency = "DAILY",
                    EndDate = toDate
                }
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentDAL.Setup(x => x.GetAppointmentsByDateRangeAsync(
                    userId, fromDate, toDate, null, true))
                .ReturnsAsync(new List<Appointment> { recurringAppointment });

            // Act
            var result = await _appointmentBL.GetAppointmentsAsync(userId, fromDate, toDate, null, true);

            // Assert
            result.Should().NotBeNull();
            // Should have multiple instances for daily recurrence
            var instances = result.Where(a => a.Title == "Daily Meeting" && a.IsRecurringInstance).ToList();
            instances.Should().NotBeEmpty();
            instances.All(i => i.ParentAppointmentId == recurringAppointment.Id).Should().BeTrue();
        }

        [Fact]
        public async Task GetAppointmentsAsync_ExcludeRecurring_ShouldNotExpandInstances()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);

            var user = new User { Id = userId, IsActive = true };
            var recurringAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = "Recurring Meeting",
                StartDateTime = fromDate.AddHours(10),
                EndDateTime = fromDate.AddHours(11),
                OrganizerId = userId,
                Organizer = user,
                RecurrenceRuleId = Guid.NewGuid(),
                RecurrenceRule = new RecurrenceRule { Frequency = "DAILY", EndDate = toDate }
            };

            var regularAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = "Regular Meeting",
                StartDateTime = fromDate.AddHours(14),
                EndDateTime = fromDate.AddHours(15),
                OrganizerId = userId,
                Organizer = user
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentDAL.Setup(x => x.GetAppointmentsByDateRangeAsync(
                    userId, fromDate, toDate, null, false))
                .ReturnsAsync(new List<Appointment> { recurringAppointment, regularAppointment });

            // Act
            var result = await _appointmentBL.GetAppointmentsAsync(userId, fromDate, toDate, null, false);

            // Assert
            result.Should().HaveCount(1); // Only non-recurring appointment
            result.First().Title.Should().Be("Regular Meeting");
            result.Should().NotContain(a => a.IsRecurring);
        }

        #endregion

        #region GetConflictingAppointmentsWithRecurrenceAsync Tests

        [Fact]
        public async Task GetConflictingAppointmentsWithRecurrenceAsync_WithNoConflicts_ShouldReturnEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var startDateTime = DateTime.UtcNow.AddHours(1);
            var endDateTime = DateTime.UtcNow.AddHours(2);

            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    startDateTime, endDateTime, userId, null))
                .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _appointmentBL.GetConflictingAppointmentsWithRecurrenceAsync(
                startDateTime, endDateTime, userId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetConflictingAppointmentsWithRecurrenceAsync_WithDirectConflict_ShouldReturnConflicting()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var startDateTime = DateTime.UtcNow.AddHours(1);
            var endDateTime = DateTime.UtcNow.AddHours(2);

            var conflictingAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = "Existing Meeting",
                StartDateTime = startDateTime.AddMinutes(30), // Overlaps
                EndDateTime = endDateTime.AddMinutes(30),
                OrganizerId = userId
            };

            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    startDateTime, endDateTime, userId, null))
                .ReturnsAsync(new List<Appointment> { conflictingAppointment });

            // Act
            var result = await _appointmentBL.GetConflictingAppointmentsWithRecurrenceAsync(
                startDateTime, endDateTime, userId);

            // Assert
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(conflictingAppointment.Id);
        }

        [Fact]
        public async Task GetConflictingAppointmentsWithRecurrenceAsync_WithRecurringConflict_ShouldDetectConflict()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var startDateTime = DateTime.Today.AddHours(10);
            var endDateTime = DateTime.Today.AddHours(11);

            var recurringAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = "Daily Meeting",
                StartDateTime = DateTime.Today.AddDays(-5).AddHours(10), // Started 5 days ago
                EndDateTime = DateTime.Today.AddDays(-5).AddHours(11),
                OrganizerId = userId,
                RecurrenceRuleId = Guid.NewGuid(),
                RecurrenceRule = new RecurrenceRule
                {
                    Frequency = "DAILY",
                    EndDate = DateTime.Today.AddDays(30) // Still active
                }
            };

            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    startDateTime, endDateTime, userId, null))
                .ReturnsAsync(new List<Appointment> { recurringAppointment });

            // Act
            var result = await _appointmentBL.GetConflictingAppointmentsWithRecurrenceAsync(
                startDateTime, endDateTime, userId);

            // Assert
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(recurringAppointment.Id);
        }

        [Fact]
        public async Task GetConflictingAppointmentsWithRecurrenceAsync_WithExcludedAppointment_ShouldNotIncludeIt()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var excludeId = Guid.NewGuid();
            var startDateTime = DateTime.UtcNow.AddHours(1);
            var endDateTime = DateTime.UtcNow.AddHours(2);

            var appointment1 = new Appointment
            {
                Id = excludeId,
                Title = "Excluded Meeting",
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                OrganizerId = userId
            };

            var appointment2 = new Appointment
            {
                Id = Guid.NewGuid(),
                Title = "Conflicting Meeting",
                StartDateTime = startDateTime.AddMinutes(30),
                EndDateTime = endDateTime.AddMinutes(30),
                OrganizerId = userId
            };

            _mockAppointmentDAL.Setup(x => x.GetPotentialConflictingAppointmentsAsync(
                    startDateTime, endDateTime, userId, excludeId))
                .ReturnsAsync(new List<Appointment> { appointment2 }); // Only appointment2

            // Act
            var result = await _appointmentBL.GetConflictingAppointmentsWithRecurrenceAsync(
                startDateTime, endDateTime, userId, excludeId);

            // Assert
            result.Should().HaveCount(1);
            result.Should().NotContain(a => a.Id == excludeId);
            result.First().Id.Should().Be(appointment2.Id);
        }

        #endregion

        #region DeleteAppointmentAsync Tests

        // [Fact]
        // public async Task DeleteAppointmentAsync_WithValidRequest_ShouldDeleteAppointment()
        // {
        //     // Arrange
        //     var userId = Guid.NewGuid();
        //     var appointmentId = Guid.NewGuid();
        //
        //     var appointment = new Appointment
        //     {
        //         Id = appointmentId,
        //         OrganizerId = userId,
        //         Title = "Meeting to Delete"
        //     };
        //
        //     _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);
        //     _mockAppointmentAttendeeDAL.Setup(x => x.IsOrganizerAsync(appointmentId, userId)).ReturnsAsync(true);
        //     _mockAppointmentDAL.Setup(x => x.DeleteAsync(appointmentId)).ReturnsAsync(true);
        //     _mockAppointmentAttendeeDAL.Setup(x => x.DeleteAllAttendeesAsync(appointmentId)).Returns(Task.CompletedTask);
        //
        //     // Act
        //     var result = await _appointmentBL.DeleteAppointmentAsync(appointmentId, userId);
        //
        //     // Assert
        //     result.Should().BeTrue();
        //
        //     // Verify deletion
        //     _mockAppointmentDAL.Verify(x => x.DeleteAsync(appointmentId), Times.Once);
        //     _mockAppointmentAttendeeDAL.Verify(x => x.DeleteAllAttendeesAsync(appointmentId), Times.Once);
        //
        //     // Verify logging
        //     _mockLogger.Verify(
        //         x => x.Log(
        //             LogLevel.Information,
        //             It.IsAny<EventId>(),
        //             It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Appointment deleted successfully")),
        //             It.IsAny<Exception>(),
        //             It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        //         Times.Once);
        // }

        [Fact]
        public async Task DeleteAppointmentAsync_WithNonExistentAppointment_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId)).ReturnsAsync((Appointment)null);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    _appointmentBL.DeleteAppointmentAsync(appointmentId, userId));

            exception.Message.Should().Be("Appointment not found.");

            // Verify no deletion occurred
            _mockAppointmentDAL.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_WithNonOrganizer_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();

            var appointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = Guid.NewGuid(), // Different user
                Title = "Someone else's meeting"
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);
            _mockAppointmentAttendeeDAL.Setup(x => x.IsOrganizerAsync(appointmentId, userId)).ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _appointmentBL.DeleteAppointmentAsync(appointmentId, userId));

            exception.Message.Should().Be("Only the organizer can delete the appointment.");

            // Verify no deletion occurred
            _mockAppointmentDAL.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_WhenDeleteFails_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();

            var appointment = new Appointment
            {
                Id = appointmentId,
                OrganizerId = userId,
                Title = "Meeting"
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);
            _mockAppointmentAttendeeDAL.Setup(x => x.IsOrganizerAsync(appointmentId, userId)).ReturnsAsync(true);
            _mockAppointmentDAL.Setup(x => x.DeleteAsync(appointmentId)).ReturnsAsync(false);

            // Act
            var result = await _appointmentBL.DeleteAppointmentAsync(appointmentId, userId);

            // Assert
            result.Should().BeFalse();

            // Verify attendees were not deleted when appointment deletion failed
            _mockAppointmentAttendeeDAL.Verify(x => x.DeleteAllAttendeesAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region GetAppointmentTypesAsync Tests

        [Fact]
        public async Task GetAppointmentTypesAsync_ShouldReturnAllAppointmentTypes()
        {
            // Arrange
            var appointmentTypes = new List<AppointmentType>
            {
                new AppointmentType { Id = Guid.NewGuid(), Name = "Meeting", Color = "#FF0000" },
                new AppointmentType { Id = Guid.NewGuid(), Name = "Interview", Color = "#00FF00" },
                new AppointmentType { Id = Guid.NewGuid(), Name = "Personal", Color = "#0000FF" }
            };

            _mockAppointmentTypeDAL.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentTypes);

            // Act
            var result = await _appointmentBL.GetAppointmentTypesAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(appointmentTypes);
        }

        [Fact]
        public async Task GetAppointmentTypesAsync_WithEmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            _mockAppointmentTypeDAL.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<AppointmentType>());

            // Act
            var result = await _appointmentBL.GetAppointmentTypesAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}