using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Calendar.DTOs;
using Calendar.IntegrationTests.Common;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Calendar.IntegrationTests.Controllers
{
    public class AttendeeIntegrationTests : IntegrationTestBase
    {
        public AttendeeIntegrationTests(CustomWebApplicationFactory factory, ITestOutputHelper output) 
            : base(factory, output)
        {
        }

        [Fact]
        public async Task GetAllAttendees_WithAuthentication_ShouldReturnAttendees()
        {
            // Arrange
            var authResponse = await CreateAndAuthenticateUser();
            AddAuthorizationHeader(authResponse.Token);

            // Act
            var response = await Client.GetAsync("/api/attendee?excludeCurrentUser=true");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var attendees = await DeserializeResponse<List<AttendeeResponseDto>>(response);
            attendees.Should().NotBeNull();
            
            Output.WriteLine($"Found {attendees!.Count} attendees");
        }
    }
}