using System.Net;
using Hems_Backend.Application.Common.Models;

namespace Hems_Backend.Application.Common.Exceptions;

/// <summary>
/// Base application exception with structured error codes for frontend i18n.
/// </summary>
public class AppException : Exception
{
    /// <summary>
    /// Machine-readable error code for frontend i18n lookup.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// HTTP status code to return.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Parameters for message interpolation.
    /// </summary>
    public Dictionary<string, object>? Params { get; }

    public AppException(
        string code,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        Dictionary<string, object>? parameters = null)
        : base(MessageDefaults.GetMessage(code))
    {
        Code = code;
        StatusCode = statusCode;
        Params = parameters;
    }

    public AppException(
        string code,
        string customMessage,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        Dictionary<string, object>? parameters = null)
        : base(customMessage)
    {
        Code = code;
        StatusCode = statusCode;
        Params = parameters;
    }

    /// <summary>
    /// Creates an AppException with a single parameter.
    /// </summary>
    public static AppException WithParam(string code, string paramName, object paramValue, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        return new AppException(code, statusCode, new Dictionary<string, object> { [paramName] = paramValue });
    }

    /// <summary>
    /// Creates a Not Found exception.
    /// </summary>
    public static AppException NotFound(string code, Dictionary<string, object>? parameters = null)
    {
        return new AppException(code, HttpStatusCode.NotFound, parameters);
    }

    /// <summary>
    /// Creates a Forbidden exception.
    /// </summary>
    public static AppException Forbidden(string code, Dictionary<string, object>? parameters = null)
    {
        return new AppException(code, HttpStatusCode.Forbidden, parameters);
    }

    /// <summary>
    /// Creates an Unauthorized exception.
    /// </summary>
    public static AppException Unauthorized(string code, Dictionary<string, object>? parameters = null)
    {
        return new AppException(code, HttpStatusCode.Unauthorized, parameters);
    }

    /// <summary>
    /// Creates a Bad Request exception.
    /// </summary>
    public static AppException BadRequest(string code, Dictionary<string, object>? parameters = null)
    {
        return new AppException(code, HttpStatusCode.BadRequest, parameters);
    }

    /// <summary>
    /// Creates a Conflict exception.
    /// </summary>
    public static AppException Conflict(string code, Dictionary<string, object>? parameters = null)
    {
        return new AppException(code, HttpStatusCode.Conflict, parameters);
    }
}

/// <summary>
/// Structured API error response.
/// </summary>
public class ApiErrorResponse
{
    public int StatusCode { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Params { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiErrorResponse FromException(AppException ex)
    {
        return new ApiErrorResponse
        {
            StatusCode = (int)ex.StatusCode,
            Code = ex.Code,
            Message = ex.Message,
            Params = ex.Params
        };
    }

    public static ApiErrorResponse FromException(Exception ex, int statusCode, string code)
    {
        return new ApiErrorResponse
        {
            StatusCode = statusCode,
            Code = code,
            Message = ex.Message
        };
    }
}

/// <summary>
/// Structured API success response with optional data.
/// </summary>
public class ApiResponse<T>
{
    public int StatusCode { get; set; } = 200;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data, string code = "", string? message = null)
    {
        return new ApiResponse<T>
        {
            StatusCode = 200,
            Code = code,
            Message = message ?? (string.IsNullOrEmpty(code) ? "Success" : MessageDefaults.GetMessage(code)),
            Data = data
        };
    }

    public static ApiResponse<T> Created(T data, string code = "", string? message = null)
    {
        return new ApiResponse<T>
        {
            StatusCode = 201,
            Code = code,
            Message = message ?? (string.IsNullOrEmpty(code) ? "Created" : MessageDefaults.GetMessage(code)),
            Data = data
        };
    }
}

/// <summary>
/// Simple API response without data.
/// </summary>
public class ApiResponse
{
    public int StatusCode { get; set; } = 200;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse Success(string code)
    {
        return new ApiResponse
        {
            StatusCode = 200,
            Code = code,
            Message = MessageDefaults.GetMessage(code)
        };
    }

    public static ApiResponse Success(string code, string message)
    {
        return new ApiResponse
        {
            StatusCode = 200,
            Code = code,
            Message = message
        };
    }
}
