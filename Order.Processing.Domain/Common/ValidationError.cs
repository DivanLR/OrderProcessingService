namespace Order.Processing.Domain.Common;

public sealed record ValidationError : Error
{
    public ValidationError(Error[] errors)
        : base("validation_error", "One or more validation errors occurred.", ErrorType.Validation)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }
}
