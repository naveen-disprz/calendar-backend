using System;
using System.Net;
using System.Threading.Tasks;
using Calendar.DTOs;
using Calendar.IntegrationTests.Common;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Calendar.IntegrationTests.Controllers
{
    public class AuthIntegrationTests : IntegrationTestBase
    {
        public AuthIntegrationTests(CustomWebApplicationFactory factory, ITestOutputHelper output) 
            : base(factory, output)
        {
        }

        [Fact]
        public async Task Signup_WithValidData_ShouldCreateUserAndReturnToken()
        {
            // Arrange
            var signupRequest = new SignupRequestDto
            {
                Email = $"testuser_{Guid.NewGuid()}@example.com",
                Password = "Test@123456",
                ConfirmPassword = "Test@123456",
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var response = await Client.PostAsync("/api/auth/signup", CreateJsonContent(signupRequest));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var authResponse = await DeserializeResponse<AuthResponseDto>(response);
            authResponse.Should().NotBeNull();
            authResponse.User.Email.Should().Be(signupRequest.Email.ToLower());
            
            Output.WriteLine($"User created with ID: {authResponse.User.Id}");
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange - Use helper method to create user
            var email = $"logintest_{Guid.NewGuid()}@example.com";
            var password = "Test@123456";
            
            var authResponse = await CreateAndAuthenticateUser(email, password, "Login", "Test");

            // Act - Login
            var loginRequest = new LoginRequestDto
            {
                Email = email,
                Password = password
            };

            var loginResponse = await Client.PostAsync("/api/auth/login", CreateJsonContent(loginRequest));

            // Assert
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var loginAuth = await DeserializeResponse<AuthResponseDto>(loginResponse);
            loginAuth.Should().NotBeNull();
            loginAuth!.User.Id.Should().Be(authResponse.User.Id);
        }
    }
}
