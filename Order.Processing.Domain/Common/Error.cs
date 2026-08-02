using System;
using System.Collections.Generic;
using System.Text;

namespace Order.Processing.Domain.Common;

public record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Validation(string message) => new("validation_error", message, ErrorType.Validation);
    public static Error Unauthorized(string message) => new("unauthorized", message, ErrorType.Failure);
    public static Error Internal(string message) => new("internal_error", message, ErrorType.Failure);
}

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict
}
