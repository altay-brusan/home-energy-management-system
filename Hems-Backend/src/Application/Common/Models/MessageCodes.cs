namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Standardized message codes for API responses.
/// These codes are used by frontend for i18n lookup.
/// </summary>
public static class MessageCodes
{
    #region Authentication & Authorization (AUTH_*)

    public const string AUTH_USER_NOT_AUTHENTICATED = "AUTH_USER_NOT_AUTHENTICATED";
    public const string AUTH_INVALID_CREDENTIALS = "AUTH_INVALID_CREDENTIALS";
    public const string AUTH_EMAIL_NOT_CONFIRMED = "AUTH_EMAIL_NOT_CONFIRMED";
    public const string AUTH_ACCOUNT_LOCKED = "AUTH_ACCOUNT_LOCKED";
    public const string AUTH_TOKEN_EXPIRED = "AUTH_TOKEN_EXPIRED";
    public const string AUTH_PERMISSION_DENIED = "AUTH_PERMISSION_DENIED";
    public const string AUTH_AUTHORIZATION_REQUIRED = "AUTH_AUTHORIZATION_REQUIRED";

    #endregion

    #region Invitations (INVITATION_*)

    public const string INVITATION_NOT_FOUND = "INVITATION_NOT_FOUND";
    public const string INVITATION_EXPIRED = "INVITATION_EXPIRED";
    public const string INVITATION_ALREADY_ACCEPTED = "INVITATION_ALREADY_ACCEPTED";
    public const string INVITATION_ALREADY_DECLINED = "INVITATION_ALREADY_DECLINED";
    public const string INVITATION_ALREADY_EXISTS = "INVITATION_ALREADY_EXISTS";
    public const string INVITATION_CANNOT_INVITE_SELF = "INVITATION_CANNOT_INVITE_SELF";
    public const string INVITATION_CANNOT_INVITE_OWNER = "INVITATION_CANNOT_INVITE_OWNER";
    public const string INVITATION_ONLY_OWNER_CAN_INVITE = "INVITATION_ONLY_OWNER_CAN_INVITE";
    public const string INVITATION_ONLY_OWNER_CAN_REMOVE = "INVITATION_ONLY_OWNER_CAN_REMOVE";
    public const string INVITATION_USER_ALREADY_MEMBER = "INVITATION_USER_ALREADY_MEMBER";
    public const string INVITATION_SITE_ALREADY_SHARED = "INVITATION_SITE_ALREADY_SHARED";

    // Success
    public const string INVITATION_ACCEPTED = "INVITATION_ACCEPTED";
    public const string INVITATION_DECLINED = "INVITATION_DECLINED";
    public const string INVITATION_SENT = "INVITATION_SENT";
    public const string INVITATION_USER_REMOVED = "INVITATION_USER_REMOVED";

    #endregion

    #region Sites (SITE_*)

    public const string SITE_NOT_FOUND = "SITE_NOT_FOUND";
    public const string SITE_ACCESS_DENIED = "SITE_ACCESS_DENIED";
    public const string SITE_ONLY_OWNER_CAN_DELETE = "SITE_ONLY_OWNER_CAN_DELETE";
    public const string SITE_ONLY_OWNER_CAN_UPDATE = "SITE_ONLY_OWNER_CAN_UPDATE";
    public const string SITE_CANNOT_DELETE_WITH_SETUP = "SITE_CANNOT_DELETE_WITH_SETUP";
    public const string SITE_COORDINATES_NOT_SET = "SITE_COORDINATES_NOT_SET";

    // Success
    public const string SITE_CREATED = "SITE_CREATED";
    public const string SITE_UPDATED = "SITE_UPDATED";
    public const string SITE_DELETED = "SITE_DELETED";

    #endregion

    #region Setups (SETUP_*)

