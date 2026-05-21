using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.Enemies;
using RPG_ESI07.Application.Queries.Enemies;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Tests.Controllers;

public class EnemiesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly EnemiesController _controller;

    public EnemiesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new EnemiesController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithEnemyList()
    {
        var enemies = new List<Enemy>
        {
            new() { Id = 1, Name = "Goblin", Type = Constants.EnemyTypeBasic,  MaxHP = 50  },
            new() { Id = 2, Name = "Dragon", Type = Constants.EnemyTypeBoss,   MaxHP = 900 },
        };
        var expected = new GetAllEnemiesResponse(enemies);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllEnemiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WhenNoEnemies()
    {
        var expected = new GetAllEnemiesResponse(new List<Enemy>());

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllEnemiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_CallsMediatorOnce()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllEnemiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAllEnemiesResponse(new List<Enemy>()));

        await _controller.GetAll();

        _mediatorMock.Verify(
            m => m.Send(It.IsAny<GetAllEnemiesQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task GetById_ReturnsOk_WhenEnemyExists()
    {
        var enemy = new Enemy { Id = 1, Name = "Goblin", Type = Constants.EnemyTypeBasic, MaxHP = 50 };
        var expected = new GetEnemyByIdResponse(enemy);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEnemyByIdQuery>(q => q.Id == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetById(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetByType_ReturnsOk_WithFilteredEnemies()
    {
        var enemies = new List<Enemy> { new() { Id = 2, Name = "Dragon", Type = Constants.EnemyTypeBoss, MaxHP = 900 } };
        var expected = new GetEnemiesByTypeResponse(enemies);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEnemiesByTypeQuery>(q => q.Type == Constants.EnemyTypeBoss), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetByType(Constants.EnemyTypeBoss);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }
    [Fact]
    public async Task Create_ReturnsOk_WhenSuccessful()
    {
        var command = new CreateEnemyCommand(
            "Goblin", Constants.EnemyTypeBasic, 50, 8, 3, 12,
            1.0f, 1.0f, 10, 5, "Un gobelin", 5f,
            null, null, null, Constants.EnemyStateRepos);
        var expected = new CreateEnemyResponse(1, "Enemy created successfully");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

        var result = await _controller.Create(command);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenSuccessful()
    {
        var command = new UpdateEnemyCommand(
            1, "Goblin Modifié", Constants.EnemyTypeBasic, 60, 10, 3, 12,
            1.0f, 1.0f, 12, 6, "Un gobelin plus fort", 5f,
            null, null, null, Constants.EnemyStateRepos);
        var expected = new UpdateEnemyResponse(true, "Enemy updated successfully");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

        var result = await _controller.Update(1, command);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenSuccessful()
    {
        var expected = new DeleteEnemyResponse(true, "Enemy deleted successfully");

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteEnemyCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

        var result = await _controller.Delete(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetScaled_ReturnsOk_WhenEnemyExists()
    {
        var dto = new ScaledEnemyDto(
            1, "Goblin", Constants.EnemyTypeBasic,
            225, 36, 9, 63,
            1.0f, 1.0f, 63, 32,
            null, null, null, null,
            Constants.EnemyStateRepos, 4.5f);
        var expected = new GetScaledEnemyResponse(dto);

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetScaledEnemyQuery>(q => q.EnemyId == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetScaled(1, 5, 0);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetScaled_ReturnsNotFound_WhenEnemyNotExists()
    {
        _mediatorMock.Setup(m => m.Send(
                It.Is<GetScaledEnemyQuery>(q => q.EnemyId == 999),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetScaledEnemyResponse(null));

        var result = await _controller.GetScaled(999, 5, 0);

        result.Should().BeOfType<NotFoundResult>();
    }
}