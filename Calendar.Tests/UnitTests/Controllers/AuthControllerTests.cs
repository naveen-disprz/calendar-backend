using Xunit;
using Moq;
using FluentAssertions;
using Calendar.Controllers;
using Calendar.Business;
using Calendar.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Calendar.Tests.Controllers;

public class AuthControllerTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IAuthBL> _mockAuthBL;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _mockAuthBL = new Mock<IAuthBL>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mockAuthBL.Object, _mockLogger.Object);
    }

    #region Signup Tests

    [Fact]
    public async Task Signup_WithValidRequest_ShouldReturnOkWithAuthResponse()
    {
        // Arrange
        var signupRequest = new SignupRequestDto
        {
            Email = "test@example.com",
            Password = "Tes",
            FirstName = "John",
            LastName = "Doe"
        };

        var expectedResponse = new AuthResponseDto
        {
            Token = "jwt_token_123",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserResponseDto
            {
                Id = Guid.NewGuid(),
                Email = signupRequest.Email.ToLower(),
                FirstName = signupRequest.FirstName,
                LastName = signupRequest.LastName,
                FullName = "John Doe",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockAuthBL.Setup(x => x.SignupAsync(signupRequest))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Signup(signupRequest);


        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
        authResponse.Token.Should().Be(expectedResponse.Token);

        _mockAuthBL.Verify(x => x.SignupAsync(signupRequest), Times.Once);
    }

    [Fact]
    public async Task Signup_WhenUserAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var signupRequest = new SignupRequestDto
        {
            Email = "existing@example.com",
            Password = "Test@123",
            FirstName = "Jane",
            LastName = "Smith"
        };

        _mockAuthBL.Setup(x => x.SignupAsync(signupRequest))
            .ThrowsAsync(new InvalidOperationException("User with this email already exists."));

        // Act
        var result = await _controller.Signup(signupRequest);

        // Assert
        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Signup_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        var signupRequest = new SignupRequestDto
        {
            Email = "invalid",
            Password = "123",
            FirstName = "",
            LastName = ""
        };

        _controller.ModelState.AddModelError("Email", "Invalid email format");

        // Act
        var result = await _controller.Signup(signupRequest);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        _mockAuthBL.Verify(x => x.SignupAsync(It.IsAny<SignupRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task Signup_WithInternalServerError_ShouldReturnInternalServerError()
    {
        // Arrange
        var signupRequest = new SignupRequestDto
        {
            Email = "test@example.com",
            Password = "Test@123",
            FirstName = "John",
            LastName = "Doe"
        };

        var unexpectedException = new Exception("Database connection failed");

        _mockAuthBL.Setup(x => x.SignupAsync(signupRequest))
            .ThrowsAsync(unexpectedException);

        // Act
        var result = await _controller.Signup(signupRequest);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);

        var errorResponse = objectResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.Message.Should().Be("An error occurred during registration");
        errorResponse.Errors.Should().BeEmpty(); // No specific errors for internal server error

        // Verify that the error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Signup failed")),
                unexpectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Verify that the BL method was called
        _mockAuthBL.Verify(x => x.SignupAsync(signupRequest), Times.Once);
    }

    #endregion
    
    

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithAuthResponse()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "Test@123"
        };

        var expectedResponse = new AuthResponseDto
        {
            Token = "jwt_token_123",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserResponseDto
            {
                Id = Guid.NewGuid(),
                Email = loginRequest.Email.ToLower(),
                FirstName = "John",
                LastName = "Doe",
                FullName = "John Doe",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
        
        

        _mockAuthBL.Setup(x => x.LoginAsync(loginRequest))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
        authResponse.Token.Should().Be(expectedResponse.Token);

        _mockAuthBL.Verify(x => x.LoginAsync(loginRequest), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        _mockAuthBL.Setup(x => x.LoginAsync(loginRequest))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "",
            Password = ""
        };

        _controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        _mockAuthBL.Verify(x => x.LoginAsync(It.IsAny<LoginRequestDto>()), Times.Never);
    }
    
    [Fact]
    public async Task Login_WithInternalServerError_ShouldReturnInternalServerError()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test@123",
        };

        var unexpectedException = new Exception("Database connection failed");

        _mockAuthBL.Setup(x => x.LoginAsync(loginRequest))
            .ThrowsAsync(unexpectedException);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);

        var errorResponse = objectResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.Message.Should().Be("An error occurred during login");
        errorResponse.Errors.Should().BeEmpty(); // No specific errors for internal server error

        // Verify that the error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Login failed")),
                unexpectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Verify that the BL method was called
        _mockAuthBL.Verify(x => x.LoginAsync(loginRequest), Times.Once);
    }

    #endregion
}