using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Calendar.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Calendar.IntegrationTests.Common
{
    public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        protected readonly CustomWebApplicationFactory Factory;
        protected readonly HttpClient Client;
        protected readonly ITestOutputHelper Output;
        protected readonly JsonSerializerOptions JsonOptions;

        protected IntegrationTestBase(CustomWebApplicationFactory factory, ITestOutputHelper output)
        {
            Factory = factory;
            Output = output;
            Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        #region Helper Methods

        protected StringContent CreateJsonContent(object obj)
        {
            return new StringContent(
                JsonSerializer.Serialize(obj),
                Encoding.UTF8,
                "application/json"
            );
        }

        protected async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }

        protected void AddAuthorizationHeader(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        protected void RemoveAuthorizationHeader()
        {
            Client.DefaultRequestHeaders.Authorization = null;
        }

        protected async Task<AuthResponseDto> CreateAndAuthenticateUser(
            string? email = null, 
            string? password = null, 
            string? firstName = null, 
            string? lastName = null)
        {
            email ??= $"user_{Guid.NewGuid()}@example.com";
            password ??= "Test@123456";
            firstName ??= "Test";
            lastName ??= "User";

            var signupRequest = new SignupRequestDto
            {
                Email = email,
                Password = password,
                ConfirmPassword = password,
                FirstName = firstName,
                LastName = lastName
            };
            
            var loginRequest = new LoginRequestDto
            {
                Email = email,
                Password = password,
            };

            var signupResponse = await Client.PostAsync("/api/auth/signup", CreateJsonContent(signupRequest));
            signupResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            
            var loginResponse = await Client.PostAsync("/api/auth/login", CreateJsonContent(loginRequest));
            loginResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var authResponse = await DeserializeResponse<AuthResponseDto>(loginResponse);
            authResponse.Should().NotBeNull();

            return authResponse!;
        }

        protected async Task<string> GetAuthToken(string email, string password)
        {
            var loginRequest = new LoginRequestDto
            {
                Email = email,
                Password = password
            };

            var response = await Client.PostAsync("/api/auth/login", CreateJsonContent(loginRequest));
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var authResponse = await DeserializeResponse<AuthResponseDto>(response);
            return authResponse!.Token;
        }

        #endregion

        public virtual void Dispose()
        {
            Client?.Dispose();
        }
    }
}
