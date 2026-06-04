using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class AuditLogRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuditLogRepository _repository;

    public AuditLogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new AuditLogRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRecords_OrderedById()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new AuditLog { Id = 2, EventType = "TYPE_B", Timestamp = DateTime.UtcNow },
            new AuditLog { Id = 1, EventType = "TYPE_A", Timestamp = DateTime.UtcNow }
        };
        await _context.AuditLogs.AddRangeAsync(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1); // Vérification du tri ascendant
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        // Arrange
        var log = new AuditLog { Id = 5, EventType = "LOGIN", Timestamp = DateTime.UtcNow };
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(5);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.EventType.Should().Be("LOGIN");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_InsertsRecordAndSavesChanges()
    {
        // Arrange
        var newLog = new AuditLog { Id = 10, EventType = "LOGOUT", Timestamp = DateTime.UtcNow };

        // Act
        await _repository.AddAsync(newLog);

        // Assert
        var dbRecord = await _context.AuditLogs.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.EventType.Should().Be("LOGOUT");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        // Arrange
        var log = new AuditLog { Id = 1, EventType = "DELETE", Timestamp = DateTime.UtcNow };
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(1);

        // Assert
        var dbRecord = await _context.AuditLogs.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        // Arrange
        var log = new AuditLog { Id = 1, EventType = "KEEP", Timestamp = DateTime.UtcNow };
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(999);

        // Assert
        var remainingCount = await _context.AuditLogs.CountAsync();
        remainingCount.Should().Be(1);
    }
}