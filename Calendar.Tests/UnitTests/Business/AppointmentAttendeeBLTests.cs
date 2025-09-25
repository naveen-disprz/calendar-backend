using Xunit;
using Moq;
using FluentAssertions;
using Calendar.Business;
using Calendar.DataAccess;
using Calendar.DTOs;
using Calendar.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Calendar.Tests.Business
{
    public class AppointmentAttendeeBLTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Mock<ILogger<AppointmentAttendeeBL>> _mockLogger;
        private readonly Mock<IUserDAL> _mockUserDAL;
        private readonly Mock<IAppointmentDAL> _mockAppointmentDAL;
        private readonly Mock<IAppointmentBL> _mockAppointmentBL;
        private readonly Mock<IAppointmentAttendeeDAL> _mockAppointmentAttendeeDAL;
        private readonly AppointmentAttendeeBL _appointmentAttendeeBL;

        public AppointmentAttendeeBLTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _mockLogger = new Mock<ILogger<AppointmentAttendeeBL>>();
            _mockUserDAL = new Mock<IUserDAL>();
            _mockAppointmentDAL = new Mock<IAppointmentDAL>();
            _mockAppointmentBL = new Mock<IAppointmentBL>();
            _mockAppointmentAttendeeDAL = new Mock<IAppointmentAttendeeDAL>();

            _appointmentAttendeeBL = new AppointmentAttendeeBL(
                _mockUserDAL.Object,
                _mockAppointmentDAL.Object,
                _mockAppointmentBL.Object,
                _mockAppointmentAttendeeDAL.Object,
                _mockLogger.Object
            );
        }

        #region GetAvailableAttendeesAsync Tests

        [Fact]
        public async Task GetAvailableAttendeesAsync_WithExcludeUserId_ShouldReturnFilteredAttendees()
        {
            // Arrange
            var excludeUserId = Guid.NewGuid();
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    IsActive = true
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user2@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    IsActive = true
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user3@example.com",
                    FirstName = "Bob",
                    LastName = "Johnson",
                    IsActive = true
                }
            };

            _mockUserDAL.Setup(x => x.GetAllAsync(excludeUserId))
                .ReturnsAsync(users);

            // Act
            var result = await _appointmentAttendeeBL.GetAvailableAttendeesAsync(excludeUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            
            result[0].Email.Should().Be("user1@example.com");
            result[0].FirstName.Should().Be("John");
            result[0].LastName.Should().Be("Doe");
            result[0].FullName.Should().Be("John Doe");
            
            result[1].Email.Should().Be("user2@example.com");
            result[1].FullName.Should().Be("Jane Smith");
            
            result[2].Email.Should().Be("user3@example.com");
            result[2].FullName.Should().Be("Bob Johnson");

            _mockUserDAL.Verify(x => x.GetAllAsync(excludeUserId), Times.Once);
        }

        [Fact]
        public async Task GetAvailableAttendeesAsync_WithoutExcludeUserId_ShouldReturnAllAttendees()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    FirstName = "Alice",
                    LastName = "Williams",
                    IsActive = true
                }
            };

            _mockUserDAL.Setup(x => x.GetAllAsync(null))
                .ReturnsAsync(users);

            // Act
            var result = await _appointmentAttendeeBL.GetAvailableAttendeesAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Email.Should().Be("user1@example.com");
            result[0].FullName.Should().Be("Alice Williams");
            
            _mockUserDAL.Verify(x => x.GetAllAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetAvailableAttendeesAsync_WithEmptyUserList_ShouldReturnEmptyList()
        {
            // Arrange
            _mockUserDAL.Setup(x => x.GetAllAsync(It.IsAny<Guid?>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _appointmentAttendeeBL.GetAvailableAttendeesAsync(Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAvailableAttendeesAsync_WhenDALThrowsException_ShouldLogAndRethrow()
        {
            // Arrange
            var expectedException = new Exception("Database error");
            _mockUserDAL.Setup(x => x.GetAllAsync(It.IsAny<Guid?>()))
                .ThrowsAsync(expectedException);

            // Act
            Func<Task> act = async () => await _appointmentAttendeeBL.GetAvailableAttendeesAsync(Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database error");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error retrieving available attendees")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region CheckAttendeeAvailabilityAsync Tests

        [Fact]
        public async Task CheckAttendeeAvailabilityAsync_WithAvailableAttendee_ShouldReturnAvailable()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                ExcludeAppointmentId = null
            };

            var attendee = new User
            {
                Id = request.AttendeeId,
                Email = "available@example.com",
                FirstName = "Available",
                LastName = "User",
                IsActive = true
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(request.AttendeeId))
                .ReturnsAsync(attendee);

            _mockAppointmentBL.Setup(x => x.GetConflictingAppointmentsWithRecurrenceAsync(
                    request.StartDateTime,
                    request.EndDateTime,
                    request.AttendeeId,
                    request.ExcludeAppointmentId))
                .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _appointmentAttendeeBL.CheckAttendeeAvailabilityAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsAvailable.Should().BeTrue();
            result.Message.Should().Be($"{attendee.FullName} is available for the requested time slot.");
            result.Attendee.Should().NotBeNull();
            result.Attendee.Id.Should().Be(attendee.Id);
            result.Attendee.Email.Should().Be(attendee.Email);
            result.Attendee.FirstName.Should().Be(attendee.FirstName);
            result.Attendee.LastName.Should().Be(attendee.LastName);
            result.Attendee.FullName.Should().Be("Available User");
            result.RequestedStartTime.Should().Be(request.StartDateTime);
            result.RequestedEndTime.Should().Be(request.EndDateTime);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => 
                        o.ToString().Contains("Availability check for attendee") && 
                        o.ToString().Contains("True") &&
                        o.ToString().Contains("0 conflicts")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CheckAttendeeAvailabilityAsync_WithUnavailableAttendee_ShouldReturnUnavailable()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                ExcludeAppointmentId = null
            };

            var attendee = new User
            {
                Id = request.AttendeeId,
                Email = "busy@example.com",
                FirstName = "Busy",
                LastName = "Person",
                IsActive = true
            };

            var conflictingAppointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    Title = "Existing Meeting",
                    StartDateTime = request.StartDateTime,
                    EndDateTime = request.EndDateTime
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    Title = "Another Meeting",
                    StartDateTime = request.StartDateTime.AddMinutes(30),
                    EndDateTime = request.EndDateTime.AddMinutes(30)
                }
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(request.AttendeeId))
                .ReturnsAsync(attendee);

            _mockAppointmentBL.Setup(x => x.GetConflictingAppointmentsWithRecurrenceAsync(
                    request.StartDateTime,
                    request.EndDateTime,
                    request.AttendeeId,
                    request.ExcludeAppointmentId))
                .ReturnsAsync(conflictingAppointments);

            // Act
            var result = await _appointmentAttendeeBL.CheckAttendeeAvailabilityAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsAvailable.Should().BeFalse();
            result.Message.Should().Be($"{attendee.FullName} has 2 conflicting appointment(s) during the requested time.");
            result.Attendee.Id.Should().Be(attendee.Id);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => 
                        o.ToString().Contains("Availability check for attendee") && 
                        o.ToString().Contains("False") &&
                        o.ToString().Contains("2 conflicts")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CheckAttendeeAvailabilityAsync_WithExcludeAppointmentId_ShouldExcludeFromCheck()
        {
            // Arrange
            var excludeAppointmentId = Guid.NewGuid();
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                ExcludeAppointmentId = excludeAppointmentId
            };

            var attendee = new User
            {
                Id = request.AttendeeId,
                Email = "user@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(request.AttendeeId))
                .ReturnsAsync(attendee);

            _mockAppointmentBL.Setup(x => x.GetConflictingAppointmentsWithRecurrenceAsync(
                    request.StartDateTime,
                    request.EndDateTime,
                    request.AttendeeId,
                    excludeAppointmentId))
                .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _appointmentAttendeeBL.CheckAttendeeAvailabilityAsync(request);

            // Assert
            result.IsAvailable.Should().BeTrue();

            _mockAppointmentBL.Verify(x => x.GetConflictingAppointmentsWithRecurrenceAsync(
                request.StartDateTime,
                request.EndDateTime,
                request.AttendeeId,
                excludeAppointmentId), Times.Once);
        }

        [Fact]
        public async Task CheckAttendeeAvailabilityAsync_WithNonExistentAttendee_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            _mockUserDAL.Setup(x => x.GetByIdAsync(request.AttendeeId))
                .ReturnsAsync((User)null);

            // Act
            Func<Task> act = async () => await _appointmentAttendeeBL.CheckAttendeeAvailabilityAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Attendee not found or inactive.");

            _mockAppointmentBL.Verify(x => x.GetConflictingAppointmentsWithRecurrenceAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>()), Times.Never);
        }

        [Fact]
        public async Task CheckAttendeeAvailabilityAsync_WhenExceptionOccurs_ShouldLogAndRethrow()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            var expectedException = new Exception("Database error");
            _mockUserDAL.Setup(x => x.GetByIdAsync(request.AttendeeId))
                .ThrowsAsync(expectedException);

            // Act
            Func<Task> act = async () => await _appointmentAttendeeBL.CheckAttendeeAvailabilityAsync(request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database error");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => 
                        o.ToString().Contains("Error checking availability for attendee") &&
                        o.ToString().Contains(request.AttendeeId.ToString())),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region GetAppointmentAttendeesAsync Tests

        [Fact]
        public async Task GetAppointmentAttendeesAsync_WithValidAppointment_ShouldReturnAttendees()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var appointment = new Appointment
            {
                Id = appointmentId,
                Title = "Team Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            var appointmentAttendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.NewGuid(),
                    IsOrganizer = true,
                    User = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "organizer@example.com",
                        FirstName = "Organizer",
                        LastName = "User",
                        IsActive = true
                    }
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.NewGuid(),
                    IsOrganizer = false,
                    User = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "attendee1@example.com",
                        FirstName = "Attendee",
                        LastName = "One",
                        IsActive = true
                    }
                },
                new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    UserId = Guid.NewGuid(),
                    IsOrganizer = false,
                    User = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "attendee2@example.com",
                        FirstName = "Attendee",
                        LastName = "Two",
                        IsActive = true
                    }
                }
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ReturnsAsync(appointment);

            _mockAppointmentAttendeeDAL.Setup(x => x.GetAttendeesByAppointmentIdAsync(appointmentId))
                .ReturnsAsync(appointmentAttendees);

            // Act
            var result = await _appointmentAttendeeBL.GetAppointmentAttendeesAsync(appointmentId, currentUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            
            _testOutputHelper.WriteLine(result[0].Email);

            result[0].Email.Should().Be("organizer@example.com");
            result[0].FirstName.Should().Be("Organizer");
            result[0].LastName.Should().Be("User");
            result[0].FullName.Should().Be("Organizer User");

            result[1].Email.Should().Be("attendee1@example.com");
            result[1].FullName.Should().Be("Attendee One");

            result[2].Email.Should().Be("attendee2@example.com");
            result[2].FullName.Should().Be("Attendee Two");

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => 
                        o.ToString().Contains("Retrieved 3 attendees for appointment") &&
                        o.ToString().Contains(appointmentId.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAppointmentAttendeesAsync_WithNoAttendees_ShouldReturnEmptyList()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var appointment = new Appointment
            {
                Id = appointmentId,
                Title = "Solo Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ReturnsAsync(appointment);

            _mockAppointmentAttendeeDAL.Setup(x => x.GetAttendeesByAppointmentIdAsync(appointmentId))
                .ReturnsAsync(new List<AppointmentAttendee>());

            // Act
            var result = await _appointmentAttendeeBL.GetAppointmentAttendeesAsync(appointmentId, currentUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => 
                        o.ToString().Contains("Retrieved 0 attendees for appointment")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAppointmentAttendeesAsync_WithNonExistentAppointment_ShouldThrowArgumentException()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ReturnsAsync((Appointment)null);

            // Act
            Func<Task> act = async () => await _appointmentAttendeeBL.GetAppointmentAttendeesAsync(appointmentId, currentUserId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Appointment not found.");

            _mockAppointmentAttendeeDAL.Verify(x => x.GetAttendeesByAppointmentIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetAppointmentAttendeesAsync_WithAttendeesHavingFullNameProperty_ShouldUseFullName()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var appointment = new Appointment { Id = appointmentId };

            var appointmentAttendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    User = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "user@example.com",
                        FirstName = "John",
                        LastName = "Doe",
                        IsActive = true
                    }
                }
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ReturnsAsync(appointment);

            _mockAppointmentAttendeeDAL.Setup(x => x.GetAttendeesByAppointmentIdAsync(appointmentId))
                .ReturnsAsync(appointmentAttendees);

            // Act
            var result = await _appointmentAttendeeBL.GetAppointmentAttendeesAsync(appointmentId, currentUserId);

            // Assert
            result.Should().HaveCount(1);
            result[0].FullName.Should().Be("John Doe");
        }

        [Fact]
        public async Task GetAppointmentAttendeesAsync_WhenExceptionOccurs_ShouldLogAndRethrow()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var expectedException = new Exception("Database error");
            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ThrowsAsync(expectedException);

            // Act
            Func<Task> act = async () => await _appointmentAttendeeBL.GetAppointmentAttendeesAsync(appointmentId, currentUserId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database error");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => 
                        o.ToString().Contains("Error retrieving attendees for appointment") &&
                        o.ToString().Contains(appointmentId.ToString())),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAppointmentAttendeesAsync_ShouldMaintainOrderOfAttendees()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var appointment = new Appointment { Id = appointmentId };

            var appointmentAttendees = new List<AppointmentAttendee>
            {
                new AppointmentAttendee
                {
                    User = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "z@example.com",
                        FirstName = "Zack",
                        LastName = "Last"
                    }
                },
                new AppointmentAttendee
                {
                    User = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "a@example.com",
                        FirstName = "Alice",
                        LastName = "First"
                    }
                },
                new AppointmentAttendee
                {
                    User = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "m@example.com",
                        FirstName = "Mike",
                        LastName = "Middle"
                    }
                }
            };

            _mockAppointmentDAL.Setup(x => x.GetByIdAsync(appointmentId))
                .ReturnsAsync(appointment);

            _mockAppointmentAttendeeDAL.Setup(x => x.GetAttendeesByAppointmentIdAsync(appointmentId))
                .ReturnsAsync(appointmentAttendees);

            // Act
            var result = await _appointmentAttendeeBL.GetAppointmentAttendeesAsync(appointmentId, currentUserId);

            // Assert
            result.Should().HaveCount(3);
            result[0].FirstName.Should().Be("Zack");
            result[1].FirstName.Should().Be("Alice");
            result[2].FirstName.Should().Be("Mike");
        }

        #endregion
    }
}
