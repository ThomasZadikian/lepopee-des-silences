using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Services;

namespace RPG_ESI07.Tests.Infrastructure.Services;

public class CompanionMarkovServiceTests
{
    [Fact]
    public void ComputeNextState_ReturnsValidState()
    {
        var current = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastCombatAt = DateTime.UtcNow
        };

        var validStates = new[]
        {
            Constants.CompanionStateRepos,
            Constants.CompanionStateJeu,
            Constants.CompanionStateManger,
            Constants.CompanionStateExcite,
            Constants.CompanionStateTriste,
            Constants.CompanionStateEndormi
        };

        // On lance 100 fois pour vérifier que le résultat est toujours un état valide
        for (int i = 0; i < 100; i++)
        {
            var next = CompanionMarkovService.ComputeNextState(current, 0, 0);
            validStates.Should().Contain(next);
        }
    }

    [Fact]
    public void ComputeNextState_FavorsExcite_WhenManyVictories()
    {
        var current = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastCombatAt = DateTime.UtcNow
        };

        // Avec 50 victoires, EXCITE doit être fréquent
        var results = Enumerable.Range(0, 200)
            .Select(_ => CompanionMarkovService.ComputeNextState(current, 50, 0))
            .ToList();

        var exciteCount = results.Count(r => r == Constants.CompanionStateExcite);
        exciteCount.Should().BeGreaterThan(30); // Au moins 15% des cas
    }

    [Fact]
    public void ComputeNextState_FavorsTriste_WhenManyDefeats()
    {
        var current = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastCombatAt = DateTime.UtcNow
        };

        var results = Enumerable.Range(0, 200)
            .Select(_ => CompanionMarkovService.ComputeNextState(current, 0, 20))
            .ToList();

        var tristeCount = results.Count(r => r == Constants.CompanionStateTriste);
        tristeCount.Should().BeGreaterThan(30);
    }

    [Fact]
    public void ComputeNextState_FavorsEndormi_WhenNoCombatFor2Hours()
    {
        var current = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastCombatAt = DateTime.UtcNow.AddHours(-3)
        };

        var results = Enumerable.Range(0, 200)
            .Select(_ => CompanionMarkovService.ComputeNextState(current, 0, 0))
            .ToList();

        var endormiCount = results.Count(r => r == Constants.CompanionStateEndormi);
        endormiCount.Should().BeGreaterThan(20);
    }

    [Fact]
    public void ComputeNextState_HandlesUnknownState_WithoutCrash()
    {
        var current = new CompanionState
        {
            CurrentState = "UNKNOWN_STATE",
            LastCombatAt = DateTime.UtcNow
        };

        var act = () => CompanionMarkovService.ComputeNextState(current, 0, 0);
        act.Should().NotThrow();
    }
}

public class CompanionControllerTests
{
    private readonly Mock<ICompanionRepository> _mockRepo;
    private readonly CompanionController _controller;

    public CompanionControllerTests()
    {
        _mockRepo = new Mock<ICompanionRepository>(MockBehavior.Strict);
        _controller = new CompanionController(_mockRepo.Object);
    }

    [Fact]
    public async Task GetState_ReturnsOk_WithCurrentState()
    {
        var state = new CompanionState
        {
            CurrentState = Constants.CompanionStateExcite,
            LastUpdated = DateTime.UtcNow,
            VictoriesToday = 15,
            DefeatesToday = 2
        };
        _mockRepo.Setup(r => r.GetOrCreateAsync()).ReturnsAsync(state);

        var result = await _controller.GetState();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetState_CreatesState_WhenNoneExists()
    {
        var state = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastUpdated = DateTime.UtcNow,
            VictoriesToday = 0,
            DefeatesToday = 0
        };
        _mockRepo.Setup(r => r.GetOrCreateAsync()).ReturnsAsync(state);

        var result = await _controller.GetState();

        result.Should().BeOfType<OkObjectResult>();
        _mockRepo.Verify(r => r.GetOrCreateAsync(), Times.Once);
    }
}