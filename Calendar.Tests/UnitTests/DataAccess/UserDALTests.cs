using Xunit;
using FluentAssertions;
using Calendar.DataAccess;
using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calendar.Tests.DataAccess
{
    public class UserDALTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserDAL _userDAL;

        public UserDALTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _userDAL = new UserDAL(_context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region GetByEmailAsync Tests

        [Fact]
        public async Task GetByEmailAsync_WithExistingActiveUser_ShouldReturnUser()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.GetByEmailAsync("test@example.com");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            result.FirstName.Should().Be(user.FirstName);
        }

        [Fact]
        public async Task GetByEmailAsync_WithDifferentCase_ShouldReturnUser()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act - Test with different cases
            var result1 = await _userDAL.GetByEmailAsync("TEST@EXAMPLE.COM");
            var result2 = await _userDAL.GetByEmailAsync("Test@Example.Com");

            // Assert
            result1.Should().NotBeNull();
            result1.Id.Should().Be(user.Id);
            result2.Should().NotBeNull();
            result2.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByEmailAsync_WithInactiveUser_ShouldReturnNull()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "inactive@example.com",
                FirstName = "Inactive",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = false, // Inactive
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.GetByEmailAsync("inactive@example.com");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_WithNonExistentEmail_ShouldReturnNull()
        {
            // Act
            var result = await _userDAL.GetByEmailAsync("nonexistent@example.com");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldAddUserToDatabase()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                PasswordHash = "hash123",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _userDAL.CreateAsync(user);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(user);

            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
            savedUser.Email.Should().Be(user.Email);
            savedUser.PasswordHash.Should().Be(user.PasswordHash);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreatedUser()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "return@example.com",
                FirstName = "Return",
                LastName = "Test",
                PasswordHash = "hash456",
                IsActive = false, // Even inactive users can be created
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _userDAL.CreateAsync(user);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            result.IsActive.Should().BeFalse();
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_WithExistingActiveUser_ShouldReturnTrue()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "exists@example.com",
                FirstName = "Exists",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.ExistsAsync("exists@example.com");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithDifferentCase_ShouldReturnTrue()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "casetest@example.com",
                FirstName = "Case",
                LastName = "Test",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _userDAL.ExistsAsync("CASETEST@EXAMPLE.COM");
            var result2 = await _userDAL.ExistsAsync("CaseTest@Example.COM");

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithInactiveUser_ShouldReturnFalse()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "inactive@example.com",
                FirstName = "Inactive",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.ExistsAsync("inactive@example.com");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistentUser_ShouldReturnFalse()
        {
            // Act
            var result = await _userDAL.ExistsAsync("nonexistent@example.com");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithExistingActiveUser_ShouldReturnUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "byid@example.com",
                FirstName = "ById",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(userId);
            result.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task GetByIdAsync_WithInactiveUser_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "inactive@example.com",
                FirstName = "Inactive",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.GetByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Act
            var result = await _userDAL.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetByIdsAsync Tests

        [Fact]
        public async Task GetByIdsAsync_WithMultipleIds_ShouldReturnOnlyActiveUsers()
        {
            // Arrange
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            var user3Id = Guid.NewGuid();

            var users = new List<User>
            {
                new User
                {
                    Id = user1Id,
                    Email = "user1@example.com",
                    FirstName = "User1",
                    LastName = "Test",
                    PasswordHash = "hash1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = user2Id,
                    Email = "user2@example.com",
                    FirstName = "User2",
                    LastName = "Test",
                    PasswordHash = "hash2",
                    IsActive = false, // Inactive
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = user3Id,
                    Email = "user3@example.com",
                    FirstName = "User3",
                    LastName = "Test",
                    PasswordHash = "hash3",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var requestedIds = new List<Guid> { user1Id, user2Id, user3Id };

            // Act
            var result = await _userDAL.GetByIdsAsync(requestedIds);

            // Assert
            result.Should().HaveCount(2); // Only active users
            result.Should().Contain(u => u.Id == user1Id);
            result.Should().Contain(u => u.Id == user3Id);
            result.Should().NotContain(u => u.Id == user2Id); // Inactive user excluded
        }

        [Fact]
        public async Task GetByIdsAsync_WithEmptyList_ShouldReturnEmptyList()
        {
            // Act
            var result = await _userDAL.GetByIdsAsync(new List<Guid>());

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_WithNonExistentIds_ShouldReturnEmptyList()
        {
            // Arrange
            var nonExistentIds = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Act
            var result = await _userDAL.GetByIdsAsync(nonExistentIds);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_WithMixOfExistingAndNonExistingIds_ShouldReturnOnlyExisting()
        {
            // Arrange
            var existingUserId = Guid.NewGuid();
            var user = new User
            {
                Id = existingUserId,
                Email = "existing@example.com",
                FirstName = "Existing",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var requestedIds = new List<Guid>
            {
                existingUserId,
                Guid.NewGuid(), // Non-existent
                Guid.NewGuid()  // Non-existent
            };

            // Act
            var result = await _userDAL.GetByIdsAsync(requestedIds);

            // Assert
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(existingUserId);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_WithNoExclude_ShouldReturnAllActiveUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    FirstName = "User1",
                    LastName = "Test",
                    PasswordHash = "hash1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user2@example.com",
                    FirstName = "User2",
                    LastName = "Test",
                    PasswordHash = "hash2",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "inactive@example.com",
                    FirstName = "Inactive",
                    LastName = "User",
                    PasswordHash = "hash3",
                    IsActive = false, // Inactive
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.GetAllAsync(null);

            // Assert
            result.Should().HaveCount(2); // Only active users
            result.Should().Contain(u => u.Email == "user1@example.com");
            result.Should().Contain(u => u.Email == "user2@example.com");
            result.Should().NotContain(u => u.Email == "inactive@example.com");
        }

        [Fact]
        public async Task GetAllAsync_WithExcludeUserId_ShouldExcludeSpecifiedUser()
        {
            // Arrange
            var excludeUserId = Guid.NewGuid();
            var users = new List<User>
            {
                new User
                {
                    Id = excludeUserId,
                    Email = "exclude@example.com",
                    FirstName = "Exclude",
                    LastName = "User",
                    PasswordHash = "hash1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "include1@example.com",
                    FirstName = "Include1",
                    LastName = "User",
                    PasswordHash = "hash2",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "include2@example.com",
                    FirstName = "Include2",
                    LastName = "User",
                    PasswordHash = "hash3",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.GetAllAsync(excludeUserId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().NotContain(u => u.Id == excludeUserId);
            result.Should().Contain(u => u.Email == "include1@example.com");
            result.Should().Contain(u => u.Email == "include2@example.com");
        }

        [Fact]
        public async Task GetAllAsync_WithNoUsers_ShouldReturnEmptyList()
        {
            // Act
            var result = await _userDAL.GetAllAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ExcludeNonExistentUser_ShouldReturnAllActiveUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    FirstName = "User1",
                    LastName = "Test",
                    PasswordHash = "hash1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "user2@example.com",
                    FirstName = "User2",
                    LastName = "Test",
                    PasswordHash = "hash2",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var nonExistentUserId = Guid.NewGuid();

            // Act
            var result = await _userDAL.GetAllAsync(nonExistentUserId);

            // Assert
            result.Should().HaveCount(2); // All active users since excluded ID doesn't exist
        }

        #endregion
    }
}
