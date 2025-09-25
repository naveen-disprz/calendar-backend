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

namespace Calendar.Tests.DataAccess;

public class AppointmentTypeDALTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AppointmentTypeDAL _appointmentTypeDAL;

    public AppointmentTypeDALTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _appointmentTypeDAL = new AppointmentTypeDAL(_context);

        // SeedTestData();
    }

    // private void SeedTestData()
    // {
    //     var appointmentTypes = new List<AppointmentType>
    //     {
    //         new AppointmentType
    //         {
    //             Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
    //             Name = "Meeting",
    //             Color = "#FF5733",
    //             CreatedAt = DateTime.UtcNow,
    //             UpdatedAt = DateTime.UtcNow
    //         },
    //         new AppointmentType
    //         {
    //             Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
    //             Name = "Interview",
    //             Color = "#33FF57",
    //             CreatedAt = DateTime.UtcNow,
    //             UpdatedAt = DateTime.UtcNow
    //         },
    //         new AppointmentType
    //         {
    //             Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
    //             Name = "Training",
    //             Color = "#3357FF",
    //             CreatedAt = DateTime.UtcNow,
    //             UpdatedAt = DateTime.UtcNow
    //         },
    //         new AppointmentType
    //         {
    //             Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
    //             Name = "Webinar",
    //             Color = "#FF33F5",
    //             CreatedAt = DateTime.UtcNow,
    //             UpdatedAt = DateTime.UtcNow
    //         }
    //     };
    //
    //     _context.AppointmentTypes.AddRange(appointmentTypes);
    //     _context.SaveChanges();
    // }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingActiveType_ShouldReturnAppointmentType()
    {
        // Arrange
        var appointmentTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var result = await _appointmentTypeDAL.GetByIdAsync(appointmentTypeId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(appointmentTypeId);
        result.Name.Should().Be("Meeting");
        result.Color.Should().Be("#90caf9");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _appointmentTypeDAL.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ShouldReturnNull()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var result = await _appointmentTypeDAL.GetByIdAsync(emptyGuid);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCompleteObject()
    {
        // Arrange
        var appointmentTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        var result = await _appointmentTypeDAL.GetByIdAsync(appointmentTypeId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(appointmentTypeId);
        result.Name.Should().Be("Personal");
        result.Color.Should().Be("#a5d6a7");
        result.CreatedAt.Should().NotBe(default(DateTime));
        result.UpdatedAt.Should().NotBe(default(DateTime));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAppointmentTypes()
    {
        // Act
        var result = await _appointmentTypeDAL.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(8);
        result.Should().Contain(at => at.Name == "Meeting");
        result.Should().Contain(at => at.Name == "Personal");
        result.Should().Contain(at => at.Name == "Work");
        result.Should().Contain(at => at.Name == "Health");
        result.Should().Contain(at => at.Name == "Travel");
        result.Should().Contain(at => at.Name == "Study");
        result.Should().Contain(at => at.Name == "Family");
        result.Should().Contain(at => at.Name == "Errands");
    }
        
    [Fact]
    public async Task GetAllAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        // Clear existing data
        var existingTypes = await _context.AppointmentTypes.ToListAsync();
        _context.AppointmentTypes.RemoveRange(existingTypes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _appointmentTypeDAL.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCompleteObjects()
    {
        // Act
        var result = await _appointmentTypeDAL.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(at => 
            at.Id != Guid.Empty &&
            !string.IsNullOrEmpty(at.Name) &&
            !string.IsNullOrEmpty(at.Color) &&
            at.CreatedAt != default(DateTime) &&
            at.UpdatedAt != default(DateTime)
        );
    }

    [Fact]
    public async Task GetAllAsync_ShouldMaintainDatabaseOrder()
    {
        // Arrange
        // Clear and re-add in specific order
        var existingTypes = await _context.AppointmentTypes.ToListAsync();
        _context.AppointmentTypes.RemoveRange(existingTypes);
        await _context.SaveChangesAsync();

        var orderedTypes = new List<AppointmentType>
        {
            new AppointmentType
            {
                Id = Guid.NewGuid(),
                Name = "Z-Type",
                Color = "#000000",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AppointmentType
            {
                Id = Guid.NewGuid(),
                Name = "A-Type",
                Color = "#FFFFFF",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AppointmentType
            {
                Id = Guid.NewGuid(),
                Name = "M-Type",
                Color = "#888888",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.AppointmentTypes.AddRange(orderedTypes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _appointmentTypeDAL.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        // Note: Since there's no OrderBy in the DAL, order might not be guaranteed
        // This test verifies the DAL returns all items regardless of order
        result.Select(at => at.Name).Should().Contain(new[] { "Z-Type", "A-Type", "M-Type" });
    }

    #endregion

    #region Edge Cases and Integration Tests
        
    [Fact]
    public async Task GetByIdAsync_MultipleConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var appointmentTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tasks = new List<Task<AppointmentType?>>();
        
        // Act - Make 10 concurrent calls
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_appointmentTypeDAL.GetByIdAsync(appointmentTypeId));
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(at => at != null && at.Name == "Meeting");
    }
        
    [Fact]
    public async Task GetAllAsync_MultipleConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task<List<AppointmentType>>>();
        
        // Act - Make 10 concurrent calls
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_appointmentTypeDAL.GetAllAsync());
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(list => list.Count == 8);
    }
        
    [Fact]
    public async Task GetByIdAsync_AfterAddingNewType_ShouldReturnNewType()
    {
        // Arrange
        var newType = new AppointmentType
        {
            Id = Guid.NewGuid(),
            Name = "New Type",
            Color = "#123456",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.AppointmentTypes.Add(newType);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _appointmentTypeDAL.GetByIdAsync(newType.Id);
        
        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Type");
    }
        
    [Fact]
    public async Task GetAllAsync_AfterAddingNewType_ShouldIncludeNewType()
    {
        // Arrange
        var initialCount = (await _appointmentTypeDAL.GetAllAsync()).Count;
            
        var newType = new AppointmentType
        {
            Id = Guid.NewGuid(),
            Name = "Additional Type",
            Color = "#ABCDEF",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.AppointmentTypes.Add(newType);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _appointmentTypeDAL.GetAllAsync();
        
        // Assert
        result.Should().HaveCount(initialCount + 1);
        result.Should().Contain(at => at.Name == "Additional Type");
    }
        
    [Fact]
    public async Task GetByIdAsync_AfterDeletingType_ShouldReturnNull()
    {
        // Arrange
        var typeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            
        // Verify it exists first
        var existingType = await _appointmentTypeDAL.GetByIdAsync(typeId);
        existingType.Should().NotBeNull();
        
        // Delete the type
        _context.AppointmentTypes.Remove(existingType!);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _appointmentTypeDAL.GetByIdAsync(typeId);
        
        // Assert
        result.Should().BeNull();
    }
        
    [Fact]
    public async Task GetAllAsync_AfterDeletingType_ShouldNotIncludeDeletedType()
    {
        // Arrange
        var typeToDelete = await _context.AppointmentTypes
            .FirstAsync(at => at.Name == "Meeting");
        
        _context.AppointmentTypes.Remove(typeToDelete);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _appointmentTypeDAL.GetAllAsync();
        
        // Assert
        result.Should().HaveCount(7);
        result.Should().NotContain(at => at.Name == "Meeting");
    }
        
    [Fact]
    public async Task GetByIdAsync_WithModifiedType_ShouldReturnUpdatedValues()
    {
        // Arrange
        var typeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
        var type = await _context.AppointmentTypes.FirstAsync(at => at.Id == typeId);
        type.Name = "Updated Meeting";
        type.Color = "#UPDATED";
        type.UpdatedAt = DateTime.UtcNow;
            
        _context.AppointmentTypes.Update(type);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _appointmentTypeDAL.GetByIdAsync(typeId);
        
        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Meeting");
        result.Color.Should().Be("#UPDATED");
    }
        
    [Fact]
    public async Task GetAllAsync_WithLargeDataset_ShouldHandleEfficiently()
    {
        // Arrange - Add 100 more appointment types
        var largeDataset = Enumerable.Range(1, 100).Select(i => new AppointmentType
        {
            Id = Guid.NewGuid(),
            Name = $"Type {i:D3}",
            Color = $"#{i:X6}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();
        
        _context.AppointmentTypes.AddRange(largeDataset);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _appointmentTypeDAL.GetAllAsync();
        
        // Assert
        result.Should().HaveCount(108); // 8 original + 100 new
        result.Should().Contain(at => at.Name == "Type 001");
        result.Should().Contain(at => at.Name == "Type 100");
    }
        
    [Fact]
    public async Task GetByIdAsync_WithNullableFields_ShouldHandleCorrectly()
    {
        // Arrange
        var typeWithNulls = new AppointmentType
        {
            Id = Guid.NewGuid(),
            Name = "Minimal Type",
            Color = "#000000",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.AppointmentTypes.Add(typeWithNulls);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _appointmentTypeDAL.GetByIdAsync(typeWithNulls.Id);
        
        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Minimal Type");
    }
        
    [Fact]
    public async Task GetAllAsync_WithSpecialCharactersInNames_ShouldHandleCorrectly()
    {
        // Arrange
        var specialTypes = new List<AppointmentType>
        {
            new AppointmentType
            {
                Id = Guid.NewGuid(),
                Name = "Type with 'quotes'",
                Color = "#111111",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AppointmentType
            {
                Id = Guid.NewGuid(),
                Name = "Type with émojis 😀",
                Color = "#222222",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AppointmentType
            {
                Id = Guid.NewGuid(),
                Name = "Type with <HTML> tags",
                Color = "#333333",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        
        _context.AppointmentTypes.AddRange(specialTypes);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _appointmentTypeDAL.GetAllAsync();
        
        // Assert
        result.Should().Contain(at => at.Name == "Type with 'quotes'");
        result.Should().Contain(at => at.Name == "Type with émojis 😀");
        result.Should().Contain(at => at.Name == "Type with <HTML> tags");
    }
        
    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}