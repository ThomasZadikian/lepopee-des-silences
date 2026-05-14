using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Tests.Infrastructure.Data;

public class AppDbContextTests : IDisposable
{
    private readonly AppDbContext _context;

    public AppDbContextTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void ModelConfiguration_UserEntity_AppliesUniqueUsernameIndex()
    {
        var entityType = _context.Model.FindEntityType(typeof(User));

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Any(p => p.Name == nameof(User.Username)));

        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void ModelConfiguration_EnemyEntity_AppliesCheckConstraints()
    {
        var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
        var entityType = designTimeModel.FindEntityType(typeof(Enemy));

        var checkConstraints = entityType!.GetCheckConstraints();

        checkConstraints.Should().ContainSingle(c => c.Name == "CK_Enemy_Type");
        checkConstraints.Should().ContainSingle(c => c.Name == "CK_Enemy_Stats");
        checkConstraints.Should().ContainSingle(c => c.Name == "CK_Enemy_Resistance");
    }
    [Fact]
    public void ModelConfiguration_Relationships_UserToPlayerProfile_IsCascadeDelete()
    {
        var entityType = _context.Model.FindEntityType(typeof(PlayerProfile));
        var foreignKey = entityType!.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));

        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        foreignKey.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void DbSets_AreAccessible()
    {
        _context.Users.Should().NotBeNull();
        _context.PlayerProfiles.Should().NotBeNull();
        _context.Enemies.Should().NotBeNull();
        _context.Items.Should().NotBeNull();
        _context.Skills.Should().NotBeNull();
    }
}