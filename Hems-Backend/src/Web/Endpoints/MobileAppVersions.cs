using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.MobileAppVersions.Commands.CreateMobileAppVersion;
using Hems_Backend.Application.MobileAppVersions.Commands.DeleteMobileAppVersion;
using Hems_Backend.Application.MobileAppVersions.Commands.RevokeVersion;
using Hems_Backend.Application.MobileAppVersions.Commands.SetLatestVersion;
using Hems_Backend.Application.MobileAppVersions.Commands.UpdateMobileAppVersion;
using Hems_Backend.Application.MobileAppVersions.Queries.CheckAppVersion;
using Hems_Backend.Application.MobileAppVersions.Queries.GetMobileAppVersionById;
using Hems_Backend.Application.MobileAppVersions.Queries.GetMobileAppVersions;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// Mobile App Version API - Manages mobile app version checking and administration.
/// - Public: Version check endpoint for mobile apps
/// - Admin: Full CRUD access to version management
/// </summary>
public class MobileAppVersions : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // ==== PUBLIC - Version check (no auth required) ====
        app.MapGroup(this)
            .MapGet(CheckVersion, "Check");

        // ==== ADMIN ONLY - Version management ====
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)
            .MapGet(GetVersions, "Admin/Versions")
            .MapGet(GetVersionById, "Admin/Versions/{id}")
            .MapPost(CreateVersion, "Admin/Versions")
            .MapPut(UpdateVersion, "Admin/Versions/{id}")
            .MapDelete(DeleteVersion, "Admin/Versions/{id}")
            .MapPost(SetLatestVersion, "Admin/Versions/{id}/SetLatest")
            .MapPost(RevokeVersion, "Admin/Versions/{id}/Revoke");
    }

    // ========================================================================
    // PUBLIC ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Check if a mobile app version requires or has an update available.
    /// This endpoint is public (no authentication required).
    /// </summary>
    public async Task<AppVersionCheckResultDto> CheckVersion(
        ISender sender,
        [AsParameters] CheckVersionRequest request)
    {
        return await sender.Send(new CheckAppVersionQuery
        {
            Platform = request.Platform,
            Version = request.Version
        });
    }

    // ========================================================================
    // ADMIN ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get all mobile app versions (paginated). Admin only.
    /// </summary>
    public async Task<PaginatedList<MobileAppVersionDto>> GetVersions(
        ISender sender,
        [AsParameters] GetMobileAppVersionsQuery query)
    {
        return await sender.Send(query);
    }

    /// <summary>
    /// Get a mobile app version by ID. Admin only.
    /// </summary>
    public async Task<MobileAppVersionDto> GetVersionById(
        ISender sender,
        Guid id)
    {
        return await sender.Send(new GetMobileAppVersionByIdQuery(id));
    }

    /// <summary>
    /// Create a new mobile app version. Admin only.
    /// </summary>
    public async Task<Guid> CreateVersion(
        ISender sender,
        CreateMobileAppVersionCommand command)
    {
        return await sender.Send(command);
    }

    /// <summary>
    /// Update an existing mobile app version. Admin only.
    /// </summary>
    public async Task<IResult> UpdateVersion(
        ISender sender,
        Guid id,
        UpdateMobileAppVersionCommand command)
    {
        if (id != command.Id) return Results.BadRequest();
        await sender.Send(command);
        return Results.NoContent();
    }

    /// <summary>
    /// Soft delete a mobile app version. Admin only.
    /// </summary>
    public async Task<IResult> DeleteVersion(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteMobileAppVersionCommand(id));
        return Results.NoContent();
    }

    /// <summary>
    /// Mark a version as the latest for its platform. Admin only.
    /// </summary>
    public async Task<IResult> SetLatestVersion(
        ISender sender,
        Guid id)
    {
        await sender.Send(new SetLatestVersionCommand(id));
        return Results.Ok(new { message = "Version marked as latest." });
    }

    /// <summary>
    /// Revoke a specific version, forcing users to update. Admin only.
    /// </summary>
    public async Task<IResult> RevokeVersion(
        ISender sender,
        Guid id,
        RevokeVersionCommand command)
    {
        if (id != command.Id) return Results.BadRequest();
        await sender.Send(command);
        return Results.Ok(new { message = "Version has been revoked." });
    }
}

/// <summary>
/// Request parameters for the version check endpoint.
/// </summary>
public class CheckVersionRequest
{
    public MobilePlatform Platform { get; set; }
    public string Version { get; set; } = string.Empty;
}
