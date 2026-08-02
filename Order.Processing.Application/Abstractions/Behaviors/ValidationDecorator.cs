using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using FluentValidation.Results;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Abstractions.Behaviors;

internal static class ValidationDecorator
{
    internal sealed class CommandHandler<TCommand, TValue> : ICommandHandler<TCommand, Result<TValue>>
        where TCommand : ICommand<Result<TValue>>
    {
        private readonly ICommandHandler<TCommand, Result<TValue>> _innerHandler;
        private readonly IEnumerable<IValidator<TCommand>> _validators;

        public CommandHandler(
            ICommandHandler<TCommand, Result<TValue>> innerHandler,
            IEnumerable<IValidator<TCommand>> validators)
        {
            _innerHandler = innerHandler;
            _validators = validators;
        }

        public async Task<Result<TValue>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            ValidationFailure[] validationFailures = await ValidateAsync(command, _validators, cancellationToken);

            if (validationFailures.Length == 0)
            {
                return await _innerHandler.HandleAsync(command, cancellationToken);
            }

            return Result.Failure<TValue>(CreateValidationError(validationFailures));
        }
    }

    internal sealed class CommandBaseHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> _innerHandler;
        private readonly IEnumerable<IValidator<TCommand>> _validators;

        public CommandBaseHandler(
            ICommandHandler<TCommand> innerHandler,
            IEnumerable<IValidator<TCommand>> validators)
        {
            _innerHandler = innerHandler;
            _validators = validators;
        }

        public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            ValidationFailure[] validationFailures = await ValidateAsync(command, _validators, cancellationToken);

            if (validationFailures.Length == 0)
            {
                return await _innerHandler.HandleAsync(command, cancellationToken);
            }

            return Result.Failure(CreateValidationError(validationFailures));
        }
    }

    private static async Task<ValidationFailure[]> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return [];
        }

        var context = new ValidationContext<TCommand>(command);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        ValidationFailure[] validationFailures = validationResults
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .ToArray();

        return validationFailures;
    }

    private static ValidationError CreateValidationError(ValidationFailure[] validationFailures) =>
        new(validationFailures
            .Select(failure => new Error(failure.ErrorCode, failure.ErrorMessage, ErrorType.Validation))
            .ToArray());
}