    public const string SETUP_NOT_FOUND = "SETUP_NOT_FOUND";
    public const string SETUP_MISSING_DEVICE_IDENTIFIER = "SETUP_MISSING_DEVICE_IDENTIFIER";
    public const string SETUP_SITE_ALREADY_HAS_SETUP = "SETUP_SITE_ALREADY_HAS_SETUP";
    public const string SETUP_DEVICE_NOT_FOUND_THINGSBOARD = "SETUP_DEVICE_NOT_FOUND_THINGSBOARD";
    public const string SETUP_DEVICE_NOT_FOUND_LOCAL = "SETUP_DEVICE_NOT_FOUND_LOCAL";
    public const string SETUP_DEVICE_ALREADY_IN_USE = "SETUP_DEVICE_ALREADY_IN_USE";
    public const string SETUP_CANNOT_DELETE_WITH_DEVICES = "SETUP_CANNOT_DELETE_WITH_DEVICES";

    // Success
    public const string SETUP_CREATED = "SETUP_CREATED";
    public const string SETUP_UPDATED = "SETUP_UPDATED";
    public const string SETUP_DELETED = "SETUP_DELETED";

    #endregion

    #region Devices (DEVICE_*)

    public const string DEVICE_NOT_FOUND = "DEVICE_NOT_FOUND";
    public const string DEVICE_ACCESS_DENIED = "DEVICE_ACCESS_DENIED";
    public const string DEVICE_NOT_IN_SETUP = "DEVICE_NOT_IN_SETUP";
    public const string DEVICE_NOT_ACTIVE = "DEVICE_NOT_ACTIVE";
    public const string DEVICE_NO_THINGSBOARD_MAPPING = "DEVICE_NO_THINGSBOARD_MAPPING";

    #endregion

    #region Device Components (DEVICE_COMPONENT_*)

    public const string DEVICE_COMPONENT_NOT_FOUND = "DEVICE_COMPONENT_NOT_FOUND";

    #endregion

    #region Parameters (PARAMETER_*)

    public const string PARAMETER_NOT_FOUND = "PARAMETER_NOT_FOUND";
    public const string PARAMETER_VALUE_NOT_FOUND = "PARAMETER_VALUE_NOT_FOUND";
    public const string PARAMETER_ALREADY_EXISTS = "PARAMETER_ALREADY_EXISTS";
    public const string PARAMETER_IN_USE = "PARAMETER_IN_USE";
    public const string PARAMETER_IN_SCHEMA = "PARAMETER_IN_SCHEMA";
    public const string PARAMETER_GROUP_NOT_FOUND = "PARAMETER_GROUP_NOT_FOUND";
    public const string PARAMETER_GROUP_CIRCULAR_REF = "PARAMETER_GROUP_CIRCULAR_REF";
    public const string PARAMETER_GROUP_HAS_SUBGROUPS = "PARAMETER_GROUP_HAS_SUBGROUPS";
    public const string PARAMETER_GROUP_HAS_PARAMETERS = "PARAMETER_GROUP_HAS_PARAMETERS";

    // Success
    public const string PARAMETER_UPDATED = "PARAMETER_UPDATED";
    public const string PARAMETER_CREATED = "PARAMETER_CREATED";
    public const string PARAMETER_DELETED = "PARAMETER_DELETED";

    #endregion

    #region Notifications (NOTIFICATION_*)

    public const string NOTIFICATION_NOT_FOUND = "NOTIFICATION_NOT_FOUND";
    public const string NOTIFICATION_ACCESS_DENIED = "NOTIFICATION_ACCESS_DENIED";
    public const string NOTIFICATION_TYPE_NOT_FOUND = "NOTIFICATION_TYPE_NOT_FOUND";
    public const string NOTIFICATION_DEVICE_NOT_ASSIGNED = "NOTIFICATION_DEVICE_NOT_ASSIGNED";

    // Success
    public const string NOTIFICATION_MARKED_READ = "NOTIFICATION_MARKED_READ";
    public const string NOTIFICATIONS_ALL_MARKED_READ = "NOTIFICATIONS_ALL_MARKED_READ";
    public const string NOTIFICATION_DELETED = "NOTIFICATION_DELETED";

    #endregion

    #region Mobile App Versions (VERSION_*)

