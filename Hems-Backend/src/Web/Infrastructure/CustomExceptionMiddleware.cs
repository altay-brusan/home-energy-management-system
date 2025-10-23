using Hems_Backend.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace Hems_Backend.Web.Infrastructure;

public class CustomExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomExceptionMiddleware> _logger;

    public CustomExceptionMiddleware(RequestDelegate next, ILogger<CustomExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        switch (exception)
        {
            case FluentValidation.ValidationException:
                await HandleFluentValidationException(context, exception);
                break;
            case Hems_Backend.Application.Common.Exceptions.ValidationException:
                await HandleApplicationValidationException(context, exception);
                break;
            case Hems_Backend.Application.Common.Exceptions.NotFoundException:
                await HandleNotFoundException(context, exception);
                break;
            case UnauthorizedAccessException:
                await HandleUnauthorizedAccessException(context, exception);
                break;
            case ForbiddenAccessException:
                await HandleForbiddenAccessException(context, exception);
                break;
            default:
                await HandleGenericException(context, exception);
                break;
        }
    }

    private static async Task HandleFluentValidationException(HttpContext context, Exception ex)
    {
        var exception = (FluentValidation.ValidationException)ex;
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var modelState = new Dictionary<string, string[]>();
        foreach (var error in exception.Errors)
        {
            if (!modelState.ContainsKey(error.PropertyName))
                modelState[error.PropertyName] = new string[] { };
            
            var errors = modelState[error.PropertyName].ToList();
            errors.Add(error.ErrorMessage);
            modelState[error.PropertyName] = errors.ToArray();
        }

        await context.Response.WriteAsJsonAsync(new ValidationProblemDetails(modelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private static async Task HandleApplicationValidationException(HttpContext context, Exception ex)
    {
        var exception = (Hems_Backend.Application.Common.Exceptions.ValidationException)ex;
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        await context.Response.WriteAsJsonAsync(new
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

    private static async Task HandleNotFoundException(HttpContext context, Exception ex)
    {
        var exception = (Hems_Backend.Application.Common.Exceptions.NotFoundException)ex;
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        await context.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Detail = exception.Message
        });
    }

    private static async Task HandleUnauthorizedAccessException(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        });
    }

    private static async Task HandleForbiddenAccessException(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        });
    }

    private static async Task HandleGenericException(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            statusCode = StatusCodes.Status500InternalServerError,
            message = "An error occurred while processing your request",
            type = "InternalError",
            timestamp = DateTime.UtcNow
        });
    }
}
