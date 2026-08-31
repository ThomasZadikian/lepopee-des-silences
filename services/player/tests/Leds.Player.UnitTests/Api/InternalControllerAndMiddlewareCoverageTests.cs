using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Leds.Player.Api.Controllers;
using Leds.Player.Api.Middleware;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Application.Internal.ConsumeRunOutcome;
using Leds.Player.Application.Players;
using Leds.Player.Application.Players.ClaimNpcOffering;
using Leds.Player.Application.Players.GrantNpcReputationMilestone;
using Leds.Player.Application.Players.UnlockSkill;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Leds.Player.UnitTests.Api;

public sealed class InternalControllerAndMiddlewareCoverageTests
{
    private static readonly Guid PlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CharacterId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task InternalPlayersController_ShouldCoverOptionalRequestBranches()
    {
        var sender = new Mock<ISender>();
        var profile = PlayerProfileDto.FromDomain(PlayerProfile.Create("Player", DateTimeOffset.UtcNow));
        sender.Setup(x => x.Send(It.IsAny<UnlockSkillCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        sender.Setup(x => x.Send(It.IsAny<ClaimNpcOfferingCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        sender.Setup(x => x.Send(It.IsAny<GrantNpcReputationMilestoneCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        var controller = new InternalPlayersController(sender.Object);

        (await controller.UnlockSkill(PlayerId, CharacterId, "skill.a", null, CancellationToken.None))
            .Result.Should().BeOfType<OkObjectResult>();
        (await controller.UnlockSkill(PlayerId, CharacterId, "skill.a", new UnlockSkillRequest("event"), CancellationToken.None))
            .Result.Should().BeOfType<OkObjectResult>();
        (await controller.ClaimNpcOffering(PlayerId, "npc", "offer", null, CancellationToken.None))
            .Result.Should().BeOfType<OkObjectResult>();
        (await controller.ClaimNpcOffering(PlayerId, "npc", "offer", new SourceRunRequest(Guid.NewGuid()), CancellationToken.None))
            .Result.Should().BeOfType<OkObjectResult>();
        (await controller.GrantReputationMilestone(PlayerId, "npc", "milestone", null, CancellationToken.None))
            .Result.Should().BeOfType<OkObjectResult>();
        (await controller.GrantReputationMilestone(PlayerId, "npc", "milestone", new SourceRunRequest(Guid.NewGuid()), CancellationToken.None))
            .Result.Should().BeOfType<OkObjectResult>();

        sender.Verify(x => x.Send(
            It.Is<UnlockSkillCommand>(c => c.Source == "devtools"), It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(x => x.Send(
            It.Is<UnlockSkillCommand>(c => c.Source == "event"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InternalProjectionsController_ShouldMapProcessedFlag(bool processed)
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<ConsumeRunOutcomeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsumeRunOutcomeResponse(processed, processed ? null : "rejected"));
        var controller = new InternalProjectionsController(sender.Object);
        var request = new RunOutcomeRequest(Guid.NewGuid(), "run.outcome", "1", DateTime.UtcNow, "{}");

        var result = await controller.ConsumeRunOutcome(request, CancellationToken.None);

        if (processed)
            result.Result.Should().BeOfType<OkObjectResult>();
        else
            result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("validation", 400)]
    [InlineData("domain", 400)]
    [InlineData("unauthorized", 401)]
    [InlineData("not-found", 404)]
    [InlineData("conflict", 409)]
    [InlineData("unexpected", 500)]
    public async Task ExceptionMiddleware_ShouldMapEveryExceptionFamily(string scenario, int expectedStatus)
    {
        Exception exception = scenario switch
        {
            "validation" => new ValidationException([new ValidationFailure("field", "invalid")]),
            "domain" => new DomainException("domain failure"),
            "unauthorized" => new UnauthorizedException("unauthorized"),
            "not-found" => new NotFoundException("Thing", Guid.NewGuid()),
            "conflict" => new ConflictException("conflict"),
            _ => new InvalidOperationException("unexpected")
        };
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            Mock.Of<ILogger<ExceptionHandlingMiddleware>>());
        var context = new DefaultHttpContext();
        context.Request.Path = "/coverage";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(expectedStatus);
        context.Response.ContentType.Should().Be("application/problem+json");
        context.Response.Body.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExceptionMiddleware_ShouldPassThroughSuccessfulRequest()
    {
        var called = false;
        var middleware = new ExceptionHandlingMiddleware(
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            Mock.Of<ILogger<ExceptionHandlingMiddleware>>());
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        called.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
