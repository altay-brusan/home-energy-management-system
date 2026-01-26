using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SignalRTester;

class Program
{
    // Base URLs - Change these as needed
    private static string BaseApiUrl = "https://hems-api-dev.oklidis.com.tr";
    private static string BaseHubUrl = "https://hems-api-dev.oklidis.com.tr";

    private static string? _cachedToken;

    static async Task Main(string[] args)
    {
        Console.WriteLine("🔔 SignalR Hub Tester");
        Console.WriteLine("=====================================");
        Console.WriteLine();

        while (true)
        {
            Console.WriteLine("Select environment:");
            Console.WriteLine("  1. DEV  (hems-api-dev.oklidis.com.tr)");
            Console.WriteLine("  2. PROD (hems-api.pentaunity.com)");
            Console.Write("Choice [1]: ");
            var envChoice = Console.ReadLine();

            if (envChoice == "2")
            {
                BaseApiUrl = "https://hems-api.pentaunity.com";
                BaseHubUrl = "https://hems-api.pentaunity.com";
            }
            else
            {
                BaseApiUrl = "https://hems-api-dev.oklidis.com.tr";
                BaseHubUrl = "https://hems-api-dev.oklidis.com.tr";
            }

            Console.WriteLine($"✅ Using: {BaseApiUrl}");
            Console.WriteLine();

            Console.WriteLine("Select test mode:");
            Console.WriteLine("  1. DeviceNotificationHub (Alarms/Notifications)");
            Console.WriteLine("  2. DeviceSettingsHub (Parameter Updates) - Listen Only");
            Console.WriteLine("  3. DeviceSettingsHub (Parameter Updates) - Full Test (API + SignalR)");
            Console.WriteLine("  4. PowerFlowHub (Power Flow Data)");
            Console.WriteLine("  q. Quit");
            Console.Write("Choice: ");

            var choice = Console.ReadLine();

            switch (choice?.ToLower())
            {
                case "1":
                    await TestDeviceNotificationHub();
                    break;
                case "2":
                    await TestDeviceSettingsHub();
                    break;
                case "3":
                    await TestDeviceSettingsHubFullTest();
                    break;
                case "4":
                    await TestPowerFlowHub();
                    break;
                case "q":
                    return;
                default:
                    Console.WriteLine("Invalid choice");
                    break;
            }

            Console.WriteLine("\n=====================================\n");
        }
    }

    // ========================================================================
    // DeviceSettingsHub Test - Listen Only
    // ========================================================================
    static async Task TestDeviceSettingsHub()
    {
        Console.WriteLine("\n🔧 DeviceSettingsHub Tester (Listen Only)");
        Console.WriteLine("=========================================");

        var hubUrl = $"{BaseHubUrl}/deviceSettingsHub";

        Console.Write("Enter Correlation ID to subscribe: ");
        var correlationId = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            Console.WriteLine("❌ Correlation ID is required");
            return;
        }

        var connection = CreateHubConnection(hubUrl, useWebSockets: true);

        // Set up message handlers
        SetupDeviceSettingsHubHandlers(connection);

