using Xunit;
using Moq;
using FluentAssertions;
using Calendar.Controllers;
using Calendar.Business;
using Calendar.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Calendar.Tests.Controllers
{
    public class AttendeeControllerTests
    {
        private readonly Mock<IAppointmentAttendeeBL> _mockAppointmentAttendeeBL;
        private readonly Mock<ILogger<AttendeeController>> _mockLogger;
        private readonly AttendeeController _controller;
        private readonly Guid _currentUserId;

        public AttendeeControllerTests()
        {
            _mockAppointmentAttendeeBL = new Mock<IAppointmentAttendeeBL>();
            _mockLogger = new Mock<ILogger<AttendeeController>>();
            _controller = new AttendeeController(_mockAppointmentAttendeeBL.Object, _mockLogger.Object);
            
            _currentUserId = Guid.NewGuid();
            SetupUserClaims(_currentUserId);
        }

        private void SetupUserClaims(Guid userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "testuser@example.com")
            };
            
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region GetAllAttendees Tests

        [Fact]
        public async Task GetAllAttendees_WithExcludeCurrentUserTrue_ShouldReturnAttendeesExcludingCurrentUser()
        {
            // Arrange
            var expectedAttendees = new List<AttendeeResponseDto>
            {
                new AttendeeResponseDto
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    FullName = "John Doe",
                },
                new AttendeeResponseDto
                {
                    Id = Guid.NewGuid(),
                    Email = "user2@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    FullName = "Jane Smith",
                }
            };

            _mockAppointmentAttendeeBL.Setup(x => x.GetAvailableAttendeesAsync(_currentUserId))
                .ReturnsAsync(expectedAttendees);

            // Act
            var result = await _controller.GetAllAttendeesAsync(excludeCurrentUser: true);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var attendees = okResult.Value.Should().BeOfType<List<AttendeeResponseDto>>().Subject;
            
            attendees.Should().HaveCount(2);
            attendees.Should().BeEquivalentTo(expectedAttendees);
            
            _mockAppointmentAttendeeBL.Verify(x => x.GetAvailableAttendeesAsync(_currentUserId), Times.Once);
        }

        [Fact]
        public async Task GetAllAttendees_WithExcludeCurrentUserFalse_ShouldReturnAllAttendees()
        {
            // Arrange
            var expectedAttendees = new List<AttendeeResponseDto>
            {
                new AttendeeResponseDto
                {
                    Id = _currentUserId,
                    Email = "currentuser@example.com",
                    FirstName = "Current",
                    LastName = "User",
                    FullName = "Current User",
                },
                new AttendeeResponseDto
                {
                    Id = Guid.NewGuid(),
                    Email = "other@example.com",
                    FirstName = "Other",
                    LastName = "User",
                    FullName = "Other User",
                }
            };

            _mockAppointmentAttendeeBL.Setup(x => x.GetAvailableAttendeesAsync(null))
                .ReturnsAsync(expectedAttendees);

            // Act
            var result = await _controller.GetAllAttendeesAsync(excludeCurrentUser: false);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var attendees = okResult.Value.Should().BeOfType<List<AttendeeResponseDto>>().Subject;
            
            attendees.Should().HaveCount(2);
            attendees.Should().Contain(a => a.Id == _currentUserId);
            
            _mockAppointmentAttendeeBL.Verify(x => x.GetAvailableAttendeesAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetAllAttendees_WithInvalidUserToken_ShouldReturnUnauthorized()
        {
            // Arrange
            SetupUserClaims(Guid.Empty); // Invalid GUID
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await _controller.GetAllAttendeesAsync();

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            
            errorResponse.Message.Should().Be("Invalid user token");
            
            _mockAppointmentAttendeeBL.Verify(x => x.GetAvailableAttendeesAsync(It.IsAny<Guid?>()), Times.Never);
        }

        [Fact]
        public async Task GetAllAttendees_WhenBLThrowsArgumentException_ShouldReturnBadRequest()
        {
            // Arrange
            var errorMessage = "Invalid request parameters";
            _mockAppointmentAttendeeBL.Setup(x => x.GetAvailableAttendeesAsync(_currentUserId))
                .ThrowsAsync(new ArgumentException(errorMessage));

            // Act
            var result = await _controller.GetAllAttendeesAsync();

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            
            errorResponse.Message.Should().Be(errorMessage);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Get attendees failed - invalid argument")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllAttendees_WhenUnexpectedExceptionOccurs_ShouldReturn500()
        {
            // Arrange
            var exception = new Exception("Database connection failed");
            _mockAppointmentAttendeeBL.Setup(x => x.GetAvailableAttendeesAsync(_currentUserId))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.GetAllAttendeesAsync();

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
            
            var errorResponse = statusCodeResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("An error occurred while retrieving attendees");
            
            // Verify error logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Get attendees failed for user")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllAttendees_WithEmptyResult_ShouldReturnEmptyList()
        {
            // Arrange
            _mockAppointmentAttendeeBL.Setup(x => x.GetAvailableAttendeesAsync(_currentUserId))
                .ReturnsAsync(new List<AttendeeResponseDto>());

            // Act
            var result = await _controller.GetAllAttendeesAsync();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var attendees = okResult.Value.Should().BeOfType<List<AttendeeResponseDto>>().Subject;
            
            attendees.Should().BeEmpty();
        }

        #endregion

        #region CheckAttendeeAvailability Tests

        [Fact]
        public async Task CheckAttendeeAvailability_WithAvailableAttendee_ShouldReturnAvailable()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                ExcludeAppointmentId = null
            };

            var expectedResponse = new AvailabilityResponseDto
            {
                IsAvailable = true,
                Message = "Attendee is available",
                Attendee = new AttendeeResponseDto
                {
                    Id = request.AttendeeId,
                    Email = "attendee@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    FullName = "John Doe",
                },
                RequestedStartTime = request.StartDateTime,
                RequestedEndTime = request.EndDateTime
            };

            _mockAppointmentAttendeeBL.Setup(x => x.CheckAttendeeAvailabilityAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CheckAttendeeAvailabilityAsync(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AvailabilityResponseDto>().Subject;
            
            response.IsAvailable.Should().BeTrue();
            response.Message.Should().Be("Attendee is available");
            response.Attendee.Id.Should().Be(request.AttendeeId);
            response.RequestedStartTime.Should().Be(request.StartDateTime);
            response.RequestedEndTime.Should().Be(request.EndDateTime);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => 
                        o.ToString().Contains("Availability check") && 
                        o.ToString().Contains("True")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CheckAttendeeAvailability_WithUnavailableAttendee_ShouldReturnUnavailable()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            var expectedResponse = new AvailabilityResponseDto
            {
                IsAvailable = false,
                Message = "Attendee has a conflicting appointment",
                Attendee = new AttendeeResponseDto
                {
                    Id = request.AttendeeId,
                    Email = "busy@example.com",
                    FirstName = "Busy",
                    LastName = "User",
                    FullName = "Busy User",
                },
                RequestedStartTime = request.StartDateTime,
                RequestedEndTime = request.EndDateTime
            };

            _mockAppointmentAttendeeBL.Setup(x => x.CheckAttendeeAvailabilityAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CheckAttendeeAvailabilityAsync(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AvailabilityResponseDto>().Subject;
            
            response.IsAvailable.Should().BeFalse();
            response.Message.Should().Be("Attendee has a conflicting appointment");
        }

        [Fact]
        public async Task CheckAttendeeAvailability_WithExcludeAppointmentId_ShouldExcludeFromCheck()
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

            var expectedResponse = new AvailabilityResponseDto
            {
                IsAvailable = true,
                Message = "Attendee is available",
                Attendee = new AttendeeResponseDto { Id = request.AttendeeId },
                RequestedStartTime = request.StartDateTime,
                RequestedEndTime = request.EndDateTime
            };

            _mockAppointmentAttendeeBL.Setup(x => x.CheckAttendeeAvailabilityAsync(
                It.Is<CheckAvailabilityRequestDto>(r => r.ExcludeAppointmentId == excludeAppointmentId)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CheckAttendeeAvailabilityAsync(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AvailabilityResponseDto>().Subject;
            
            response.IsAvailable.Should().BeTrue();
            
            _mockAppointmentAttendeeBL.Verify(x => x.CheckAttendeeAvailabilityAsync(
                It.Is<CheckAvailabilityRequestDto>(r => r.ExcludeAppointmentId == excludeAppointmentId)), 
                Times.Once);
        }

        [Fact]
        public async Task CheckAttendeeAvailability_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.Empty,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddHours(-1) // End before start
            };

            _controller.ModelState.AddModelError("EndDateTime", "End time must be after start time");
            _controller.ModelState.AddModelError("AttendeeId", "Invalid attendee ID");

            // Act
            var result = await _controller.CheckAttendeeAvailabilityAsync(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            
            errorResponse.Message.Should().Be("Validation failed");
            errorResponse.Errors.Should().HaveCount(2);
            errorResponse.Errors.Should().Contain("End time must be after start time");
            errorResponse.Errors.Should().Contain("Invalid attendee ID");
            
            _mockAppointmentAttendeeBL.Verify(x => x.CheckAttendeeAvailabilityAsync(It.IsAny<CheckAvailabilityRequestDto>()), Times.Never);
        }

        [Fact]
        public async Task CheckAttendeeAvailability_WithInvalidUserToken_ShouldReturnUnauthorized()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            // Act
            var result = await _controller.CheckAttendeeAvailabilityAsync(request);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            
            errorResponse.Message.Should().Be("Invalid user token");
        }

        [Fact]
        public async Task CheckAttendeeAvailability_WhenBLThrowsArgumentException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            var errorMessage = "Invalid attendee ID";
            _mockAppointmentAttendeeBL.Setup(x => x.CheckAttendeeAvailabilityAsync(request))
                .ThrowsAsync(new ArgumentException(errorMessage));

            // Act
            var result = await _controller.CheckAttendeeAvailabilityAsync(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            
            errorResponse.Message.Should().Be(errorMessage);
            
            // Verify warning log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Availability check failed - invalid argument")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CheckAttendeeAvailability_WhenUnexpectedExceptionOccurs_ShouldReturn500()
        {
            // Arrange
            var request = new CheckAvailabilityRequestDto
            {
                AttendeeId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            var exception = new Exception("Database error");
            _mockAppointmentAttendeeBL.Setup(x => x.CheckAttendeeAvailabilityAsync(request))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.CheckAttendeeAvailabilityAsync(request);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
            
            var errorResponse = statusCodeResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("An error occurred while checking attendee availability");
            
            // Verify error log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Availability check failed for attendee")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region GetAppointmentAttendees Tests

        [Fact]
        public async Task GetAppointmentAttendees_WithValidAppointment_ShouldReturnAttendees()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var expectedAttendees = new List<AttendeeResponseDto>
            {
                new AttendeeResponseDto
                {
                    Id = Guid.NewGuid(),
                    Email = "organizer@example.com",
                    FirstName = "Organizer",
                    LastName = "User",
                    FullName = "Organizer User",
                },
                new AttendeeResponseDto
                {
                    Id = Guid.NewGuid(),
                    Email = "attendee1@example.com",
                    FirstName = "Attendee",
                    LastName = "One",
                    FullName = "Attendee One",
                },
                new AttendeeResponseDto
                {
                    Id = Guid.NewGuid(),
                    Email = "attendee2@example.com",
                    FirstName = "Attendee",
                    LastName = "Two",
                    FullName = "Attendee Two",
                }
            };

            _mockAppointmentAttendeeBL.Setup(x => x.GetAppointmentAttendeesAsync(appointmentId, _currentUserId))
                .ReturnsAsync(expectedAttendees);

            // Act
            var result = await _controller.GetAppointmentAttendeesAsync(appointmentId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var attendees = okResult.Value.Should().BeOfType<List<AttendeeResponseDto>>().Subject;
            
            attendees.Should().HaveCount(3);
            attendees.Should().BeEquivalentTo(expectedAttendees);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => 
                        o.ToString().Contains("Retrieved 3 attendees") && 
                        o.ToString().Contains(appointmentId.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAppointmentAttendees_WithEmptyAttendees_ShouldReturnEmptyList()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            _mockAppointmentAttendeeBL.Setup(x => x.GetAppointmentAttendeesAsync(appointmentId, _currentUserId))
                .ReturnsAsync(new List<AttendeeResponseDto>());

            // Act
            var result = await _controller.GetAppointmentAttendeesAsync(appointmentId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var attendees = okResult.Value.Should().BeOfType<List<AttendeeResponseDto>>().Subject;
            
            attendees.Should().BeEmpty();
            
            // Verify logging shows 0 attendees
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Retrieved 0 attendees")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAppointmentAttendees_WithNonExistentAppointment_ShouldReturnNotFound()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var errorMessage = "Appointment not found";
            
            _mockAppointmentAttendeeBL.Setup(x => x.GetAppointmentAttendeesAsync(appointmentId, _currentUserId))
                .ThrowsAsync(new ArgumentException(errorMessage));

            // Act
            var result = await _controller.GetAppointmentAttendeesAsync(appointmentId);

            // Assert
            var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            
            errorResponse.Message.Should().Be(errorMessage);
            
            // Verify warning log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Get appointment attendees failed - not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAppointmentAttendees_WithInvalidUserToken_ShouldReturnUnauthorized()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            var appointmentId = Guid.NewGuid();

            // Act
            var result = await _controller.GetAppointmentAttendeesAsync(appointmentId);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            
            errorResponse.Message.Should().Be("Invalid user token");
            
            _mockAppointmentAttendeeBL.Verify(x => x.GetAppointmentAttendeesAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetAppointmentAttendees_WhenUnexpectedExceptionOccurs_ShouldReturn500()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var exception = new Exception("Database connection failed");
            
            _mockAppointmentAttendeeBL.Setup(x => x.GetAppointmentAttendeesAsync(appointmentId, _currentUserId))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.GetAppointmentAttendeesAsync(appointmentId);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
            
            var errorResponse = statusCodeResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("An error occurred while retrieving appointment attendees");
            
            // Verify error log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => 
                        o.ToString().Contains("Get appointment attendees failed") &&
                        o.ToString().Contains(appointmentId.ToString())),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAppointmentAttendees_WithMultipleCallsForSameAppointment_ShouldReturnConsistentResults()
        {
            // Arrange
            var appointmentId = Guid.NewGuid();
            var expectedAttendees = new List<AttendeeResponseDto>
            {
                new AttendeeResponseDto
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    FirstName = "User",
                    LastName = "One",
                    FullName = "User One",
                }
            };

            _mockAppointmentAttendeeBL.Setup(x => x.GetAppointmentAttendeesAsync(appointmentId, _currentUserId))
                .ReturnsAsync(expectedAttendees);

            // Act
            var result1 = await _controller.GetAppointmentAttendeesAsync(appointmentId);
            var result2 = await _controller.GetAppointmentAttendeesAsync(appointmentId);

            // Assert
            var okResult1 = result1.Result.Should().BeOfType<OkObjectResult>().Subject;
            var attendees1 = okResult1.Value.Should().BeOfType<List<AttendeeResponseDto>>().Subject;

            var okResult2 = result2.Result.Should().BeOfType<OkObjectResult>().Subject;
            var attendees2 = okResult2.Value.Should().BeOfType<List<AttendeeResponseDto>>().Subject;

            attendees1.Should().BeEquivalentTo(attendees2);
            
            _mockAppointmentAttendeeBL.Verify(x => x.GetAppointmentAttendeesAsync(appointmentId, _currentUserId), Times.Exactly(2));
        }

        #endregion
    }
}

