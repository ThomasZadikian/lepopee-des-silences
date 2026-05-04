using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Queries.Enemies;
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
            new() { Id = 1, Name = "Goblin", Type = "basic",  MaxHP = 50  },
            new() { Id = 2, Name = "Dragon", Type = "boss",   MaxHP = 900 },
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
        var enemy = new Enemy { Id = 1, Name = "Goblin", Type = "basic", MaxHP = 50 };
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
        var enemies = new List<Enemy> { new() { Id = 2, Name = "Dragon", Type = "boss", MaxHP = 900 } };
        var expected = new GetEnemiesByTypeResponse(enemies);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEnemiesByTypeQuery>(q => q.Type == "boss"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetByType("boss");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }
}