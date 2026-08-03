using FluentValidation;
using FluentValidation.Results;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Abstractions.Behaviors;

internal static class ValidationDecorator
{
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
            ValidationFailure[] validationFailures = await ValidateAsync(command, cancellationToken);

            if (validationFailures.Length == 0)
            {
                return await _innerHandler.HandleAsync(command, cancellationToken);
            }

            return Result.Failure(CreateValidationError(validationFailures));
        }

        private async Task<ValidationFailure[]> ValidateAsync(TCommand command, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                return [];
            }

            var context = new ValidationContext<TCommand>(command);

            ValidationResult[] validationResults = await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

            return validationResults
                .Where(validationResult => !validationResult.IsValid)
                .SelectMany(validationResult => validationResult.Errors)
                .ToArray();
        }

        private static ValidationError CreateValidationError(ValidationFailure[] validationFailures) =>
            new(validationFailures
                .Select(failure => new Error(failure.ErrorCode, failure.ErrorMessage, ErrorType.Validation))
                .ToArray());
    }
}
