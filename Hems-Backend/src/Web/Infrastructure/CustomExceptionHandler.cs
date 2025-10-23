using Hems_Backend.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace Hems_Backend.Web.Infrastructure;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;

    public CustomExceptionHandler()
    {
        // Register known exception types and handlers.
        _exceptionHandlers = new()
            {
                { typeof(FluentValidation.ValidationException), HandleFluentValidationException },
                { typeof(Hems_Backend.Application.Common.Exceptions.ValidationException), HandleApplicationValidationException },
                { typeof(Hems_Backend.Application.Common.Exceptions.NotFoundException), HandleNotFoundException },
                { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
                { typeof(ForbiddenAccessException), HandleForbiddenAccessException },
            };
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true;
        }

        return false;
    }

    private async Task HandleFluentValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (FluentValidation.ValidationException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var modelState = new Dictionary<string, string[]>();
        foreach (var error in exception.Errors)
        {
            if (!modelState.ContainsKey(error.PropertyName))
                modelState[error.PropertyName] = new string[] { };
            
            var errors = modelState[error.PropertyName].ToList();
            errors.Add(error.ErrorMessage);
            modelState[error.PropertyName] = errors.ToArray();
        }

        await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(modelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private async Task HandleApplicationValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (Hems_Backend.Application.Common.Exceptions.ValidationException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new
        {
            statusCode = StatusCodes.Status400BadRequest,
            message = "Validation failed",
            type = "ValidationError",
            timestamp = DateTime.UtcNow,
            errors = exception.Errors?.Select(e => new
            {
                propertyName = e.Key,
                errorMessages = e.Value
            }).ToArray() ?? new object[0]
        });
    }

    private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
    {
        var exception = (Hems_Backend.Application.Common.Exceptions.NotFoundException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Detail = exception.Message
        });
    }

    private async Task HandleUnauthorizedAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        });
    }

    private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        });
    }
}
