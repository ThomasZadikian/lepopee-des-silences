using FluentAssertions;
using RPG_ESI07.Infrastructure.Services;

namespace RPG_ESI07.Tests.Infrastructure.Services;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher;

    public Argon2PasswordHasherTests()
    {
        _hasher = new Argon2PasswordHasher();
    }

    [Fact]
    public void HashPassword_ThrowsArgumentException_WhenPasswordIsEmpty()
    {
        Action act = () => _hasher.HashPassword("");
        act.Should().Throw<ArgumentException>().WithParameterName("password");
    }

    [Fact]
    public void HashPassword_ReturnsValidBase64String()
    {
        var password = "StrongPassword123!";
        var hash = _hasher.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        Action act = () => Convert.FromBase64String(hash);
        act.Should().NotThrow();
    }

    [Fact]
    public void HashPassword_GeneratesDifferentHashes_ForSamePassword()
    {
        var password = "StrongPassword123!";

        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        hash1.Should().NotBe(hash2); 
    }

    [Theory]
    [InlineData(null, "somehash")]
    [InlineData("", "somehash")]
    [InlineData("password", null)]
    [InlineData("password", "")]
    public void VerifyPassword_ReturnsFalse_WhenInputsAreInvalid(string? password, string? hash)
    {
        var result = _hasher.VerifyPassword(password!, hash!);
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ReturnsTrue_WhenPasswordIsCorrect()
    {
        var password = "CorrectPassword123!";
        var hash = _hasher.HashPassword(password);

        var result = _hasher.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenPasswordIsIncorrect()
    {
        var hash = _hasher.HashPassword("CorrectPassword123!");

        var result = _hasher.VerifyPassword("WrongPassword!", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenHashIsMalformed()
    {
        var malformedHash = Convert.ToBase64String(new byte[10]); // Taille incorrecte

        var result = _hasher.VerifyPassword("password", malformedHash);

        result.Should().BeFalse();
    }
}