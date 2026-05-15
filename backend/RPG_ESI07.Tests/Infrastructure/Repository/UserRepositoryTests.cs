using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repositories;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
    }
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsUser_WhenExists()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = System.Text.Encoding.UTF8.GetBytes("test@example.com"),
            PasswordHash = "hash"
        };
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.GetByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByUsernameAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UsernameExistsAsync_ReturnsTrue_WhenExists()
    {
        // Arrange
        var user = new User
        {
            Username = "existing",
            Email = Array.Empty<byte>(),
            PasswordHash = "hash"
        };
        await _repository.AddAsync(user);

        // Act
        var exists = await _repository.UsernameExistsAsync("existing");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UsernameExistsAsync_ReturnsFalse_WhenNotExists()
    {
        // Act
        var exists = await _repository.UsernameExistsAsync("nonexistent");

        // Assert
        exists.Should().BeFalse();
    }
}