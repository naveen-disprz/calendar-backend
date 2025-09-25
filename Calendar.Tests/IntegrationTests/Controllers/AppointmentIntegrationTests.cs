using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Calendar.DTOs;
using Calendar.IntegrationTests.Common;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Calendar.IntegrationTests.Controllers
{
    public class AppointmentIntegrationTests : IntegrationTestBase
    {
        public AppointmentIntegrationTests(CustomWebApplicationFactory factory, ITestOutputHelper output) 
            : base(factory, output)
        {
        }

        [Fact]
        public async Task GetAppointmentTypes_WithAuthentication_ShouldReturnTypes()
        {
            // Arrange - Create and authenticate user
            var authResponse = await CreateAndAuthenticateUser();
            LoginRequestDto loginRequestDto = new LoginRequestDto
            {
                Email = authResponse.User.Email,
                Password = "Test@123456"
            };
            var loginResponse = await Client.PostAsync("/api/auth/login", CreateJsonContent(loginRequestDto));
            var loginR = DeserializeResponse<AuthResponseDto>(loginResponse);

            // AddAuthorizationHeader(authResponse.Token);
            Client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
            // AddAuthorizationHeader(loginR.Result.Token);
            
            // Act
            var response = await Client.GetAsync("/api/appointment/types");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var types = await DeserializeResponse<List<AppointmentTypeResponseDto>>(response);
            types.Should().NotBeNull();
            types.Should().NotBeEmpty();
            
            Output.WriteLine($"Found {types!.Count} appointment types");
        }

        [Fact]
        public async Task CreateAppointment_WithValidData_ShouldCreateAppointment()
        {
            // Arrange
            var authResponse = await CreateAndAuthenticateUser();
            AddAuthorizationHeader(authResponse.Token);

            var appointmentRequest = new CreateAppointmentRequestDto
            {
                Title = "Test Meeting",
                Description = "Test Description",
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Location = "Conference Room A",
                Recurrence = new RecurrenceRequestDto
                {
                    Frequency = "WEEKLY",
                    DaysOfWeek = [
                        DayOfWeek.Sunday, 
                        DayOfWeek.Monday, 
                        DayOfWeek.Tuesday, 
                        DayOfWeek.Wednesday, 
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday, 
                        DayOfWeek.Saturday, 
                    ],
                    EndDate = DateTime.UtcNow.AddDays(5),
                }
            };

            // Act
            var response = await Client.PostAsync("/api/appointment", CreateJsonContent(appointmentRequest));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var appointment = await DeserializeResponse<AppointmentResponseDto>(response);
            appointment.Should().NotBeNull();
            appointment!.Title.Should().Be(appointmentRequest.Title);
        }
    }
}
