using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class UserConsentRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserConsentRepository _repository;

    public UserConsentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new UserConsentRepository(_context);
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
        var consents = new List<UserConsent>
        {
            new UserConsent { Id = 2, UserId = 2, AnalyticsConsent = false, MarketingConsent = true },
            new UserConsent { Id = 1, UserId = 1, AnalyticsConsent = true, MarketingConsent = false }
        };
        await _context.UserConsents.AddRangeAsync(consents);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        var consent = new UserConsent { Id = 5, UserId = 10, AnalyticsConsent = true, MarketingConsent = true };
        await _context.UserConsents.AddAsync(consent);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.UserId.Should().Be(10);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_InsertsRecordAndSavesChanges()
    {
        var newConsent = new UserConsent { Id = 10, UserId = 5, AnalyticsConsent = false, MarketingConsent = false };

        await _repository.AddAsync(newConsent);

        var dbRecord = await _context.UserConsents.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.UserId.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesRecordAndSavesChanges()
    {
        var consent = new UserConsent { Id = 1, UserId = 1, AnalyticsConsent = false, MarketingConsent = false };
        await _context.UserConsents.AddAsync(consent);
        await _context.SaveChangesAsync();

        _context.Entry(consent).State = EntityState.Detached;

        var updatedConsent = new UserConsent { Id = 1, UserId = 1, AnalyticsConsent = true, MarketingConsent = true };
        await _repository.UpdateAsync(updatedConsent);

        var dbRecord = await _context.UserConsents.FindAsync(1);
        dbRecord.Should().NotBeNull();
        dbRecord!.AnalyticsConsent.Should().BeTrue();
        dbRecord.MarketingConsent.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        var consent = new UserConsent { Id = 1, UserId = 1, AnalyticsConsent = true, MarketingConsent = true };
        await _context.UserConsents.AddAsync(consent);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.UserConsents.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        var consent = new UserConsent { Id = 1, UserId = 1, AnalyticsConsent = true, MarketingConsent = true };
        await _context.UserConsents.AddAsync(consent);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var remainingCount = await _context.UserConsents.CountAsync();
        remainingCount.Should().Be(1);
    }
}