using Microsoft.AspNetCore.Http.HttpResults;

namespace Hems_Backend.Web.Endpoints;

public class SentryTest : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(TestSentryException, "test-exception")
            .MapGet(TestSentryLog, "test-log")
            .MapGet(HealthCheck);
    }

    /// <summary>
    /// Test endpoint to trigger a Sentry exception capture
    /// </summary>
    public IResult TestSentryException()
    {
        throw new Exception("Sentry test exception - This is intentional for testing error tracking!");
    }

    /// <summary>
    /// Test endpoint to send a Sentry log event
    /// </summary>
    public Ok<string> TestSentryLog(ILogger<SentryTest> logger)
    {
        logger.LogError("Sentry test log - Error level message sent to Sentry");
        logger.LogWarning("Sentry test log - Warning level message (not sent to Sentry)");
        logger.LogInformation("Sentry test log - Info level breadcrumb");

        return TypedResults.Ok("Sentry log test completed. Check Sentry dashboard for error event.");
    }

    /// <summary>
    /// Health check endpoint (does not trigger Sentry)
    /// </summary>
    public Ok<string> HealthCheck()
    {
        return TypedResults.Ok("Sentry integration is active and ready!");
    }
}