    public const string VERSION_NOT_FOUND = "VERSION_NOT_FOUND";
    public const string VERSION_ALREADY_EXISTS = "VERSION_ALREADY_EXISTS";
    public const string VERSION_CANNOT_SET_INACTIVE_LATEST = "VERSION_CANNOT_SET_INACTIVE_LATEST";

    // Success
    public const string VERSION_MARKED_LATEST = "VERSION_MARKED_LATEST";
    public const string VERSION_REVOKED = "VERSION_REVOKED";

    #endregion

    #region Dashboard (DASHBOARD_*)

    public const string DASHBOARD_INVALID_PERIOD = "DASHBOARD_INVALID_PERIOD";
    public const string DASHBOARD_MISSING_DATE_RANGE = "DASHBOARD_MISSING_DATE_RANGE";
    public const string DASHBOARD_MISSING_DATE = "DASHBOARD_MISSING_DATE";
    public const string DASHBOARD_MISSING_WEEK = "DASHBOARD_MISSING_WEEK";
    public const string DASHBOARD_MISSING_MONTH = "DASHBOARD_MISSING_MONTH";
    public const string DASHBOARD_MISSING_YEAR = "DASHBOARD_MISSING_YEAR";

    #endregion

    #region Weather (WEATHER_*)

    public const string WEATHER_FETCH_FAILED = "WEATHER_FETCH_FAILED";
    public const string WEATHER_SITE_NO_COORDINATES = "WEATHER_SITE_NO_COORDINATES";

    #endregion

    #region Users (USER_*)

    public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    public const string USER_EMAIL_NOT_FOUND = "USER_EMAIL_NOT_FOUND";
    public const string USER_CANNOT_DELETE_WITH_ACTIVE_SHARES = "USER_CANNOT_DELETE_WITH_ACTIVE_SHARES";
    public const string USER_ACCOUNT_DELETED = "USER_ACCOUNT_DELETED";
    public const string USER_DEMO_CANNOT_DELETE = "USER_DEMO_CANNOT_DELETE";
    public const string USER_ADMIN_CANNOT_DELETE = "USER_ADMIN_CANNOT_DELETE";

    #endregion

    #region Generic/Common

    public const string RECORD_NOT_FOUND = "RECORD_NOT_FOUND";
    public const string VALIDATION_FAILED = "VALIDATION_FAILED";
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
    public const string INVALID_EXPORT_STRATEGY = "INVALID_EXPORT_STRATEGY";

    #endregion
}

