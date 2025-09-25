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

namespace Calendar.Tests.Business
{
    public class AppointmentAttendeeBLEdgeCaseTests
    {
        private readonly Mock<ILogger<AppointmentAttendeeBL>> _mockLogger;
        private readonly Mock<IUserDAL> _mockUserDAL;
        private readonly Mock<IAppointmentDAL> _mockAppointmentDAL;
        private readonly Mock<IAppointmentBL> _mockAppointmentBL;
        private readonly Mock<IAppointmentAttendeeDAL> _mockAppointmentAttendeeDAL;
        private readonly AppointmentAttendeeBL _appointmentAttendeeBL;

        public AppointmentAttendeeBLEdgeCaseTests()
        {
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

        [Fact]
        public async Task CheckAttendeeAvailabilityAsync_WithPastDateTime_ShouldStillProcess()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(-2), // Past time
                EndDateTime = DateTime.UtcNow.AddHours(-1),   // Past time
                ExcludeAppointmentId = null
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
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _appointmentAttendeeBL.CheckAttendeeAvailabilityAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsAvailable.Should().BeTrue();
            // The BL should still process past dates (validation should be in controller/DTO)
        }

        [Fact]
        public async Task CheckAttendeeAvailabilityAsync_WithVeryLongTimeRange_ShouldHandle()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddDays(365), // Very long range
                ExcludeAppointmentId = null
            };

            var attendee = new User
            {
                Id = request.AttendeeId,
                Email = "user@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true
            };

            var manyConflicts = Enumerable.Range(0, 100)
                .Select(i => new Appointment
                {
                    Id = Guid.NewGuid(),
                    Title = $"Meeting {i}",
                    StartDateTime = DateTime.UtcNow.AddDays(i),
                    EndDateTime = DateTime.UtcNow.AddDays(i).AddHours(1)
                })
                .ToList();

            _mockUserDAL.Setup(x => x.GetByIdAsync(request.AttendeeId))
                .ReturnsAsync(attendee);

            _mockAppointmentBL.Setup(x => x.GetConflictingAppointmentsWithRecurrenceAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(manyConflicts);

            // Act
            var result = await _appointmentAttendeeBL.CheckAttendeeAvailabilityAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsAvailable.Should().BeFalse();
            result.Message.Should().Contain("100 conflicting appointment(s)");
        }
        

        [Fact]
        public async Task GetAvailableAttendeesAsync_WithSpecialCharactersInNames_ShouldHandleCorrectly()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    FirstName = "Jean-Pierre",
                    LastName = "O'Connor",
                    IsActive = true
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user2@example.com",
                    FirstName = "María",
                    LastName = "García-López",
                    IsActive = true
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user3@example.com",
                    FirstName = "李",
                    LastName = "明",
                    IsActive = true
                }
            };

            _mockUserDAL.Setup(x => x.GetAllAsync(It.IsAny<Guid?>()))
                .ReturnsAsync(users);

            // Act
            var result = await _appointmentAttendeeBL.GetAvailableAttendeesAsync(null);

            // Assert
            result.Should().HaveCount(3);
            result[0].FullName.Should().Be("Jean-Pierre O'Connor");
            result[1].FullName.Should().Be("María García-López");
            result[2].FullName.Should().Be("李 明");
        }

        [Fact]
        public async Task GetAppointmentAttendeesAsync_WithNullUserProperties_ShouldHandleGracefully()
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
                        FirstName = null, // Null first name
                        LastName = "Doe",
                        IsActive = true
                    }
                },
                new AppointmentAttendee
                {
                    User = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "user2@example.com",
                        FirstName = "John",
                        LastName = null, // Null last name
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
            result.Should().HaveCount(2);
            // Should handle null names gracefully
            result[0].FirstName.Should().BeNull();
            result[0].LastName.Should().Be("Doe");
            result[1].FirstName.Should().Be("John");
            result[1].LastName.Should().BeNull();
        }
    }
}
