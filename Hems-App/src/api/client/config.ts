export const API_CONFIG = {
  // LOCAL TESTING - change back to 'https://hems-api-dev.oklidis.com.tr/api' for production
  // Android Emulator: use 10.0.2.2 to reach host machine's localhost
  BASE_URL: 'https://hems-api-dev.oklidis.com.tr/api',
  TIMEOUT: 30000,
  RETRY_ATTEMPTS: 3,
  RETRY_DELAY: 1000,
  TOKEN_REFRESH_THRESHOLD: 300000,
} as const;

export const ENDPOINTS = {
  AUTH: {
    LOGIN: '/Users/login',
    REGISTER: '/Users/register',
    REFRESH: '/Users/refresh',
    USER_STATUS: '/Auth/user-status',
    USER_STATUS_MOBILE: '/Auth/mobile/get-user-status',
    FORGOT_PASSWORD: '/Users/forgotPassword',
    RESET_PASSWORD: '/Users/resetPassword',
    CONFIRM_EMAIL: '/Users/confirmEmail',
    RESEND_CONFIRMATION: '/Users/resendConfirmationEmail',
    USER_INFO: '/Users/userInfo2',
    MANAGE_2FA: '/Users/manage/2fa',
    MANAGE_INFO: '/Users/manage/info',
    UPDATE_PROFILE: '/Users/update-profile',
  },
  SITES: {
    // Admin endpoints (require AdminOnly authorization)
    GET_ALL: '/Sites/Admin/Sites',
    GET_BY_ID: '/Sites/Admin/Site/:id',
    DELETE_ADMIN: '/Sites/Admin/Site/:id',
    // User-facing endpoints
    GET_USER_SITES: '/Sites/UserSites',
    CREATE: '/Sites/Site',
    UPDATE: '/Sites/Site/:id',
    DELETE: '/Sites/Site/:id',
  },
  SETUPS: {
    // Admin endpoints (require AdminOnly authorization)
    GET_ALL: '/Setups/Admin/Setups',
    GET_BY_ID: '/Setups/Admin/Setup/:id',
    DELETE_ADMIN: '/Setups/Admin/Setup/:id',
    // User-facing endpoints
    GET_SETUP_DEVICE: '/Setups/SetupDevice',
    CREATE: '/Setups/Setup',
    UPDATE: '/Setups/Setup/:id',
    DELETE: '/Setups/Setup/:id',
  },
  DEVICES: {
    // User-facing endpoint
    LOGIN: '/Devices/login',
    // Admin endpoints (require AdminOnly authorization)
    GET_BY_ID: '/Devices/Admin/Device/:id',
    CREATE: '/Devices/Admin/Device',
    UPDATE: '/Devices/Admin/Device/:id',
    DELETE: '/Devices/Admin/Device/:id',
  },
  DEVICE_COMPONENTS: {
    // User-facing endpoints (used by mobile app)
    GET_DEVICE_SUMMARY: '/DeviceComponents/DeviceSummary',
    GET_COMPONENT_DETAILS: '/DeviceComponents/Details/:componentId',
    // Admin endpoints (require AdminOnly authorization)
    GET_BY_DEVICE: '/DeviceComponents/Admin/DeviceComponents/:deviceId',
    GET_BY_ID: '/DeviceComponents/Admin/Component/:id',
    CREATE: '/DeviceComponents/Admin/Component',
    UPDATE: '/DeviceComponents/Admin/Component/:id',
    DELETE: '/DeviceComponents/Admin/Component/:id',
  },
  DEVICE_DATA: {
    GET_ALL: '/DeviceData',
    CREATE: '/DeviceData',
    CREATE_ENERGY: '/DeviceData/energy',
    BULK_CREATE: '/DeviceData/bulk',
    GET_STATUS: '/DeviceData/Status/:deviceId',
  },
  DASHBOARD: {
    GET_MOBILE: '/Dashboard/mobile/get-dashboard',
    GET_COMPONENT_DATA: '/Dashboard/mobile/device/:deviceId/component/:componentId/data',
    HERO: '/Dashboard/hero',
    SOC: '/Dashboard/Soc',
    SOH: '/Dashboard/Soh',
    TODAY_SUMMARY: '/Dashboard/TodayEnergySummary',
    POWER_AND_ENERGY_METRICS: '/Dashboard/PowerAndEnergyMetrics',
    GET_DEVICE_COMPONENTS_STATUS: '/Dashboard/DeviceComponentsStatus',
    BATTERY_COMPONENT: '/Dashboard/graphs/Components/Battery',
    INVERTER_COMPONENT: '/Dashboard/graphs/Components/Inverter',
    ENERGY_FLOW: '/Dashboard/graphs/EnergyFlow',
    ENERGY_PRODUCTION: '/Dashboard/graphs/EnergyProduction',
    ENERGY_CONSUMPTION: '/Dashboard/graphs/EnergyConsumption',
    POWER_METRICS: '/Dashboard/graphs/PowerMetrics',
    BATTERY: '/Dashboard/graphs/Battery',
    CHARGE_DISCHARGE: '/Dashboard/graphs/ChargeDischarge',
    GRID_PERFORMANCE: '/Dashboard/GridPerformance',
    GRID_PERFORMANCE_SUMMARY: '/Dashboard/GridPerformanceSummary',
    GET_WEATHER_FORECAST: '/WeatherForecasts/:siteId',
    ENERGY_REVENUE: '/Dashboard/EnergyRevenue',
    ENERGY_SOURCE_MIX: '/Dashboard/EnergySourceMix',
    SOC_HISTORY: '/Dashboard/SocHistory',
  },
  ROLES: {
    // Admin endpoints (require AdminOnly authorization)
    GET_ALL: '/RoleManagement/Admin/Roles',
    CREATE: '/RoleManagement/Admin/Roles',
    DELETE: '/RoleManagement/Admin/Roles/:roleName',
    ASSIGN: '/RoleManagement/Admin/Assign',
    GET_USER_ROLES: '/RoleManagement/Admin/User/:userId/Roles',
  },
  INVITATIONS: {
    CREATE: '/Invitations/Create',
    GET_BY_SITE: '/Invitations/BySite/:siteId',
    ACCEPT: '/Invitations/Accept',
    DECLINE: '/Invitations/Decline',
    REMOVE_USER: '/Invitations/RemoveUser/:invitationId',
  },
  DEVICE_NOTIFICATIONS: {
    CREATE: '/DeviceNotifications',
    GET_ALL: '/DeviceNotifications',
    GET_DELETED: '/DeviceNotifications/Deleted',
    MARK_READ: '/DeviceNotifications/:id/MarkRead',
    MARK_ALL_READ: '/DeviceNotifications/MarkAllRead/:siteId',
    DELETE: '/DeviceNotifications/:id',
    RESTORE: '/DeviceNotifications/:id/Restore',
    PURGE: '/DeviceNotifications/:id/Purge',
    // Admin endpoints (require AdminOnly authorization)
    GET_TYPES: '/DeviceNotifications/Admin/Types',
  },
  WEATHER_FORECASTS: {
    GET_BY_SITE: '/WeatherForecasts/:siteId',
  },
  LOCATIONS: {
    GET_COUNTRIES: '/Locations',
    GET_CITIES_BY_COUNTRY: '/Locations/Cities/:countryId',
  },
  // ParameterSchemas - merged from ParameterGroups, Parameters, ParameterValueSettingSchemes
  PARAMETER_SCHEMAS: {
    // Admin endpoints (require AdminOnly authorization)
    GROUPS: {
      GET_ALL: '/ParameterSchemas/Admin/Groups',
      GET_BY_ID: '/ParameterSchemas/Admin/Groups/:id',
      CREATE: '/ParameterSchemas/Admin/Groups',
      UPDATE: '/ParameterSchemas/Admin/Groups/:id',
      DELETE: '/ParameterSchemas/Admin/Groups/:id',
    },
    PARAMETERS: {
      GET_ALL: '/ParameterSchemas/Admin/Parameters',
      GET_BY_ID: '/ParameterSchemas/Admin/Parameters/:id',
      CREATE: '/ParameterSchemas/Admin/Parameters',
      UPDATE: '/ParameterSchemas/Admin/Parameters/:id',
      DELETE: '/ParameterSchemas/Admin/Parameters/:id',
    },
    VALUES: {
      // User-facing (all authenticated users)
      GET_ALL: '/ParameterSchemas/Values',
      GET_TREE: '/ParameterSchemas/Values/Tree',
      // Admin only
      CREATE: '/ParameterSchemas/Admin/Values',
      UPDATE: '/ParameterSchemas/Admin/Values/:id',
      DELETE: '/ParameterSchemas/Admin/Values/:id',
    },
  },
  // Legacy endpoints kept for backward compatibility - will be removed
  PARAMETER_GROUPS: {
    GET_ALL: '/ParameterSchemas/Admin/Groups',
    GET_BY_ID: '/ParameterSchemas/Admin/Groups/:id',
    CREATE: '/ParameterSchemas/Admin/Groups',
    UPDATE: '/ParameterSchemas/Admin/Groups/:id',
    DELETE: '/ParameterSchemas/Admin/Groups/:id',
  },
  PARAMETERS: {
    GET_ALL: '/ParameterSchemas/Admin/Parameters',
    GET_BY_ID: '/ParameterSchemas/Admin/Parameters/:id',
    CREATE: '/ParameterSchemas/Admin/Parameters',
    UPDATE: '/ParameterSchemas/Admin/Parameters/:id',
    DELETE: '/ParameterSchemas/Admin/Parameters/:id',
  },
  PARAMETER_VALUE_SETTING_SCHEMES: {
    GET_ALL: '/ParameterSchemas/Values',
    GET_TREE: '/ParameterSchemas/Values/Tree',
    CREATE: '/ParameterSchemas/Admin/Values',
    UPDATE: '/ParameterSchemas/Admin/Values/:id',
    DELETE: '/ParameterSchemas/Admin/Values/:id',
  },
  // DeviceSettings - includes RPC-synced operations and admin database operations
  DEVICE_SETTINGS: {
    // RPC-synced operations (User-facing, recommended)
    GET_PARAMETERS: '/DeviceSettings/device/:deviceId/parameters',
    GET_TREE: '/DeviceSettings/device/:deviceId/tree',
    UPDATE_PARAMETER: '/DeviceSettings/device/:deviceId/parameters/:parameterId',
    UPDATE_PARAMETERS: '/DeviceSettings/device/:deviceId/parameters',
    GET_MODES: '/DeviceSettings/modes',
    GET_DEVICE_MODE: '/DeviceSettings/device/:deviceId/mode',
    UPDATE_DEVICE_MODE: '/DeviceSettings/device/:deviceId/mode',
    GET_DEVICE_PRICE: '/DeviceSettings/device/:deviceId/price',
    UPDATE_DEVICE_PRICE: '/DeviceSettings/device/:deviceId/price',
    // Operation state and history (for cloud resilience)
    GET_OPERATION_STATE: '/DeviceSettings/operations/:correlationId',
    GET_OPERATION_HISTORY: '/DeviceSettings/device/:deviceId/history',
    // Demo/testing
    CHANGE_EXPORT_STRATEGY: '/DeviceSettings/demo/export-strategy',
    // Admin database-only operations
    ADMIN: {
      GET_ALL: '/DeviceSettings/admin',
      GET_TREE: '/DeviceSettings/admin/tree',
      CREATE: '/DeviceSettings/admin',
      UPDATE: '/DeviceSettings/admin/:id',
      DELETE: '/DeviceSettings/admin/:id',
      INITIALIZE: '/DeviceSettings/admin/device/:deviceId/initialize',
      UPDATE_PARAMETER: '/DeviceSettings/admin/device/:deviceId/parameter/:parameterId',
    },
  },
  // Legacy - kept for backward compatibility
  SETUP_DEVICE_PARAMETER_VALUES: {
    GET_ALL: '/DeviceSettings/admin',
    GET_TREE: '/DeviceSettings/admin/tree',
    CREATE: '/DeviceSettings/admin',
    UPDATE: '/DeviceSettings/admin/:id',
    DELETE: '/DeviceSettings/admin/:id',
  },
  // Mobile App Version Check
  MOBILE_APP_VERSIONS: {
    CHECK: '/MobileAppVersions/Check',
  },
} as const;
