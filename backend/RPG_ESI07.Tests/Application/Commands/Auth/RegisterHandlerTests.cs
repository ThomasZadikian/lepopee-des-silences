using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Auth;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.Auth;

public class RegisterHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly Mock<ITokenService> _mockToken;
    private readonly RegisterHandler _handler;
    private readonly Mock<IPlayerProfileRepository> _mockPlayerRepo;

    public RegisterHandlerTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockHasher = new Mock<IPasswordHasher>();
        _mockToken = new Mock<ITokenService>(); 
        _mockPlayerRepo = new Mock<IPlayerProfileRepository>();

        _handler = new RegisterHandler(
            _mockUserRepo.Object,
            _mockHasher.Object,
            _mockToken.Object,
            _mockPlayerRepo.Object);
    }

    [Fact]
    public async Task Handle_NewUser_CreatesUserAndPlayerProfile_ReturnsMfaSetupRequired()
    {
        _mockUserRepo.Setup(r => r.UsernameExistsAsync("newuser")).ReturnsAsync(false);
        _mockHasher.Setup(h => h.HashPassword("Pass123!")).Returns("hashed");
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(new User { Id = 1 });
        _mockToken.Setup(t => t.GenerateMfaToken(It.IsAny<User>())).Returns("mfa-setup-token");
        _mockPlayerRepo.Setup(p => p.AddAsync(It.IsAny<PlayerProfile>())).ReturnsAsync(new PlayerProfile { UserId = 1 });

        var result = await _handler.Handle(
            new RegisterCommand("newuser", "test@test.com", "Pass123!"),
            CancellationToken.None);

        _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);

        _mockPlayerRepo.Verify(p => p.AddAsync(It.Is<PlayerProfile>(
            prof => prof.UserId == 1 && prof.CharacterName == "newuser" && prof.Level == 1
        )), Times.Once);

        result.success.Should().BeTrue();
        result.requiresMfaSetup.Should().BeTrue();
        result.requiresMfa.Should().BeFalse();
        result.token.Should().Be("mfa-setup-token");
    }

    [Fact]
    public async Task Handle_DuplicateUser_Fails()
    {
        _mockUserRepo.Setup(r => r.UsernameExistsAsync("existing")).ReturnsAsync(true);

        var result = await _handler.Handle(
            new RegisterCommand("existing", "x@x.com", "Pass123!"),
            CancellationToken.None);

        result.success.Should().BeFalse();
        result.token.Should().BeNull();
    }
}