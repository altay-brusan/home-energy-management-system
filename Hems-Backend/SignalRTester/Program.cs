using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRTester;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔔 SignalR Device Notification Tester");
        Console.WriteLine("=====================================");

        // SignalR Hub URL'i
        var hubUrl = "https://hems-api.pentaunity.com/deviceNotificationHub";
        
        // SSL Certificate Validation Bypass for testing (ONLY FOR DEVELOPMENT!)
        // WARNING: Never use this in production!
        var httpClientHandler = new System.Net.Http.HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = 
            (message, cert, chain, errors) => true; // Accept all certificates
        
        Console.WriteLine("⚠️ WARNING: SSL certificate validation is DISABLED for testing!");
        
        // Hub connection oluştur
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Use Long Polling as fallback (more reliable through proxies)
                // WebSockets might be blocked or not properly configured in Nginx
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                
                // Use HttpClient with SSL certificate bypass
                options.HttpMessageHandlerFactory = _ => httpClientHandler;
                
                // JWT token ile authentication 
                options.AccessTokenProvider = async () =>
                {
                    try
                    {
                        Console.WriteLine("🔐 Getting JWT token for hub connection...");
                        var token = await GetJwtTokenAsync();
                        if (string.IsNullOrEmpty(token))
                        {
                            Console.WriteLine("❌ JWT Token alınamadı!");
                            throw new UnauthorizedAccessException("JWT Token alınamadı");
                        }
                        Console.WriteLine($"🔑 JWT Token alındı: {token.Substring(0, Math.Min(20, token.Length))}...");
                        Console.WriteLine($"📏 Token length: {token.Length}");
                        return token;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error in AccessTokenProvider: {ex.Message}");
                        throw;
                    }
                };
            })
            .WithAutomaticReconnect()
            .Build();

        // Event handler'ları ekle
        connection.On<DeviceNotificationDto>("ReceiveDeviceNotification", (notification) =>
        {
            Console.WriteLine($"📨 YENİ NOTIFICATION ALINDI!");
            Console.WriteLine($"   Device: {notification.DeviceManufacturer}");
            Console.WriteLine($"   Site: {notification.SiteName}");
            Console.WriteLine($"   Type: {notification.DeviceNotificationType.Code}");
            Console.WriteLine($"   Message: {notification.DeviceNotificationType.Message}");
            Console.WriteLine($"   Category: {notification.DeviceNotificationType.Category}");
            Console.WriteLine($"   Timestamp: {notification.Timestamp}");
            Console.WriteLine($"   Additional Data: {notification.AdditionalData}");
            Console.WriteLine("-------------------------------------");
        });

        connection.On<DeviceNotificationDto>("ReceiveDeviceNotificationUpdate", (notification) =>
        {
            Console.WriteLine($"🔄 NOTIFICATION UPDATE ALINDI!");
            Console.WriteLine($"   Device: {notification.DeviceManufacturer}");
            Console.WriteLine($"   Type: {notification.DeviceNotificationType.Code}");
            Console.WriteLine($"   Message: {notification.DeviceNotificationType.Message}");
            Console.WriteLine("-------------------------------------");
        });

        // Connection event'leri
        connection.Closed += async (error) =>
        {
            Console.WriteLine($"❌ Connection closed: {error?.Message}");
            await Task.Delay(5000);
            await connection.StartAsync();
        };

        connection.Reconnecting += async (error) =>
        {
            Console.WriteLine($"🔄 Reconnecting: {error?.Message}");
            await Task.Delay(5000);
        };

        connection.Reconnected += async (connectionId) =>
        {
            Console.WriteLine($"✅ Reconnected with ID: {connectionId}");
            await Task.Delay(1000);
        };

        try
        {
            // Bağlantıyı başlat
            Console.WriteLine($"🔌 Connecting to SignalR Hub: {hubUrl}");
            Console.WriteLine($"🔄 Initial Connection State: {connection.State}");
            
            Console.WriteLine("📝 Starting handshake...");
            
            // Start the connection with a timeout
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            try
            {
                await connection.StartAsync(cts.Token);
                Console.WriteLine("✅ Connected to SignalR Hub!");
                Console.WriteLine($"🔄 New Connection State: {connection.State}");
                Console.WriteLine($"🆔 Connection ID: {connection.ConnectionId}");
                
                // Check if token was sent
                Console.WriteLine("🔍 Checking authentication...");
            }
            catch (Exception startEx)
            {
                Console.WriteLine($"❌ Failed to start connection: {startEx.GetType().Name}");
                Console.WriteLine($"❌ Error: {startEx.Message}");
                Console.WriteLine($"❌ Inner: {startEx.InnerException?.Message}");
                Console.WriteLine($"🔍 Checking if server is reachable...");
                
                // Test if server is reachable
                try
                {
                    var testHandler = new System.Net.Http.HttpClientHandler();
                    testHandler.ServerCertificateCustomValidationCallback = 
                        (message, cert, chain, errors) => true;
                    using var testClient = new System.Net.Http.HttpClient(testHandler);
                    var response = await testClient.GetAsync("https://hems-api.pentaunity.com/health");
                    Console.WriteLine($"🌐 Server health check: {response.StatusCode}");
                }
                catch (Exception serverEx)
                {
                    Console.WriteLine($"❌ Cannot reach server: {serverEx.Message}");
                }
                
                throw;
            }

            // Site group'una katıl (örnek site ID)
            var siteId = Guid.Parse("510649ed-fad7-4bfd-8af8-8db37c3e4483"); // Örnek site ID
            Console.WriteLine($"🏢 Joining site group: {siteId}");
            await connection.InvokeAsync("JoinSiteGroup", siteId);
            Console.WriteLine("✅ Joined site group!");

            Console.WriteLine("\n🎯 Waiting for notifications...");
            Console.WriteLine("Press 'q' to quit, 's' to join another site");
            Console.WriteLine("=====================================");

            // Kullanıcı input'u bekle
            while (true)
            {
                var key = Console.ReadKey(true);
                
                if (key.KeyChar == 'q')
                {
                    Console.WriteLine("👋 Disconnecting...");
                    break;
                }
                else if (key.KeyChar == 's')
                {
                    Console.Write("Enter Site ID: ");
                    var input = Console.ReadLine();
                    if (Guid.TryParse(input, out var newSiteId))
                    {
                        await connection.InvokeAsync("JoinSiteGroup", newSiteId);
                        Console.WriteLine($"✅ Joined site group: {newSiteId}");
                    }
                    else
                    {
                        Console.WriteLine("❌ Invalid Site ID format");
                    }
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine($"❌ Connection timeout: {ex.Message}");
            Console.WriteLine($"⏱️ Connection timeout after 30 seconds. The server might be unreachable or taking too long to respond.");
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            Console.WriteLine($"❌ HTTP Error: {ex.Message}");
            Console.WriteLine($"🔍 Inner Exception: {ex.InnerException?.Message}");
            Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error Type: {ex.GetType().Name}");
            Console.WriteLine($"❌ Error Message: {ex.Message}");
            Console.WriteLine($"❌ Inner Exception: {ex.InnerException?.Message}");
            Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
            
            // Check for common SignalR errors
            if (ex.Message.Contains("401"))
            {
                Console.WriteLine("\n⚠️ Authentication failed! Check your JWT token.");
            }
            else if (ex.Message.Contains("handshake"))
            {
                Console.WriteLine("\n⚠️ Handshake failed! This might be due to:");
                Console.WriteLine("   1. SSL certificate issues");
                Console.WriteLine("   2. CORS configuration");
                Console.WriteLine("   3. Server not running or unreachable");
                Console.WriteLine("   4. Authentication token issues");
            }
        }
        finally
        {
            await connection.DisposeAsync();
            Console.WriteLine("🔌 Connection disposed");
        }

        // Console'un kapanmasını engelle
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static async Task<string> GetJwtTokenAsync()
    {
        try
        {
            // Use HttpClient with SSL certificate bypass
            var handler = new System.Net.Http.HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = 
                (message, cert, chain, errors) => true;
            
            using var httpClient = new HttpClient(handler);
            var loginUrl = "https://hems-api.pentaunity.com/api/Users/login";
            
            // Örnek login request (gerçek credentials ile değiştir)
            var loginRequest = new
            {
                email = "administrator@localhost", // Gerçek email
                password = "***REMOVED***"    // Gerçek password
            };

            var json = System.Text.Json.JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            Console.WriteLine($"🔐 Attempting login to: {loginUrl}");
            var response = await httpClient.PostAsync(loginUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Login successful, response length: {responseContent.Length}");
                
                var loginResponse = System.Text.Json.JsonSerializer.Deserialize<IdentityLoginResponse>(responseContent);
                if (loginResponse?.accessToken != null)
                {
                    Console.WriteLine($"🔑 Token received: {loginResponse.accessToken.Substring(0, Math.Min(20, loginResponse.accessToken.Length))}...");
                    return loginResponse.accessToken;
                }
                else
                {
                    Console.WriteLine($"❌ No access token in response: {responseContent}");
                    return string.Empty;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Login failed: {response.StatusCode} - {errorContent}");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting JWT token: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            return string.Empty;
        }
    }
}

// Identity Login Response DTO
public class IdentityLoginResponse
{
    public string accessToken { get; set; } = string.Empty;
    public string refreshToken { get; set; } = string.Empty;
    public DateTime expiresAt { get; set; }
}

// Login Response DTO
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// Device Notification DTO (backend'deki ile aynı)
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
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Register { get; set; }
    public int Value { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
