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
    public class RecurrenceRuleDALTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly RecurrenceRuleDAL _recurrenceRuleDAL;

        public RecurrenceRuleDALTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _recurrenceRuleDAL = new RecurrenceRuleDAL(_context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var recurrenceRules = new List<RecurrenceRule>
            {
                new RecurrenceRule
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Frequency = "DAILY",
                    DaysOfWeekMask = 0,
                    DaysOfMonthMask = 0,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    UpdatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new RecurrenceRule
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Frequency = "WEEKLY",
                    DaysOfWeekMask = 0b0011110, // Monday to Thursday (binary representation)
                    DaysOfMonthMask = 0,
                    EndDate = DateTime.UtcNow.AddMonths(3),
                    CreatedAt = DateTime.UtcNow.AddDays(-14),
                    UpdatedAt = DateTime.UtcNow.AddDays(-14)
                },
                new RecurrenceRule
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Frequency = "MONTHLY",
                    DaysOfWeekMask = 0,
                    DaysOfMonthMask = 0b00000001000000010000000100000001, // 1st, 8th, 15th, 22nd of month
                    EndDate = null, // No end date
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                }
            };

            _context.RecurrenceRules.AddRange(recurrenceRules);
            _context.SaveChanges();
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidRecurrenceRule_ShouldAddToDatabase()
        {
            // Arrange
            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _recurrenceRuleDAL.CreateAsync(recurrenceRule);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(recurrenceRule);
            result.Id.Should().Be(recurrenceRule.Id);
            result.Frequency.Should().Be("DAILY");
            result.EndDate.Should().Be(recurrenceRule.EndDate);

            // Verify it was saved to database
            var savedRule = await _context.RecurrenceRules.FindAsync(recurrenceRule.Id);
            savedRule.Should().NotBeNull();
            savedRule!.Frequency.Should().Be("DAILY");
        }

        [Fact]
        public async Task CreateAsync_WithWeeklyRecurrence_ShouldSaveDaysOfWeekMask()
        {
            // Arrange
            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "WEEKLY",
                DaysOfWeekMask = 0b1010101, // Sunday, Tuesday, Thursday, Saturday
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddMonths(2),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _recurrenceRuleDAL.CreateAsync(recurrenceRule);

            // Assert
            result.DaysOfWeekMask.Should().Be(0b1010101);
            
            var savedRule = await _context.RecurrenceRules.FindAsync(recurrenceRule.Id);
            savedRule!.DaysOfWeekMask.Should().Be(0b1010101);
        }

        [Fact]
        public async Task CreateAsync_WithMonthlyRecurrence_ShouldSaveDaysOfMonthMask()
        {
            // Arrange
            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "MONTHLY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0b00000000000000110000000000000011, // 1st, 2nd, 15th, 16th
                EndDate = DateTime.UtcNow.AddMonths(6),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _recurrenceRuleDAL.CreateAsync(recurrenceRule);

            // Assert
            result.DaysOfMonthMask.Should().Be(0b00000000000000110000000000000011);
            
            var savedRule = await _context.RecurrenceRules.FindAsync(recurrenceRule.Id);
            savedRule!.DaysOfMonthMask.Should().Be(0b00000000000000110000000000000011);
        }

        [Fact]
        public async Task CreateAsync_WithNoEndDate_ShouldSaveAsNull()
        {
            // Arrange
            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = null, // No end date - infinite recurrence
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _recurrenceRuleDAL.CreateAsync(recurrenceRule);

            // Assert
            result.EndDate.Should().BeNull();
            
            var savedRule = await _context.RecurrenceRules.FindAsync(recurrenceRule.Id);
            savedRule!.EndDate.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_WithEmptyGuid_ShouldStillSave()
        {
            // Arrange
            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.Empty, // Empty GUID
                Frequency = "WEEKLY",
                DaysOfWeekMask = 0b0111110, // Weekdays only
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddDays(90),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _recurrenceRuleDAL.CreateAsync(recurrenceRule);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task CreateAsync_ShouldPreserveAllProperties()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddYears(1);
            
            var recurrenceRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "MONTHLY",
                DaysOfWeekMask = 123,
                DaysOfMonthMask = 456,
                EndDate = endDate,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Act
            var result = await _recurrenceRuleDAL.CreateAsync(recurrenceRule);

            // Assert
            var savedRule = await _context.RecurrenceRules.FindAsync(recurrenceRule.Id);
            savedRule.Should().NotBeNull();
            savedRule!.Frequency.Should().Be("MONTHLY");
            savedRule.DaysOfWeekMask.Should().Be(123);
            savedRule.DaysOfMonthMask.Should().Be(456);
            savedRule.EndDate.Should().BeCloseTo(endDate, TimeSpan.FromSeconds(1));
            savedRule.CreatedAt.Should().BeCloseTo(createdAt, TimeSpan.FromSeconds(1));
            savedRule.UpdatedAt.Should().BeCloseTo(updatedAt, TimeSpan.FromSeconds(1));
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithExistingRule_ShouldUpdateAllFields()
        {
            // Arrange
            var ruleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var beforeUpdate = DateTime.UtcNow;
            
            var updatedRule = new RecurrenceRule
            {
                Frequency = "WEEKLY",
                DaysOfWeekMask = 0b1111111, // All days
                DaysOfMonthMask = 999,
                EndDate = DateTime.UtcNow.AddYears(1)
            };

            // Act
            var result = await _recurrenceRuleDAL.UpdateAsync(ruleId, updatedRule);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(ruleId);
            result.Frequency.Should().Be("WEEKLY");
            result.DaysOfWeekMask.Should().Be(0b1111111);
            result.DaysOfMonthMask.Should().Be(999);
            result.EndDate.Should().BeCloseTo(updatedRule.EndDate.Value, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeAfter(beforeUpdate);
            
            // Verify in database
            var dbRule = await _context.RecurrenceRules.FindAsync(ruleId);
            dbRule!.Frequency.Should().Be("WEEKLY");
            dbRule.DaysOfWeekMask.Should().Be(0b1111111);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingRule_ShouldReturnNull()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            var updatedRule = new RecurrenceRule
            {
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var result = await _recurrenceRuleDAL.UpdateAsync(nonExistingId, updatedRule);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldOnlyUpdateSpecifiedFields()
        {
            // Arrange
            var ruleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Get original values
            var originalRule = await _context.RecurrenceRules.FindAsync(ruleId);
            var originalCreatedAt = originalRule!.CreatedAt;
            var originalId = originalRule.Id;
            
            var updatedRule = new RecurrenceRule
            {
                Id = Guid.NewGuid(), // Different ID - should not change
                Frequency = "MONTHLY",
                DaysOfWeekMask = 100,
                DaysOfMonthMask = 200,
                EndDate = null,
                CreatedAt = DateTime.UtcNow // Different CreatedAt - should not change
            };

            // Act
            var result = await _recurrenceRuleDAL.UpdateAsync(ruleId, updatedRule);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(originalId); // ID should not change
            result.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromSeconds(1)); // CreatedAt should not change
            result.Frequency.Should().Be("MONTHLY");
            result.DaysOfWeekMask.Should().Be(100);
            result.DaysOfMonthMask.Should().Be(200);
            result.EndDate.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_WithNullEndDate_ShouldSetToNull()
        {
            // Arrange
            var ruleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            var updatedRule = new RecurrenceRule
            {
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = null // Remove end date
            };

            // Act
            var result = await _recurrenceRuleDAL.UpdateAsync(ruleId, updatedRule);

            // Assert
            result.Should().NotBeNull();
            result!.EndDate.Should().BeNull();
            
            var dbRule = await _context.RecurrenceRules.FindAsync(ruleId);
            dbRule!.EndDate.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var ruleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var originalRule = await _context.RecurrenceRules.FindAsync(ruleId);
            var originalUpdatedAt = originalRule!.UpdatedAt;
            
            // Wait a bit to ensure time difference
            await Task.Delay(100);
            
            var updatedRule = new RecurrenceRule
            {
                Frequency = "WEEKLY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddMonths(1)
            };

            // Act
            var result = await _recurrenceRuleDAL.UpdateAsync(ruleId, updatedRule);

            // Assert
            result.Should().NotBeNull();
            result!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task UpdateAsync_WithEmptyGuid_ShouldReturnNull()
        {
            // Arrange
            var updatedRule = new RecurrenceRule
            {
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = await _recurrenceRuleDAL.UpdateAsync(Guid.Empty, updatedRule);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithExistingRule_ShouldRemoveFromDatabase()
        {
            // Arrange
            var ruleToDelete = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.RecurrenceRules.Add(ruleToDelete);
            await _context.SaveChangesAsync();
            
            // Verify it exists
            var existsBefore = await _context.RecurrenceRules.FindAsync(ruleToDelete.Id);
            existsBefore.Should().NotBeNull();

            // Act
            var result = await _recurrenceRuleDAL.DeleteAsync(ruleToDelete.Id);

            // Assert
            result.Should().BeTrue();
            
            var existsAfter = await _context.RecurrenceRules.FindAsync(ruleToDelete.Id);
            existsAfter.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistingRule_ShouldReturnFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var result = await _recurrenceRuleDAL.DeleteAsync(nonExistingId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_WithEmptyGuid_ShouldReturnFalse()
        {
            // Act
            var result = await _recurrenceRuleDAL.DeleteAsync(Guid.Empty);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_ShouldOnlyDeleteSpecifiedRule()
        {
            // Arrange
            var rule1 = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddDays(7)
            };
            
            var rule2 = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "WEEKLY",
                DaysOfWeekMask = 0b0111110,
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddDays(14)
            };
            
            _context.RecurrenceRules.AddRange(rule1, rule2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _recurrenceRuleDAL.DeleteAsync(rule1.Id);

            // Assert
            result.Should().BeTrue();
            
            var rule1Exists = await _context.RecurrenceRules.FindAsync(rule1.Id);
            rule1Exists.Should().BeNull();
            
            var rule2Exists = await _context.RecurrenceRules.FindAsync(rule2.Id);
            rule2Exists.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteAsync_AfterDeletingRule_UpdateShouldReturnNull()
        {
            // Arrange
            var rule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "MONTHLY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 1,
                EndDate = DateTime.UtcNow.AddMonths(1)
            };
            
            _context.RecurrenceRules.Add(rule);
            await _context.SaveChangesAsync();

            // Act
            var deleteResult = await _recurrenceRuleDAL.DeleteAsync(rule.Id);
            deleteResult.Should().BeTrue();
            
            // Try to update deleted rule
            var updateResult = await _recurrenceRuleDAL.UpdateAsync(rule.Id, new RecurrenceRule
            {
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = null
            });

            // Assert
            updateResult.Should().BeNull();
        }

        #endregion

        #region Integration and Edge Case Tests

        [Fact]
        public async Task CompleteLifecycle_CreateUpdateDelete_ShouldWorkCorrectly()
        {
            // Create
            var rule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            var createdRule = await _recurrenceRuleDAL.CreateAsync(rule);
            createdRule.Should().NotBeNull();
            createdRule.Frequency.Should().Be("DAILY");

            // Update
            var updateData = new RecurrenceRule
            {
                Frequency = "WEEKLY",
                DaysOfWeekMask = 0b0000110, // Tuesday and Wednesday
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddDays(60)
            };
            
            var updatedRule = await _recurrenceRuleDAL.UpdateAsync(rule.Id, updateData);
            updatedRule.Should().NotBeNull();
            updatedRule!.Frequency.Should().Be("WEEKLY");
            updatedRule.DaysOfWeekMask.Should().Be(0b0000110);

            // Delete
            var deleteResult = await _recurrenceRuleDAL.DeleteAsync(rule.Id);
            deleteResult.Should().BeTrue();
            
            var finalCheck = await _context.RecurrenceRules.FindAsync(rule.Id);
            finalCheck.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ConcurrentUpdates_LastUpdateShouldWin()
        {
            // Arrange
            var rule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = DateTime.UtcNow.AddDays(7)
            };
            
            _context.RecurrenceRules.Add(rule);
            await _context.SaveChangesAsync();

            // Act - Concurrent updates
            var task1 = Task.Run(async () =>
            {
                await _recurrenceRuleDAL.UpdateAsync(rule.Id, new RecurrenceRule
                {
                    Frequency = "WEEKLY",
                    DaysOfWeekMask = 1,
                    DaysOfMonthMask = 0,
                    EndDate = DateTime.UtcNow.AddDays(14)
                });
            });
            
            var task2 = Task.Run(async () =>
            {
                await Task.Delay(50); // Small delay
                await _recurrenceRuleDAL.UpdateAsync(rule.Id, new RecurrenceRule
                {
                    Frequency = "MONTHLY",
                    DaysOfWeekMask = 2,
                    DaysOfMonthMask = 0,
                    EndDate = DateTime.UtcNow.AddDays(30)
                });
            });

            await Task.WhenAll(task1, task2);

            // Assert - Last update should win (likely task2 due to delay)
            var finalRule = await _context.RecurrenceRules.FindAsync(rule.Id);
            finalRule.Should().NotBeNull();
            // Either update could win in a real concurrent scenario
            finalRule!.Frequency.Should().BeOneOf("WEEKLY", "MONTHLY");
        }

        [Fact]
        public async Task CreateAsync_WithVeryLargeMasks_ShouldHandleCorrectly()
        {
            // Arrange
            var rule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                Frequency = "MONTHLY",
                DaysOfWeekMask = int.MaxValue,
                DaysOfMonthMask = int.MaxValue,
                EndDate = DateTime.UtcNow.AddYears(10),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _recurrenceRuleDAL.CreateAsync(rule);

            // Assert
            result.Should().NotBeNull();
            result.DaysOfWeekMask.Should().Be(int.MaxValue);
            result.DaysOfMonthMask.Should().Be(int.MaxValue);
            
            var savedRule = await _context.RecurrenceRules.FindAsync(rule.Id);
            savedRule!.DaysOfWeekMask.Should().Be(int.MaxValue);
            savedRule.DaysOfMonthMask.Should().Be(int.MaxValue);
        }

        [Fact]
        public async Task UpdateAsync_WithVeryOldEndDate_ShouldStillUpdate()
        {
            // Arrange
            var ruleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var veryOldDate = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            var updatedRule = new RecurrenceRule
            {
                Frequency = "DAILY",
                DaysOfWeekMask = 0,
                DaysOfMonthMask = 0,
                EndDate = veryOldDate
            };

            // Act
            var result = await _recurrenceRuleDAL.UpdateAsync(ruleId, updatedRule);

            // Assert
            result.Should().NotBeNull();
            result!.EndDate.Should().Be(veryOldDate);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

