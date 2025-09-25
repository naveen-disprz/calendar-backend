using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Calendar.Data;
using Calendar.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Calendar.IntegrationTests.Common
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.EnableSensitiveDataLogging();
                });

                // Build service provider and ensure database is created
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();
                    SeedTestData(db);
                }
            });

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(GetTestConfiguration());
            });

            builder.UseEnvironment("Testing");
        }

        private static Dictionary<string, string?> GetTestConfiguration()
        {
            return new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "TestConnection",
                ["JWT_SECRET_KEY"] = "YourSuperSecretKeyThatIsAtLeast32CharactersLong12345",
                ["PASSWORD_SECRET_KEY"] = "YourPasswordSecretKeyThatIsAlsoVeryLongAndSecure123456",
                ["JwtSettings:SecretKey"] = "YourSuperSecretKeyThatIsAtLeast32CharactersLong12345",
                ["JwtSettings:Issuer"] = "CalendarApp",
                ["JwtSettings:Audience"] = "CalendarApp",
                ["JwtSettings:ExpiryMinutes"] = "1440"
            };
        }

        protected virtual void SeedTestData(AppDbContext context)
        {
            // Seed common test data
            if (!context.AppointmentTypes.Any())
            {
                context.AppointmentTypes.AddRange(
                    new AppointmentType { Id = Guid.NewGuid(), Name = "Meeting", Color = "#0000FF" },
                    new AppointmentType { Id = Guid.NewGuid(), Name = "Call", Color = "#00FF00" },
                    new AppointmentType { Id = Guid.NewGuid(), Name = "Task", Color = "#FF0000" }
                );
                context.SaveChanges();
            }

            // Add test users if needed
            if (!context.Users.Any())
            {
                context.Users.Add(new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "testuser@example.com",
                    FirstName = "Test",
                    LastName = "User",
                    PasswordHash = "hashed_password", // This would be properly hashed in real scenario
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                context.SaveChanges();
            }
        }
    }
}
