using FluentAssertions;
using FluentValidation;
using Leds.Player.Application.Common.Behaviors;
using MediatR;

namespace Leds.Player.UnitTests.Application.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidatorsExist()
    {
        var validators = Array.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse());
        };

        await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationPasses()
    {
        var validator = new PassingValidator();
        var validators = new IValidator<TestRequest>[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse());
        };

        await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        var validator = new FailingValidator("Validation failed");
        var validators = new IValidator<TestRequest>[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse());

        var act = () => behavior.Handle(new TestRequest("invalid"), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldNotCallNext_WhenValidationFails()
    {
        var validator = new FailingValidator("Validation failed");
        var validators = new IValidator<TestRequest>[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse());
        };

        try
        {
            await behavior.Handle(new TestRequest("invalid"), next, CancellationToken.None);
        }
        catch (ValidationException)
        {
        }

        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldRunAllValidators_WhenMultipleValidatorsExist()
    {
        var validator1 = new TrackingValidator();
        var validator2 = new TrackingValidator();
        var validators = new IValidator<TestRequest>[] { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse());

        await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        validator1.ValidateCalled.Should().BeTrue();
        validator2.ValidateCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenAnyValidatorFails()
    {
        var passingValidator = new PassingValidator();
        var failingValidator = new FailingValidator("Validation failed");
        var validators = new IValidator<TestRequest>[] { passingValidator, failingValidator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse());

        var act = () => behavior.Handle(new TestRequest("invalid"), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldCollectAllErrors_WhenMultipleValidatorsFail()
    {
        var validator1 = new FailingValidator("Error from validator 1");
        var validator2 = new FailingValidator("Error from validator 2");
        var validators = new IValidator<TestRequest>[] { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse());

        var act = () => behavior.Handle(new TestRequest("invalid"), next, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCountGreaterThan(1);
        exception.Which.Errors.Select(e => e.ErrorMessage).Should().Contain("Error from validator 1");
        exception.Which.Errors.Select(e => e.ErrorMessage).Should().Contain("Error from validator 2");
    }

    [Fact]
    public async Task Handle_ShouldReturnNextResult_WhenValidationPasses()
    {
        var validator = new PassingValidator();
        var validators = new IValidator<TestRequest>[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var expectedResponse = new TestResponse();

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        var result = await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ShouldPassRequestToValidators()
    {
        var validator = new CapturingValidator();
        var validators = new IValidator<TestRequest>[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("test-value");

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse());

        await behavior.Handle(request, next, CancellationToken.None);

        validator.CapturedValue.Should().Be("test-value");
    }

    [Fact]
    public async Task Handle_ShouldIncludeErrorMessages_WhenValidationFails()
    {
        var validator = new FailingValidator("Specific error message");
        var validators = new IValidator<TestRequest>[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse());

        var act = () => behavior.Handle(new TestRequest("invalid"), next, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle();
        exception.Which.Errors.First().ErrorMessage.Should().Be("Specific error message");
    }
}

internal sealed record TestRequest(string Value) : IRequest<TestResponse>;
internal sealed record TestResponse;

internal sealed class PassingValidator : AbstractValidator<TestRequest>
{
    public PassingValidator()
    {
        RuleFor(x => x.Value).NotEmpty();
    }
}

internal sealed class FailingValidator : AbstractValidator<TestRequest>
{
    private readonly string _errorMessage;

    public FailingValidator(string errorMessage)
    {
        _errorMessage = errorMessage;
        RuleFor(x => x.Value).Must(_ => false).WithMessage(_errorMessage);
    }
}

internal sealed class TrackingValidator : AbstractValidator<TestRequest>
{
    public bool ValidateCalled { get; private set; }

    public TrackingValidator()
    {
        RuleFor(x => x.Value).Must(_ =>
        {
            ValidateCalled = true;
            return true;
        });
    }
}

internal sealed class CapturingValidator : AbstractValidator<TestRequest>
{
    public string? CapturedValue { get; private set; }

    public CapturingValidator()
    {
        RuleFor(x => x.Value).Must(value =>
        {
            CapturedValue = value;
            return true;
        });
    }
}
