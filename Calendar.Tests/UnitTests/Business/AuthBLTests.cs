using Xunit;
using Moq;
using FluentAssertions;
using Calendar.Business;
using Calendar.DataAccess;
using Calendar.DTOs;
using Calendar.Models;
using Calendar.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Calendar.Tests.Business
{
    public class AuthBLTests
    {
        private readonly Mock<IUserDAL> _mockUserDAL;
        private readonly Mock<JwtHelper> _mockJwtHelper;
        private readonly Mock<PasswordHasher> _mockPasswordHasher;
        private readonly Mock<ILogger<AuthBL>> _mockLogger;
        private readonly AuthBL _authBL;

        public AuthBLTests()
        {
            var configurationMock = new Mock<IConfiguration>();
            var jwtLoggerMock = new Mock<ILogger<JwtHelper>>();
            var passwordHasherLoggerMock = new Mock<ILogger<PasswordHasher>>();
            
            _mockUserDAL = new Mock<IUserDAL>();
            _mockJwtHelper = new Mock<JwtHelper>(configurationMock.Object, jwtLoggerMock.Object);
            _mockPasswordHasher = new Mock<PasswordHasher>(configurationMock.Object, passwordHasherLoggerMock.Object);
            _mockLogger = new Mock<ILogger<AuthBL>>();
            
            // Setup configuration values
            configurationMock.Setup(c => c["PASSWORD_SECRET_KEY"])
                .Returns("YourPasswordSecretKeyThatIsAlsoVeryLongAndSecure123456");
            configurationMock.Setup(c => c["JWT_SECRET_KEY"])
                .Returns("YourSuperSecretKeyThatIsAtLeast32CharactersLong12345");

            _authBL = new AuthBL(
                _mockUserDAL.Object,
                _mockJwtHelper.Object,
                _mockPasswordHasher.Object,
                _mockLogger.Object
            );
        }

        #region Signup Tests - Essential Cases Only

        [Fact]
        public async Task SignupAsync_WithValidNewUser_ShouldReturnAuthResponse()
        {
            // Arrange
            var signupRequest = new SignupRequestDto
            {
                Email = "  Test@Example.COM  ", // Test trimming and lowercase
                Password = "Test@123",
                FirstName = "  John  ", // Test trimming
                LastName = "  Doe  "    // Test trimming
            };

            var hashedPassword = "hashed_password_abc123";
            var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test";
            var tokenExpiry = DateTime.UtcNow.AddHours(24);

            _mockUserDAL.Setup(x => x.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockPasswordHasher.Setup(x => x.HashPassword(signupRequest.Password))
                .Returns(hashedPassword);

            _mockUserDAL.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) => user);

            _mockJwtHelper.Setup(x => x.GenerateToken(It.IsAny<User>()))
                .Returns(jwtToken);

            _mockJwtHelper.Setup(x => x.GetTokenExpiry())
                .Returns(tokenExpiry);

            // Act
            var result = await _authBL.SignupAsync(signupRequest);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be(jwtToken);
            result.ExpiresAt.Should().Be(tokenExpiry);
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be("test@example.com"); // Verify lowercase
            result.User.FirstName.Should().Be("John"); // Verify trimmed
            result.User.LastName.Should().Be("Doe");   // Verify trimmed
            result.User.FullName.Should().Be("John Doe");
            result.User.IsActive.Should().BeTrue();

            // Verify all methods were called correctly
            _mockUserDAL.Verify(x => x.ExistsAsync("  Test@Example.COM  "), Times.Once);
            _mockPasswordHasher.Verify(x => x.HashPassword(signupRequest.Password), Times.Once);
            _mockUserDAL.Verify(x => x.CreateAsync(It.Is<User>(u =>
                u.Email == "test@example.com" &&
                u.FirstName == "John" &&
                u.LastName == "Doe" &&
                u.PasswordHash == hashedPassword &&
                u.IsActive == true &&
                u.Id != Guid.Empty &&
                u.CreatedAt > DateTime.MinValue &&
                u.UpdatedAt > DateTime.MinValue
            )), Times.Once);
            _mockJwtHelper.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Once);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("User registered successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SignupAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var signupRequest = new SignupRequestDto
            {
                Email = "existing@example.com",
                Password = "Test@123",
                FirstName = "Jane",
                LastName = "Smith"
            };

            _mockUserDAL.Setup(x => x.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authBL.SignupAsync(signupRequest));

            exception.Message.Should().Be("User with this email already exists.");

            // Verify no user creation attempted
            _mockUserDAL.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
            _mockPasswordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
            _mockJwtHelper.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task SignupAsync_WhenDALThrowsException_ShouldLogErrorAndRethrow()
        {
            // Arrange
            var signupRequest = new SignupRequestDto
            {
                Email = "test@example.com",
                Password = "Test@123",
                FirstName = "John",
                LastName = "Doe"
            };

            var expectedException = new Exception("Database connection failed");

            _mockUserDAL.Setup(x => x.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns("hashed");

            _mockUserDAL.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _authBL.SignupAsync(signupRequest));

            exception.Should().Be(expectedException);

            // Verify error logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error during user registration")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Login Tests - Essential Cases Only

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Email = "User@Example.COM", // Test case insensitive
                Password = "Test@123"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                PasswordHash = "stored_hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.login";
            var tokenExpiry = DateTime.UtcNow.AddHours(24);

            _mockUserDAL.Setup(x => x.GetByEmailAsync(loginRequest.Email))
                .ReturnsAsync(user);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(loginRequest.Password, user.PasswordHash))
                .Returns(true);

            _mockJwtHelper.Setup(x => x.GenerateToken(user))
                .Returns(jwtToken);

            _mockJwtHelper.Setup(x => x.GetTokenExpiry())
                .Returns(tokenExpiry);

            // Act
            var result = await _authBL.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be(jwtToken);
            result.ExpiresAt.Should().Be(tokenExpiry);
            result.User.Should().NotBeNull();
            result.User.Id.Should().Be(user.Id);
            result.User.Email.Should().Be(user.Email);
            result.User.FirstName.Should().Be(user.FirstName);
            result.User.LastName.Should().Be(user.LastName);
            result.User.FullName.Should().Be("John Doe");
            result.User.IsActive.Should().BeTrue();
            result.User.CreatedAt.Should().Be(user.CreatedAt);

            // Verify all methods were called
            _mockUserDAL.Verify(x => x.GetByEmailAsync(loginRequest.Email), Times.Once);
            _mockPasswordHasher.Verify(x => x.VerifyPassword(loginRequest.Password, user.PasswordHash), Times.Once);
            _mockJwtHelper.Verify(x => x.GenerateToken(user), Times.Once);
            _mockJwtHelper.Verify(x => x.GetTokenExpiry(), Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("User logged in successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentUser_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Email = "nonexistent@example.com",
                Password = "Test@123"
            };

            _mockUserDAL.Setup(x => x.GetByEmailAsync(loginRequest.Email))
                .ReturnsAsync((User)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _authBL.LoginAsync(loginRequest));

            exception.Message.Should().Be("Invalid email or password.");

            // Verify password verification was never called
            _mockPasswordHasher.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockJwtHelper.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithIncorrectPassword_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Email = "user@example.com",
                Password = "WrongPassword123"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                PasswordHash = "stored_hash",
                IsActive = true
            };

            _mockUserDAL.Setup(x => x.GetByEmailAsync(loginRequest.Email))
                .ReturnsAsync(user);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(loginRequest.Password, user.PasswordHash))
                .Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _authBL.LoginAsync(loginRequest));

            exception.Message.Should().Be("Invalid email or password.");

            // Verify JWT generation was never called
            _mockJwtHelper.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WhenDALThrowsException_ShouldLogErrorAndRethrow()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Email = "user@example.com",
                Password = "Test@123"
            };

            var expectedException = new Exception("Database timeout");

            _mockUserDAL.Setup(x => x.GetByEmailAsync(loginRequest.Email))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _authBL.LoginAsync(loginRequest));

            exception.Should().Be(expectedException);

            // Verify error logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error during login")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion
    }
}
