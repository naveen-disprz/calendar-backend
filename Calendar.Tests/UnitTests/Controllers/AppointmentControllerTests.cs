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
using System.Security.Claims;
using System.Threading.Tasks;
using Calendar.Models;

namespace Calendar.Tests.Controllers
{
    public class AppointmentControllerTests
    {
        private readonly Mock<IAppointmentBL> _mockAppointmentBL;
        private readonly Mock<ILogger<AppointmentController>> _mockLogger;
        private readonly AppointmentController _controller;

        public AppointmentControllerTests()
        {
            _mockAppointmentBL = new Mock<IAppointmentBL>();
            _mockLogger = new Mock<ILogger<AppointmentController>>();
            _controller = new AppointmentController(_mockAppointmentBL.Object, _mockLogger.Object);

            // Setup default user context with valid user ID
            SetupUserContext(Guid.NewGuid().ToString());
        }

        private void SetupUserContext(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region CreateAppointment Tests

        [Fact]
        public async Task CreateAppointment_WithValidRequest_ShouldReturnCreatedAtAction()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new CreateAppointmentRequestDto
            {
                Title = "Team Meeting",
                Description = "Weekly team sync",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Location = "Conference Room A",
                AttendeeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            };

            var expectedResponse = new AppointmentResponseDto
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Location = request.Location,
                OrganizerId = userId,
                OrganizerName = "John Doe",
                IsRecurring = false,
                FormattedTimeRange = "10:00 - 11:00",
                FormattedDateTimeRange = "Jan 15, 2024 10:00 - 11:00",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockAppointmentBL.Setup(x => x.CreateAppointmentAsync(request, userId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateAppointmentAsync(request);

            // Assert
            var createdAtActionResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            createdAtActionResult.StatusCode.Should().Be(201);

            var response = createdAtActionResult.Value.Should().BeOfType<AppointmentResponseDto>().Subject;
            response.Id.Should().Be(expectedResponse.Id);
            response.Title.Should().Be(expectedResponse.Title);

            _mockAppointmentBL.Verify(x => x.CreateAppointmentAsync(request, userId), Times.Once);

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
        public async Task CreateAppointment_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CreateAppointmentRequestDto
            {
                Title = "", // Invalid
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            _controller.ModelState.AddModelError("Title", "Title is required");
            _controller.ModelState.AddModelError("Location", "Location exceeds maximum length");

            // Act
            var result = await _controller.CreateAppointmentAsync(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;

            errorResponse.Message.Should().Be("Validation failed");
            errorResponse.Errors.Should().Contain("Title is required");
            errorResponse.Errors.Should().Contain("Location exceeds maximum length");

            _mockAppointmentBL.Verify(
                x => x.CreateAppointmentAsync(It.IsAny<CreateAppointmentRequestDto>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateAppointment_WithInvalidUserToken_ShouldReturnUnauthorized()
        {
            // Arrange
            SetupUserContext("invalid-guid"); // Invalid GUID format

            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            // Act
            var result = await _controller.CreateAppointmentAsync(request);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("Invalid user token");

            _mockAppointmentBL.Verify(
                x => x.CreateAppointmentAsync(It.IsAny<CreateAppointmentRequestDto>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateAppointment_WithConflictingAppointment_ShouldReturnConflict()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            _mockAppointmentBL.Setup(x => x.CreateAppointmentAsync(request, userId))
                .ThrowsAsync(new InvalidOperationException("Appointment conflicts with existing appointment"));

            // Act
            var result = await _controller.CreateAppointmentAsync(request);

            // Assert
            var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(409);

            var errorResponse = conflictResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("Appointment conflicts with existing appointment");
        }

        [Fact]
        public async Task CreateAppointment_WithInvalidArgument_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(2),
                EndDateTime = DateTime.UtcNow.AddHours(1) // End before start
            };

            _mockAppointmentBL.Setup(x => x.CreateAppointmentAsync(request, userId))
                .ThrowsAsync(new ArgumentException("End time must be after start time"));

            // Act
            var result = await _controller.CreateAppointmentAsync(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("End time must be after start time");
        }

        [Fact]
        public async Task CreateAppointment_WithUnauthorizedAccess_ShouldReturnUnauthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            _mockAppointmentBL.Setup(x => x.CreateAppointmentAsync(request, userId))
                .ThrowsAsync(new UnauthorizedAccessException("User not authorized"));

            // Act
            var result = await _controller.CreateAppointmentAsync(request);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("User not authorized");
        }

        [Fact]
        public async Task CreateAppointment_WithInternalServerError_ShouldReturn500()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            _mockAppointmentBL.Setup(x => x.CreateAppointmentAsync(request, userId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.CreateAppointmentAsync(request);

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);

            var errorResponse = objectResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("An error occurred while creating the appointment");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Appointment creation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region UpdateAppointment Tests

        [Fact]
        public async Task UpdateAppointment_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new UpdateAppointmentRequestDto
            {
                Title = "Updated Meeting",
                Description = "Updated description",
                StartDateTime = DateTime.UtcNow.AddHours(2),
                EndDateTime = DateTime.UtcNow.AddHours(3),
                Location = "Room B",
                AttendeeIds = new List<Guid> { Guid.NewGuid() }
            };

            var expectedResponse = new AppointmentResponseDto
            {
                Id = appointmentId,
                Title = request.Title,
                Description = request.Description,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Location = request.Location,
                OrganizerId = userId,
                UpdatedAt = DateTime.UtcNow
            };

            _mockAppointmentBL.Setup(x => x.UpdateAppointmentAsync(appointmentId, request, userId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.UpdateAppointmentAsync(appointmentId, request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AppointmentResponseDto>().Subject;
            response.Id.Should().Be(appointmentId);
            response.Title.Should().Be(request.Title);

            _mockAppointmentBL.Verify(x => x.UpdateAppointmentAsync(appointmentId, request, userId), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointment_WithNonExistentAppointment_ShouldReturnNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new UpdateAppointmentRequestDto
            {
                Title = "Updated Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(2),
                EndDateTime = DateTime.UtcNow.AddHours(3)
            };

            _mockAppointmentBL.Setup(x => x.UpdateAppointmentAsync(appointmentId, request, userId))
                .ThrowsAsync(new ArgumentException("Appointment not found"));

            // Act
            var result = await _controller.UpdateAppointmentAsync(appointmentId, request);

            // Assert
            var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("Appointment not found");
        }

        [Fact]
        public async Task UpdateAppointment_WithUnauthorizedUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new UpdateAppointmentRequestDto
            {
                Title = "Updated Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(2),
                EndDateTime = DateTime.UtcNow.AddHours(3)
            };

            _mockAppointmentBL.Setup(x => x.UpdateAppointmentAsync(appointmentId, request, userId))
                .ThrowsAsync(new UnauthorizedAccessException("Only organizer can update appointment"));

            // Act
            var result = await _controller.UpdateAppointmentAsync(appointmentId, request);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("Only organizer can update appointment");
        }

        #endregion

        #region GetAppointments Tests

        [Fact]
        public async Task GetAppointments_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var fromDate = DateTime.Today;
            var toDate = fromDate.AddDays(7);
            var appointmentTypeId = Guid.NewGuid();

            var expectedAppointments = new List<AppointmentResponseDto>
            {
                new AppointmentResponseDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Meeting 1",
                    StartDateTime = fromDate.AddHours(10),
                    EndDateTime = fromDate.AddHours(11),
                    OrganizerId = userId
                },
                new AppointmentResponseDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Meeting 2",
                    StartDateTime = fromDate.AddDays(1).AddHours(14),
                    EndDateTime = fromDate.AddDays(1).AddHours(15),
                    OrganizerId = userId
                }
            };

            _mockAppointmentBL.Setup(x => x.GetAppointmentsAsync(userId, fromDate, toDate, appointmentTypeId, true))
                .ReturnsAsync(expectedAppointments);

            // Act
            var result = await _controller.GetAppointmentsAsync(fromDate, toDate, appointmentTypeId, true);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var appointments = okResult.Value.Should().BeAssignableTo<List<AppointmentResponseDto>>().Subject;
            appointments.Should().HaveCount(2);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Retrieved 2 appointments")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAppointments_WithDefaultDates_ShouldUseDefaultRange()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var expectedAppointments = new List<AppointmentResponseDto>();

            _mockAppointmentBL.Setup(x => x.GetAppointmentsAsync(
                    userId,
                    It.Is<DateTime>(d => d.Date == DateTime.Today),
                    It.Is<DateTime>(d => d.Date == DateTime.Today.AddDays(7)),
                    null,
                    true))
                .ReturnsAsync(expectedAppointments);

            // Act
            var result = await _controller.GetAppointmentsAsync(null, null, null, true);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            _mockAppointmentBL.Verify(x => x.GetAppointmentsAsync(
                userId,
                It.Is<DateTime>(d => d.Date == DateTime.Today),
                It.Is<DateTime>(d => d.Date == DateTime.Today.AddDays(7)),
                null,
                true), Times.Once);
        }

        [Fact]
        public async Task GetAppointments_WithInvalidDateRange_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var fromDate = DateTime.Today.AddDays(7);
            var toDate = DateTime.Today; // Before fromDate

            // Act
            var result = await _controller.GetAppointmentsAsync(fromDate, toDate, null, true);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("fromDate cannot be greater than toDate");
        }

        [Fact]
        public async Task GetAppointments_WithExcessiveDateRange_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var fromDate = DateTime.Today;
            var toDate = DateTime.Today.AddDays(400); // Exceeds 365 days

            // Act
            var result = await _controller.GetAppointmentsAsync(fromDate, toDate, null, true);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("Date range cannot exceed 365 days");
        }

        #endregion

        #region DeleteAppointment Tests

        [Fact]
        public async Task DeleteAppointment_WithValidRequest_ShouldReturnNoContent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            _mockAppointmentBL.Setup(x => x.DeleteAppointmentAsync(appointmentId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteAppointmentAsync(appointmentId);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            _mockAppointmentBL.Verify(x => x.DeleteAppointmentAsync(appointmentId, userId), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Appointment deleted successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAppointment_WithNonExistentAppointment_ShouldReturnNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            _mockAppointmentBL.Setup(x => x.DeleteAppointmentAsync(appointmentId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteAppointmentAsync(appointmentId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("Appointment not found");
        }

        [Fact]
        public async Task DeleteAppointment_WithUnauthorizedUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            _mockAppointmentBL.Setup(x => x.DeleteAppointmentAsync(appointmentId, userId))
                .ThrowsAsync(new UnauthorizedAccessException("Only organizer can delete appointment"));

            // Act
            var result = await _controller.DeleteAppointmentAsync(appointmentId);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("Only organizer can delete appointment");
        }

        #endregion

        #region GetAppointmentTypes Tests

        [Fact]
        public async Task GetAppointmentTypes_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var expectedTypes = new List<AppointmentType>
            {
                new AppointmentType { Id = Guid.NewGuid(), Name = "Meeting", Color = "#FF0000" },
                new AppointmentType { Id = Guid.NewGuid(), Name = "Interview", Color = "#00FF00" },
                new AppointmentType { Id = Guid.NewGuid(), Name = "Personal", Color = "#0000FF" }
            };

            _mockAppointmentBL.Setup(x => x.GetAppointmentTypesAsync())
                .ReturnsAsync(expectedTypes);

            // Act
            var result = await _controller.GetAppointmentTypesAsync();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var types = okResult.Value.Should().BeAssignableTo<List<AppointmentType>>().Subject;
            types.Should().HaveCount(3);
            types.Should().Contain(t => t.Name == "Meeting");

            _mockAppointmentBL.Verify(x => x.GetAppointmentTypesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAppointmentTypes_WithInvalidUserToken_ShouldReturnUnauthorized()
        {
            // Arrange
            SetupUserContext("invalid-guid");

            // Act
            var result = await _controller.GetAppointmentTypesAsync();

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("Invalid user token");
        }

        [Fact]
        public async Task GetAppointmentTypes_WithInternalServerError_ShouldReturn500()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            _mockAppointmentBL.Setup(x => x.GetAppointmentTypesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAppointmentTypesAsync();

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);

            var errorResponse = objectResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("An error occurred while fetching the appointment types");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task CreateAppointment_WithNoUserClaim_ShouldReturnUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var request = new CreateAppointmentRequestDto
            {
                Title = "Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };

            // Act
            var result = await _controller.CreateAppointmentAsync(request);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
            errorResponse.Message.Should().Be("Invalid user token");
        }

        [Fact]
        public async Task CreateAppointment_WithRecurrenceRequest_ShouldPassRecurrenceData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new CreateAppointmentRequestDto
            {
                Title = "Weekly Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Recurrence = new RecurrenceRequestDto
                {
                    Frequency = "WEEKLY",
                    DaysOfWeek = new List<DayOfWeek>
                    {
                        DayOfWeek.Sunday, 
                        DayOfWeek.Monday, 
                        DayOfWeek.Tuesday, 
                        DayOfWeek.Wednesday, 
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday, 
                        DayOfWeek.Saturday,
                    },
                    EndDate = DateTime.UtcNow.AddMonths(3)
                }
            };

            var expectedResponse = new AppointmentResponseDto
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                IsRecurring = true,
                Recurrence = new RecurrenceResponseDto
                {
                    Id = Guid.NewGuid(),
                    Frequency = "WEEKLY",
                    DaysOfWeek = request.Recurrence.DaysOfWeek,
                    EndDate = request.Recurrence.EndDate
                }
            };

            _mockAppointmentBL.Setup(x => x.CreateAppointmentAsync(
                    It.Is<CreateAppointmentRequestDto>(r => r.Recurrence != null && r.Recurrence.Frequency == "WEEKLY"),
                    userId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateAppointmentAsync(request);

            // Assert
            var createdAtActionResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            var response = createdAtActionResult.Value.Should().BeOfType<AppointmentResponseDto>().Subject;
            response.IsRecurring.Should().BeTrue();
            response.Recurrence.Should().NotBeNull();
            response.Recurrence.Frequency.Should().Be("WEEKLY");
        }

        [Fact]
        public async Task UpdateAppointment_WithRecurrenceUpdate_ShouldPassRecurrenceData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            SetupUserContext(userId.ToString());

            var request = new UpdateAppointmentRequestDto
            {
                Title = "Updated Recurring Meeting",
                StartDateTime = DateTime.UtcNow.AddHours(2),
                EndDateTime = DateTime.UtcNow.AddHours(3),
                Recurrence = new RecurrenceRequestDto
                {
                    Frequency = "MONTHLY",
                    DaysOfMonth = new List<int> { 1, 15 },
                    EndDate = DateTime.UtcNow.AddMonths(6)
                }
            };

            var expectedResponse = new AppointmentResponseDto
            {
                Id = appointmentId,
                Title = request.Title,
                IsRecurring = true,
                Recurrence = new RecurrenceResponseDto
                {
                    Frequency = "MONTHLY",
                    DaysOfMonth = request.Recurrence.DaysOfMonth
                }
            };

            _mockAppointmentBL.Setup(x => x.UpdateAppointmentAsync(
                    appointmentId,
                    It.Is<UpdateAppointmentRequestDto>(r =>
                        r.Recurrence != null && r.Recurrence.Frequency == "MONTHLY"),
                    userId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.UpdateAppointmentAsync(appointmentId, request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AppointmentResponseDto>().Subject;
            response.IsRecurring.Should().BeTrue();
            response.Recurrence.Frequency.Should().Be("MONTHLY");
        }

        #endregion
    }
}