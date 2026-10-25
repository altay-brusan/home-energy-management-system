using System.Net;
using System.Text.Json;
using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Models;

namespace Hems_Backend.Web.Infrastructure;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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

        var response = exception switch
        {
            // Our new structured AppException
            AppException appEx => new ApiErrorResponse
            {
                StatusCode = (int)appEx.StatusCode,
                Code = appEx.Code,
                Message = appEx.Message,
                Params = appEx.Params
            },

            // Existing exceptions - map to message codes
            ForbiddenAccessException => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
                Code = MessageCodes.AUTH_PERMISSION_DENIED,
                Message = string.IsNullOrEmpty(exception.Message)
                    ? MessageDefaults.GetMessage(MessageCodes.AUTH_PERMISSION_DENIED)
                    : exception.Message
            },

            Hems_Backend.Application.Common.Exceptions.NotFoundException notFoundEx => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Code = MessageCodes.RECORD_NOT_FOUND,
                Message = notFoundEx.Message
            },

            Ardalis.GuardClauses.NotFoundException => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Code = MessageCodes.RECORD_NOT_FOUND,
                Message = exception.Message
            },

            Hems_Backend.Application.Common.Exceptions.ValidationException validationEx => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Code = MessageCodes.VALIDATION_FAILED,
                Message = validationEx.Message,
                Params = validationEx.Errors?.ToDictionary(e => e.Key, e => (object)e.Value)
            },

            FluentValidation.ValidationException fluentEx => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Code = MessageCodes.VALIDATION_FAILED,
                Message = "Validation failed",
                Params = fluentEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => (object)g.Select(e => e.ErrorMessage).ToArray())
            },

            InvalidOperationException => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Code = MessageCodes.VALIDATION_FAILED,
                Message = exception.Message
            },

            ArgumentException => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Code = MessageCodes.VALIDATION_FAILED,
                Message = exception.Message
            },

            UnauthorizedAccessException => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Code = MessageCodes.AUTH_USER_NOT_AUTHENTICATED,
                Message = string.IsNullOrEmpty(exception.Message)
                    ? MessageDefaults.GetMessage(MessageCodes.AUTH_USER_NOT_AUTHENTICATED)
                    : exception.Message
            },

            KeyNotFoundException => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Code = MessageCodes.RECORD_NOT_FOUND,
                Message = exception.Message
            },

            _ => new ApiErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Code = MessageCodes.INTERNAL_ERROR,
                Message = MessageDefaults.GetMessage(MessageCodes.INTERNAL_ERROR)
            }
        };

        context.Response.StatusCode = response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
