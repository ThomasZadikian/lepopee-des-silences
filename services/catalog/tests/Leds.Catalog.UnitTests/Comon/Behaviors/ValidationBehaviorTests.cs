using FluentAssertions;
using FluentValidation;
using Leds.Catalog.Application.Abstractions.Messaging;
using Leds.Catalog.Application.Common.Behaviors;
using MediatR;

namespace Leds.Catalog.UnitTests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidatorExists()
    {
        var behavior = new ValidationBehavior<TestQuery, TestResponse>(
            Array.Empty<IValidator<TestQuery>>());

        var wasCalled = false;

        RequestHandlerDelegate<TestResponse> next = cancellationToken =>
        {
            wasCalled = true;
            return Task.FromResult(new TestResponse("ok"));
        };

        var response = await behavior.Handle(
            new TestQuery("valid-key"),
            next,
            CancellationToken.None);

        response.Value.Should().Be("ok");
        wasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationSucceeds()
    {
        var behavior = new ValidationBehavior<TestQuery, TestResponse>(
            new IValidator<TestQuery>[]
            {
                new TestQueryValidator()
            });

        var wasCalled = false;

        RequestHandlerDelegate<TestResponse> next = cancellationToken =>
        {
            wasCalled = true;
            return Task.FromResult(new TestResponse("ok"));
        };

        var response = await behavior.Handle(
            new TestQuery("valid-key"),
            next,
            CancellationToken.None);

        response.Value.Should().Be("ok");
        wasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        var behavior = new ValidationBehavior<TestQuery, TestResponse>(
            new IValidator<TestQuery>[]
            {
                new TestQueryValidator()
            });

        RequestHandlerDelegate<TestResponse> next = _ =>
            Task.FromResult(new TestResponse("should-not-be-called"));

        var act = async () => await behavior.Handle(
            new TestQuery(string.Empty),
            next,
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .Where(exception =>
                exception.Errors.Any(error => error.ErrorMessage == "Key is required."));
    }

    private sealed record TestQuery(string Key) : IQuery<TestResponse>;

    private sealed record TestResponse(string Value);

    private sealed class TestQueryValidator : AbstractValidator<TestQuery>
    {
        public TestQueryValidator()
        {
            RuleFor(query => query.Key)
                .NotEmpty()
                .WithMessage("Key is required.");
        }
    }
}