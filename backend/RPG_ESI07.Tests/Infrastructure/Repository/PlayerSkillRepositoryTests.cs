using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class PlayerSkillRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PlayerSkillRepository _repository;

    public PlayerSkillRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new PlayerSkillRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Skill BuildSkill(int id) => new()
    {
        Id = id,
        Name = $"Skill {id}",
        Description = "Test",
        MPCost = 10,
        BaseDamage = 20,
    };

    private static PlayerSkill BuildPlayerSkill(int id, int skillId, int playerId = 1) => new()
    {
        Id = id,
        SkillId = skillId,
        PlayerId = playerId
    };

    [Fact]
    public async Task GetAllAsync_ReturnsAllPlayerSkills()
    {
        var skill = BuildSkill(1);
        _context.Skills.Add(skill);
        _context.Set<PlayerSkill>().Add(BuildPlayerSkill(1, 1));
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPlayerSkill_WhenExists()
    {
        _context.Skills.Add(BuildSkill(1));
        _context.Set<PlayerSkill>().Add(BuildPlayerSkill(1, 1));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByIdAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_PersistsPlayerSkill()
    {
        _context.Skills.Add(BuildSkill(1));
        await _context.SaveChangesAsync();

        await _repository.AddAsync(BuildPlayerSkill(1, 1));

        var count = await _context.Set<PlayerSkill>().CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_SavesChanges()
    {
        _context.Skills.Add(BuildSkill(1));
        var ps = BuildPlayerSkill(1, 1);
        _context.Set<PlayerSkill>().Add(ps);
        await _context.SaveChangesAsync();

        ps.PlayerId = 2;
        await _repository.UpdateAsync(ps);

        var updated = await _context.Set<PlayerSkill>().FindAsync(1);
        updated!.PlayerId.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPlayerSkill_WhenExists()
    {
        _context.Skills.Add(BuildSkill(1));
        _context.Set<PlayerSkill>().Add(BuildPlayerSkill(1, 1));
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var count = await _context.Set<PlayerSkill>().CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotThrow_WhenNotExists()
    {
        var act = async () => await _repository.DeleteAsync(999);
        await act.Should().NotThrowAsync();
    }
}