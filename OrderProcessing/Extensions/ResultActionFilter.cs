using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Order.Processing.Domain.Common;

namespace Order.Processing.Api.Extensions;

public sealed class ResultActionFilter : IAsyncAlwaysRunResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: Result result })
        {
            context.Result = Translate(result);
        }

        return next();
    }

    private static IActionResult Translate(Result result)
    {
        if (result.IsFailure)
        {
            return new HttpResultActionResult(result.ToProblemDetails());
        }

        return result.ValueOrDefault is { } value
            ? new OkObjectResult(value)
            : new NoContentResult();
    }

    private sealed class HttpResultActionResult : IActionResult
    {
        private readonly IResult _result;

        public HttpResultActionResult(IResult result)
        {
            _result = result;
        }

        public Task ExecuteResultAsync(ActionContext context) => _result.ExecuteAsync(context.HttpContext);
    }
}
