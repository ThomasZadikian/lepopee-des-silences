using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class SkillRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly SkillRepository _repository;

    public SkillRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new SkillRepository(_context);
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
        var skills = new List<Skill>
        {
            new Skill { Id = 2, Name = "Heal", EffectType = Constants.EffectHeal, MPCost = 10 },
            new Skill { Id = 1, Name = "Fireball", EffectType = Constants.EffectDamage, MPCost = 15 }
        };
        await _context.Skills.AddRangeAsync(skills);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        var skill = new Skill { Id = 5, Name = "Ice Spike", EffectType = Constants.EffectDamage, MPCost = 20 };
        await _context.Skills.AddAsync(skill);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Name.Should().Be("Ice Spike");
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
        var newSkill = new Skill { Id = 10, Name = "Shield", EffectType = Constants.EffectBuff, MPCost = 25 };

        await _repository.AddAsync(newSkill);

        var dbRecord = await _context.Skills.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.Name.Should().Be("Shield");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesRecordAndSavesChanges()
    {
        var skill = new Skill { Id = 1, Name = "OldName", EffectType = Constants.EffectDamage, MPCost = 10 };
        await _context.Skills.AddAsync(skill);
        await _context.SaveChangesAsync();

        _context.Entry(skill).State = EntityState.Detached;

        var updatedSkill = new Skill { Id = 1, Name = "NewName", EffectType = Constants.EffectDamage, MPCost = 10 };
        await _repository.UpdateAsync(updatedSkill);

        var dbRecord = await _context.Skills.FindAsync(1);
        dbRecord.Should().NotBeNull();
        dbRecord!.Name.Should().Be("NewName");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        var skill = new Skill { Id = 1, Name = "ToDelete", EffectType = Constants.EffectDamage, MPCost = 10 };
        await _context.Skills.AddAsync(skill);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.Skills.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        var skill = new Skill { Id = 1, Name = "ToKeep", EffectType = Constants.EffectDamage, MPCost = 10 };
        await _context.Skills.AddAsync(skill);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var remainingCount = await _context.Skills.CountAsync();
        remainingCount.Should().Be(1);
    }
}