/// <summary>
/// Default English messages for each code.
/// Used as fallback when frontend doesn't have translation.
/// </summary>
public static class MessageDefaults
{
    private static readonly Dictionary<string, string> Messages = new()
    {
        // Auth
        [MessageCodes.AUTH_USER_NOT_AUTHENTICATED] = "User not authenticated",
        [MessageCodes.AUTH_INVALID_CREDENTIALS] = "Invalid login credentials",
        [MessageCodes.AUTH_EMAIL_NOT_CONFIRMED] = "Email not confirmed. Please check your email and confirm your account.",
        [MessageCodes.AUTH_ACCOUNT_LOCKED] = "Account is locked due to multiple failed login attempts",
        [MessageCodes.AUTH_TOKEN_EXPIRED] = "Authentication token has expired",
        [MessageCodes.AUTH_PERMISSION_DENIED] = "Permission denied",
        [MessageCodes.AUTH_AUTHORIZATION_REQUIRED] = "Authorization required",

        // Invitations
        [MessageCodes.INVITATION_NOT_FOUND] = "Invitation not found",
        [MessageCodes.INVITATION_EXPIRED] = "This invitation has expired. Please request a new invitation.",
        [MessageCodes.INVITATION_ALREADY_ACCEPTED] = "This invitation has already been accepted",
        [MessageCodes.INVITATION_ALREADY_DECLINED] = "This invitation has already been declined",
        [MessageCodes.INVITATION_ALREADY_EXISTS] = "A pending invitation already exists for this user",
        [MessageCodes.INVITATION_CANNOT_INVITE_SELF] = "You cannot invite yourself",
        [MessageCodes.INVITATION_CANNOT_INVITE_OWNER] = "Cannot invite the site owner",
        [MessageCodes.INVITATION_ONLY_OWNER_CAN_INVITE] = "Only the site owner can send invitations",
        [MessageCodes.INVITATION_ONLY_OWNER_CAN_REMOVE] = "Only the site owner can remove other users",
        [MessageCodes.INVITATION_USER_ALREADY_MEMBER] = "This user is already a member of this site",
        [MessageCodes.INVITATION_SITE_ALREADY_SHARED] = "This site is already shared with you",
        [MessageCodes.INVITATION_ACCEPTED] = "Invitation accepted successfully",
        [MessageCodes.INVITATION_DECLINED] = "Invitation declined",
        [MessageCodes.INVITATION_SENT] = "Invitation sent successfully",
        [MessageCodes.INVITATION_USER_REMOVED] = "User removed from site successfully",

        // Sites
        [MessageCodes.SITE_NOT_FOUND] = "Site not found",
        [MessageCodes.SITE_ACCESS_DENIED] = "You don't have access to this site",
        [MessageCodes.SITE_ONLY_OWNER_CAN_DELETE] = "Only the site owner can delete this site",
        [MessageCodes.SITE_ONLY_OWNER_CAN_UPDATE] = "Only the site owner can update this site",
        [MessageCodes.SITE_CANNOT_DELETE_WITH_SETUP] = "Cannot delete site. Please delete the setup first.",
        [MessageCodes.SITE_COORDINATES_NOT_SET] = "Site coordinates are not configured",
        [MessageCodes.SITE_CREATED] = "Site created successfully",
        [MessageCodes.SITE_UPDATED] = "Site updated successfully",
        [MessageCodes.SITE_DELETED] = "Site deleted successfully",

        // Setups
        [MessageCodes.SETUP_NOT_FOUND] = "Setup not found",
        [MessageCodes.SETUP_MISSING_DEVICE_IDENTIFIER] = "Device QR code or serial number is required",
        [MessageCodes.SETUP_SITE_ALREADY_HAS_SETUP] = "This site already has a setup configured",
        [MessageCodes.SETUP_DEVICE_NOT_FOUND_THINGSBOARD] = "Device not found in ThingsBoard",
        [MessageCodes.SETUP_DEVICE_NOT_FOUND_LOCAL] = "Device not found",
        [MessageCodes.SETUP_DEVICE_ALREADY_IN_USE] = "This device is already in use",
        [MessageCodes.SETUP_CANNOT_DELETE_WITH_DEVICES] = "Cannot delete setup with connected devices",
        [MessageCodes.SETUP_CREATED] = "Setup created successfully",
        [MessageCodes.SETUP_UPDATED] = "Setup updated successfully",
        [MessageCodes.SETUP_DELETED] = "Setup deleted successfully",

        // Devices
        [MessageCodes.DEVICE_NOT_FOUND] = "Device not found",
        [MessageCodes.DEVICE_ACCESS_DENIED] = "Device access denied",
        [MessageCodes.DEVICE_NOT_IN_SETUP] = "Device is not assigned to any setup",
        [MessageCodes.DEVICE_NOT_ACTIVE] = "Device is not active",
        [MessageCodes.DEVICE_NO_THINGSBOARD_MAPPING] = "Device has no ThingsBoard mapping",

        // Device Components
        [MessageCodes.DEVICE_COMPONENT_NOT_FOUND] = "Device component not found",

        // Parameters
        [MessageCodes.PARAMETER_NOT_FOUND] = "Parameter not found",
        [MessageCodes.PARAMETER_VALUE_NOT_FOUND] = "Parameter value not found for this device",
        [MessageCodes.PARAMETER_ALREADY_EXISTS] = "This parameter already exists for this device",
        [MessageCodes.PARAMETER_IN_USE] = "Cannot delete this parameter as it is in use",
        [MessageCodes.PARAMETER_IN_SCHEMA] = "Cannot delete this parameter as it is used in a schema",
        [MessageCodes.PARAMETER_GROUP_NOT_FOUND] = "Parameter group not found",
        [MessageCodes.PARAMETER_GROUP_CIRCULAR_REF] = "A group cannot be its own subgroup",
        [MessageCodes.PARAMETER_GROUP_HAS_SUBGROUPS] = "Cannot delete group with subgroups",
        [MessageCodes.PARAMETER_GROUP_HAS_PARAMETERS] = "Cannot delete group with parameters",
        [MessageCodes.PARAMETER_UPDATED] = "Parameter updated successfully",
        [MessageCodes.PARAMETER_CREATED] = "Parameter created successfully",
        [MessageCodes.PARAMETER_DELETED] = "Parameter deleted successfully",

        // Notifications
        [MessageCodes.NOTIFICATION_NOT_FOUND] = "Notification not found",
        [MessageCodes.NOTIFICATION_ACCESS_DENIED] = "Notification access denied",
        [MessageCodes.NOTIFICATION_TYPE_NOT_FOUND] = "Notification type not found",
        [MessageCodes.NOTIFICATION_DEVICE_NOT_ASSIGNED] = "Device is not properly assigned to a setup",
        [MessageCodes.NOTIFICATION_MARKED_READ] = "Notification marked as read",
        [MessageCodes.NOTIFICATIONS_ALL_MARKED_READ] = "All notifications marked as read",
        [MessageCodes.NOTIFICATION_DELETED] = "Notification deleted",

        // Versions
        [MessageCodes.VERSION_NOT_FOUND] = "Version not found",
        [MessageCodes.VERSION_ALREADY_EXISTS] = "This version already exists for this platform",
        [MessageCodes.VERSION_CANNOT_SET_INACTIVE_LATEST] = "Cannot set an inactive version as latest",
        [MessageCodes.VERSION_MARKED_LATEST] = "Version marked as latest",
        [MessageCodes.VERSION_REVOKED] = "Version has been revoked",

        // Dashboard
        [MessageCodes.DASHBOARD_INVALID_PERIOD] = "Invalid period. Must be day, week, month, or year.",
        [MessageCodes.DASHBOARD_MISSING_DATE_RANGE] = "Start date and end date are required",
        [MessageCodes.DASHBOARD_MISSING_DATE] = "Date is required",
        [MessageCodes.DASHBOARD_MISSING_WEEK] = "Year and week are required",
        [MessageCodes.DASHBOARD_MISSING_MONTH] = "Year and month are required",
        [MessageCodes.DASHBOARD_MISSING_YEAR] = "Year is required",

        // Weather
        [MessageCodes.WEATHER_FETCH_FAILED] = "Failed to fetch weather data",
        [MessageCodes.WEATHER_SITE_NO_COORDINATES] = "Site coordinates are not set",

        // Users
        [MessageCodes.USER_NOT_FOUND] = "User not found",
        [MessageCodes.USER_EMAIL_NOT_FOUND] = "User email not found",
        [MessageCodes.USER_CANNOT_DELETE_WITH_ACTIVE_SHARES] = "Cannot delete account while sites are shared with other users. Please remove all shared users first.",
        [MessageCodes.USER_ACCOUNT_DELETED] = "Account deleted successfully",
        [MessageCodes.USER_DEMO_CANNOT_DELETE] = "Demo accounts cannot be deleted",
        [MessageCodes.USER_ADMIN_CANNOT_DELETE] = "Administrator accounts cannot be deleted",

        // Generic
        [MessageCodes.RECORD_NOT_FOUND] = "Record not found",
        [MessageCodes.VALIDATION_FAILED] = "Validation failed",
        [MessageCodes.INTERNAL_ERROR] = "An internal error occurred",
        [MessageCodes.SERVICE_UNAVAILABLE] = "Service temporarily unavailable",
        [MessageCodes.INVALID_EXPORT_STRATEGY] = "Invalid export strategy",
    };

    public static string GetMessage(string code) =>
        Messages.TryGetValue(code, out var message) ? message : code;
}
