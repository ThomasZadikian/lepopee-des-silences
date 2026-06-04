using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.Npcs;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllNpcsHandlerTests
{
    private readonly Mock<INpcRepository> _mockRepo;
    private readonly GetAllNpcsHandler _handler;

    public GetAllNpcsHandlerTests()
    {
        _mockRepo = new Mock<INpcRepository>(MockBehavior.Strict);
        _handler = new GetAllNpcsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var items = new List<Npc>
        {
            new() { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral },
            new() { Id = 2, Name = "Zara",   Zone = "BossFinal",  Type = Constants.NpcTypeMerchant }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = await _handler.Handle(new GetAllNpcsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyResponse()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Npc>());

        var result = await _handler.Handle(new GetAllNpcsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}

public class GetNpcByIdHandlerTests
{
    private readonly Mock<INpcRepository> _mockRepo;
    private readonly GetNpcByIdHandler _handler;

    public GetNpcByIdHandlerTests()
    {
        _mockRepo = new Mock<INpcRepository>(MockBehavior.Strict);
        _handler = new GetNpcByIdHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsNpc_WhenExists()
    {
        var npc = new Npc { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(npc);

        var result = await _handler.Handle(new GetNpcByIdQuery(1), CancellationToken.None);

        result.Item.Should().NotBeNull();
        result.Item!.Id.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenNotExists()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Npc?)null);

        var result = await _handler.Handle(new GetNpcByIdQuery(999), CancellationToken.None);

        result.Item.Should().BeNull();
    }
}

public class GetNpcsByZoneHandlerTests
{
    private readonly Mock<INpcRepository> _mockRepo;
    private readonly GetNpcsByZoneHandler _handler;

    public GetNpcsByZoneHandlerTests()
    {
        _mockRepo = new Mock<INpcRepository>(MockBehavior.Strict);
        _handler = new GetNpcsByZoneHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsNpcsInZone()
    {
        var items = new List<Npc>
        {
            new() { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral }
        };
        _mockRepo.Setup(r => r.GetByZoneAsync("Dev Island")).ReturnsAsync(items);

        var result = await _handler.Handle(new GetNpcsByZoneQuery("Dev Island"), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Zone.Should().Be("Dev Island");
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenZoneHasNoNpcs()
    {
        _mockRepo.Setup(r => r.GetByZoneAsync("Unknown")).ReturnsAsync(new List<Npc>());

        var result = await _handler.Handle(new GetNpcsByZoneQuery("Unknown"), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}

public class GetNpcsByTypeHandlerTests
{
    private readonly Mock<INpcRepository> _mockRepo;
    private readonly GetNpcsByTypeHandler _handler;

    public GetNpcsByTypeHandlerTests()
    {
        _mockRepo = new Mock<INpcRepository>(MockBehavior.Strict);
        _handler = new GetNpcsByTypeHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsNpcsOfType()
    {
        var items = new List<Npc>
        {
            new() { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeMerchant }
        };
        _mockRepo.Setup(r => r.GetByTypeAsync(Constants.NpcTypeMerchant)).ReturnsAsync(items);

        var result = await _handler.Handle(new GetNpcsByTypeQuery(Constants.NpcTypeMerchant), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Type.Should().Be(Constants.NpcTypeMerchant);
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoNpcsOfType()
    {
        _mockRepo.Setup(r => r.GetByTypeAsync(Constants.NpcTypeAlly)).ReturnsAsync(new List<Npc>());

        var result = await _handler.Handle(new GetNpcsByTypeQuery(Constants.NpcTypeAlly), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}