        try
        {
            await connection.StartAsync();
            Console.WriteLine($"✅ Connected to DeviceSettingsHub");
            Console.WriteLine($"🆔 Connection ID: {connection.ConnectionId}");

            // Subscribe to batch operation
            Console.WriteLine($"\n📡 Subscribing to batch operation: {correlationId}");
            await connection.InvokeAsync("SubscribeToBatchOperation", correlationId);
            Console.WriteLine("✅ Subscribed! Waiting for events...");
            Console.WriteLine("\nPress 'q' to quit, 'u' to unsubscribe");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q')
                {
                    Console.WriteLine("\n👋 Disconnecting...");
                    break;
                }
                else if (key.KeyChar == 'u')
                {
                    await connection.InvokeAsync("UnsubscribeFromBatchOperation", correlationId);
                    Console.WriteLine("✅ Unsubscribed from batch operation");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    // ========================================================================
    // DeviceSettingsHub Test - Full Test (API Call + SignalR)
    // ========================================================================
    static async Task TestDeviceSettingsHubFullTest()
    {
        Console.WriteLine("\n🔧 DeviceSettingsHub Full Test (API + SignalR)");
        Console.WriteLine("==============================================");

        var hubUrl = $"{BaseHubUrl}/deviceSettingsHub";

        Console.Write("Enter Device ID: ");
        var deviceIdStr = Console.ReadLine();

        if (!Guid.TryParse(deviceIdStr, out var deviceId))
        {
            Console.WriteLine("❌ Invalid Device ID format");
            return;
        }

        // Get token first
        var token = await GetJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Failed to get auth token");
            return;
        }

        // Create SignalR connection
        var connection = CreateHubConnection(hubUrl, useWebSockets: true);

        // Set up message handlers
        SetupDeviceSettingsHubHandlers(connection);

        try
        {
            await connection.StartAsync();
            Console.WriteLine($"✅ Connected to DeviceSettingsHub");
            Console.WriteLine($"🆔 Connection ID: {connection.ConnectionId}");

            // Get parameters for the device
            Console.WriteLine($"\n📡 Getting parameters for device: {deviceId}");
            var parameters = await GetDeviceParameters(token, deviceId);

            if (parameters == null || parameters.Count == 0)
            {
                Console.WriteLine("❌ No parameters found for device");
                return;
            }

            Console.WriteLine($"✅ Found {parameters.Count} parameters");

            // Show parameters (prioritize writable, but show all if none writable)
            var writableParams = parameters.Where(p => p.IsWritable).Take(10).ToList();

            if (writableParams.Count > 0)
            {
                Console.WriteLine("\nWritable parameters (RoleType=setting):");
                int idx = 1;
                foreach (var param in writableParams)
                {
                    Console.WriteLine($"  {idx}. {param.ParameterName} = {param.Value} {param.Unit} (ID: {param.ParameterId})");
                    idx++;
                }
            }
            else
            {
                Console.WriteLine("\n⚠️ No writable parameters found (RoleType != 'setting')");
                Console.WriteLine("\nShowing first 15 parameters:");
                int idx = 1;
                foreach (var param in parameters.Take(15))
                {
                    Console.WriteLine($"  {idx}. {param.ParameterName} = {param.Value} {param.Unit} [RoleType: {param.RoleType ?? "null"}] (ID: {param.ParameterId})");
                    idx++;
                }
                // Use all parameters for selection
                writableParams = parameters.Take(15).ToList();
            }

            Console.Write("\nEnter parameter number to update (or 'c' to enter custom): ");
            var paramChoice = Console.ReadLine();

            DeviceParameterDto? selectedParam = null;
            if (paramChoice?.ToLower() == "c")
            {
                Console.Write("Enter Parameter ID: ");
                var paramIdStr = Console.ReadLine();
                if (Guid.TryParse(paramIdStr, out var paramId))
                {
                    selectedParam = parameters.FirstOrDefault(p => p.ParameterId == paramId);
                }
            }
            else if (int.TryParse(paramChoice, out var paramIdx) && paramIdx > 0 && paramIdx <= writableParams.Count)
            {
                selectedParam = writableParams[paramIdx - 1];
            }

            if (selectedParam == null)
            {
                Console.WriteLine("❌ Invalid parameter selection");
                return;
            }

            Console.WriteLine($"\nSelected: {selectedParam.ParameterName} = {selectedParam.Value} {selectedParam.Unit}");
            Console.Write("Enter new value: ");
            var newValue = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(newValue))
            {
                Console.WriteLine("❌ Value is required");
                return;
            }

            // Call the API to update parameter
            Console.WriteLine($"\n🚀 Calling API to update parameter...");
            var correlationId = await UpdateDeviceParameters(token, deviceId, selectedParam.ParameterId, newValue);

            if (string.IsNullOrEmpty(correlationId))
            {
                Console.WriteLine("❌ Failed to start parameter update");
                return;
            }

            Console.WriteLine($"✅ API returned Correlation ID: {correlationId}");

            // Subscribe to the batch operation
            Console.WriteLine($"\n📡 Subscribing to batch operation: {correlationId}");
            await connection.InvokeAsync("SubscribeToBatchOperation", correlationId);
            Console.WriteLine("✅ Subscribed! Waiting for SignalR events...");
            Console.WriteLine("\n⏳ Watching for events (press 'q' to quit)...\n");

            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var completed = false;

            // Wait for completion or timeout
            while (!completed && !cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q')
                    {
                        Console.WriteLine("\n👋 User requested quit");
                        break;
                    }
                }
                await Task.Delay(100);
            }

            if (cts.Token.IsCancellationRequested)
            {
                Console.WriteLine("\n⏰ Timeout reached (2 minutes)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"❌ Stack: {ex.StackTrace}");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    // ========================================================================
    // DeviceNotificationHub Test
    // ========================================================================
    static async Task TestDeviceNotificationHub()
    {
        Console.WriteLine("\n🔔 DeviceNotificationHub Tester");
        Console.WriteLine("================================");

        var hubUrl = $"{BaseHubUrl}/deviceNotificationHub";

        var connection = CreateHubConnection(hubUrl, useWebSockets: false); // Uses LongPolling

        // Set up message handlers
        connection.On<DeviceNotificationDto>("ReceiveDeviceNotification", (notification) =>
        {
            Console.WriteLine($"\n📨 NEW NOTIFICATION!");
            Console.WriteLine($"   Device: {notification.DeviceManufacturer}");
            Console.WriteLine($"   Site: {notification.SiteName}");
            Console.WriteLine($"   Type: {notification.DeviceNotificationType.Code}");
            Console.WriteLine($"   Message: {notification.DeviceNotificationType.Message}");
            Console.WriteLine($"   Category: {notification.DeviceNotificationType.Category}");
            Console.WriteLine($"   Timestamp: {notification.Timestamp}");
            Console.WriteLine("-------------------------------------");
        });

        connection.On<DeviceNotificationDto>("ReceiveDeviceNotificationUpdate", (notification) =>
        {
            Console.WriteLine($"\n🔄 NOTIFICATION UPDATE!");
            Console.WriteLine($"   Device: {notification.DeviceManufacturer}");
            Console.WriteLine($"   Type: {notification.DeviceNotificationType.Code}");
            Console.WriteLine($"   Message: {notification.DeviceNotificationType.Message}");
            Console.WriteLine("-------------------------------------");
        });

        try
        {
            await connection.StartAsync();
            Console.WriteLine($"✅ Connected!");
            Console.WriteLine($"🆔 Connection ID: {connection.ConnectionId}");

            Console.Write("\nEnter Site ID to join: ");
            var siteIdStr = Console.ReadLine();

            if (Guid.TryParse(siteIdStr, out var siteId))
            {
                await connection.InvokeAsync("JoinSiteGroup", siteId);
                Console.WriteLine($"✅ Joined site group: {siteId}");
            }

            Console.WriteLine("\n🎯 Waiting for notifications...");
            Console.WriteLine("Press 'q' to quit");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q')
                {
                    Console.WriteLine("\n👋 Disconnecting...");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    // ========================================================================
    // PowerFlowHub Test
    // ========================================================================
    static async Task TestPowerFlowHub()
    {
        Console.WriteLine("\n⚡ PowerFlowHub Tester");
        Console.WriteLine("=======================");

        var hubUrl = $"{BaseHubUrl}/powerFlowHub";

        var connection = CreateHubConnection(hubUrl, useWebSockets: true);

        connection.On<object>("ReceivePowerFlowData", (data) =>
        {
            Console.WriteLine($"\n⚡ PowerFlow Data Received:");
            Console.WriteLine($"   {JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true })}");
        });

        try
        {
            await connection.StartAsync();
            Console.WriteLine($"✅ Connected!");
            Console.WriteLine($"🆔 Connection ID: {connection.ConnectionId}");

            Console.Write("\nEnter Device ID to subscribe: ");
            var deviceIdStr = Console.ReadLine();

            if (Guid.TryParse(deviceIdStr, out var deviceId))
            {
                await connection.InvokeAsync("SubscribeToDevice", deviceId);
                Console.WriteLine($"✅ Subscribed to device: {deviceId}");
            }

            Console.WriteLine("\n🎯 Waiting for power flow data...");
            Console.WriteLine("Press 'q' to quit");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q')
                {
                    Console.WriteLine("\n👋 Disconnecting...");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    static void SetupDeviceSettingsHubHandlers(HubConnection connection)
    {
        connection.On<ParameterUpdateStartedMessage>("ParameterUpdateStarted", (msg) =>
        {
            Console.WriteLine($"\n🚀 ParameterUpdateStarted");
            Console.WriteLine($"   CorrelationId: {msg.CorrelationId}");
            Console.WriteLine($"   DeviceId: {msg.DeviceId}");
            Console.WriteLine($"   TotalParameters: {msg.TotalParameters}");
            Console.WriteLine($"   StartedAt: {msg.StartedAt}");
            Console.WriteLine("-------------------------------------");
        });

        connection.On<ParameterUpdatedMessage>("ParameterUpdated", (msg) =>
        {
            Console.WriteLine($"\n✅ ParameterUpdated [{msg.CurrentIndex}/{msg.TotalParameters}]");
            Console.WriteLine($"   Parameter: {msg.ParameterName}");
            Console.WriteLine($"   NewValue: {msg.NewValue}");
            Console.WriteLine($"   RpcMethod: {msg.RpcMethod}");
            Console.WriteLine($"   UpdatedAt: {msg.UpdatedAt}");
            Console.WriteLine("-------------------------------------");
        });

        connection.On<ParameterUpdateFailedMessage>("ParameterUpdateFailed", (msg) =>
        {
            Console.WriteLine($"\n❌ ParameterUpdateFailed [{msg.CurrentIndex}/{msg.TotalParameters}]");
            Console.WriteLine($"   Parameter: {msg.ParameterName}");
            Console.WriteLine($"   Error: {msg.ErrorMessage}");
            Console.WriteLine($"   RpcMethod: {msg.RpcMethod}");
            Console.WriteLine($"   FailedAt: {msg.FailedAt}");
            Console.WriteLine("-------------------------------------");
        });

        connection.On<ParameterUpdateCompletedMessage>("ParameterUpdateCompleted", (msg) =>
        {
            Console.WriteLine($"\n🏁 ParameterUpdateCompleted");
            Console.WriteLine($"   CorrelationId: {msg.CorrelationId}");
            Console.WriteLine($"   Success: {msg.SuccessCount}/{msg.TotalParameters}");
            Console.WriteLine($"   Failed: {msg.FailedCount}");
            Console.WriteLine($"   Skipped: {msg.SkippedCount}");
            Console.WriteLine($"   WasCancelled: {msg.WasCancelled}");
            Console.WriteLine($"   Duration: {msg.Duration}");
            Console.WriteLine($"   CompletedAt: {msg.CompletedAt}");

            if (msg.FailedParameters?.Count > 0)
            {
                Console.WriteLine($"   Failed Parameters:");
                foreach (var fp in msg.FailedParameters)
                {
                    Console.WriteLine($"      - {fp.ParameterName}: {fp.ErrorMessage} (Retries: {fp.RetryAttempts})");
                }
            }
            Console.WriteLine("=====================================");
        });

        // Also register raw event handler to catch any unexpected messages
        connection.On<object>("ParameterUpdateStarted", (msg) =>
        {
            Console.WriteLine($"\n[RAW] ParameterUpdateStarted: {JsonSerializer.Serialize(msg)}");
        });
    }

    static HubConnection CreateHubConnection(string hubUrl, bool useWebSockets)
    {
        // Note: The method is synchronous but creates async callbacks internally
        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback =
            (message, cert, chain, errors) => true;

        Console.WriteLine("⚠️ WARNING: SSL certificate validation is DISABLED for testing!");
        Console.WriteLine($"🔌 Connecting to: {hubUrl}");
        Console.WriteLine($"🔧 Transport: {(useWebSockets ? "WebSockets (skipNegotiation)" : "LongPolling")}");

        var builder = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                if (useWebSockets)
                {
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                    options.SkipNegotiation = true;
                }
                else
                {
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                }

                options.HttpMessageHandlerFactory = _ => httpClientHandler;

                options.AccessTokenProvider = async () =>
                {
                    try
                    {
                        var token = await GetJwtTokenAsync();
                        if (string.IsNullOrEmpty(token))
                        {
                            Console.WriteLine("❌ JWT Token not available!");
                            throw new UnauthorizedAccessException("JWT Token not available");
                        }
                        Console.WriteLine($"🔑 Token: {token.Substring(0, Math.Min(30, token.Length))}...");
                        return token;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error getting token: {ex.Message}");
                        throw;
                    }
                };
            })
            .WithAutomaticReconnect()
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            });

        var connection = builder.Build();

        connection.Closed += (error) =>
        {
            Console.WriteLine($"❌ Connection closed: {error?.Message}");
            return Task.CompletedTask;
        };

        connection.Reconnecting += (error) =>
        {
            Console.WriteLine($"🔄 Reconnecting: {error?.Message}");
            return Task.CompletedTask;
        };

        connection.Reconnected += (connectionId) =>
        {
            Console.WriteLine($"✅ Reconnected with ID: {connectionId}");
            return Task.CompletedTask;
        };

        return connection;
    }

    static async Task<string?> GetJwtTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken))
        {
            return _cachedToken;
        }

        try
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true;

            using var httpClient = new HttpClient(handler);
            var loginUrl = $"{BaseApiUrl}/api/Users/login";

            var loginRequest = new
            {
                email = "administrator@localhost",
                password = "***REMOVED***"
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            Console.WriteLine($"🔐 Logging in to: {loginUrl}");
            var response = await httpClient.PostAsync(loginUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<IdentityLoginResponse>(responseContent);
                if (loginResponse?.accessToken != null)
                {
                    _cachedToken = loginResponse.accessToken;
                    Console.WriteLine($"✅ Login successful");
                    return _cachedToken;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Login failed: {response.StatusCode} - {errorContent}");
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting JWT token: {ex.Message}");
            return null;
        }
    }

    static async Task<List<DeviceParameterDto>?> GetDeviceParameters(string token, Guid deviceId)
    {
        try
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true;

            using var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var url = $"{BaseApiUrl}/api/DeviceSettings/device/{deviceId}/parameters";
            Console.WriteLine($"📡 GET {url}");

            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<DeviceParametersVm>(json, options);
                return result?.Parameters;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Failed to get parameters: {response.StatusCode} - {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return null;
        }
    }

    static async Task<string?> UpdateDeviceParameters(string token, Guid deviceId, Guid parameterId, string value)
    {
        try
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true;

            using var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Use batch update endpoint
            var url = $"{BaseApiUrl}/api/DeviceSettings/device/{deviceId}/parameters";
            Console.WriteLine($"📡 PUT {url}");

            var requestBody = new
            {
                parameters = new[]
                {
                    new { parameterId = parameterId, value = value }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            Console.WriteLine($"📦 Request: {json}");

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync(url, content);

            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"📬 Response ({response.StatusCode}): {responseJson}");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<UpdateParametersResponse>(responseJson, options);
                return result?.CorrelationId;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return null;
        }
    }
}

// ========================================================================
// DTOs
// ========================================================================

public class IdentityLoginResponse
{
    public string accessToken { get; set; } = string.Empty;
    public string refreshToken { get; set; } = string.Empty;
    public DateTime expiresAt { get; set; }
}

public class DeviceParametersVm
{
    public Guid DeviceId { get; set; }
    public List<DeviceParameterDto> Parameters { get; set; } = new();
}

public class DeviceParameterDto
{
    public Guid Id { get; set; }
    public Guid SetupDeviceId { get; set; }
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string ParameterDescription { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;  // "setting" = writable, "info" = read-only
    public Guid? ParameterGroupId { get; set; }
    public string? ParameterGroupName { get; set; }
    public int SortOrder { get; set; }

    // Helper property - "User" and "Admin" roles are writable
    public bool IsWritable => RoleType?.ToLower() == "user" || RoleType?.ToLower() == "admin";
}

public class UpdateParametersResponse
{
    public bool Success { get; set; }
    public string? CorrelationId { get; set; }
    public string? Message { get; set; }
}

// DeviceSettingsHub SignalR Messages
public class ParameterUpdateStartedMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid DeviceId { get; set; }
    public int TotalParameters { get; set; }
    public DateTime StartedAt { get; set; }
}

public class ParameterUpdatedMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid DeviceId { get; set; }
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string? RpcMethod { get; set; }
    public int CurrentIndex { get; set; }
    public int TotalParameters { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ParameterUpdateFailedMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid DeviceId { get; set; }
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? RpcMethod { get; set; }
    public int CurrentIndex { get; set; }
    public int TotalParameters { get; set; }
    public DateTime FailedAt { get; set; }
}

public class ParameterUpdateCompletedMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid DeviceId { get; set; }
    public int TotalParameters { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public bool WasCancelled { get; set; }
    public List<FailedParameterInfo>? FailedParameters { get; set; }
}

public class FailedParameterInfo
{
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryAttempts { get; set; }
}

// DeviceNotificationHub DTOs
public class DeviceNotificationDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid SiteId { get; set; }
    public Guid SetupDeviceId { get; set; }
    public Guid DeviceNotificationTypeId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string AdditionalData { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public DateTimeOffset Created { get; set; }
    public DeviceNotificationTypeDto DeviceNotificationType { get; set; } = null!;
    public string DeviceManufacturer { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
}

public class DeviceNotificationTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
