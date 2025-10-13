using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRTester;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔔 SignalR Device Notification Tester");
        Console.WriteLine("=====================================");

        // SignalR Hub URL'i
        var hubUrl = "https://localhost:5001/deviceNotificationHub";
        
        // Hub connection oluştur
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // JWT token ile authentication 
                options.AccessTokenProvider = async () =>
                {
                    // Burada JWT token alman gerekiyor
                    // Örnek: User login API'sinden token al
                    return await GetJwtTokenAsync();
                };
            })
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
            await connection.StartAsync();
            Console.WriteLine("✅ Connected to SignalR Hub!");

            // Site group'una katıl (örnek site ID)
            var siteId = Guid.Parse("30745101-afa3-4875-968f-f39f49ce4cdd"); // Örnek site ID
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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
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
            using var httpClient = new HttpClient();
            var loginUrl = "https://localhost:5001/api/Users/login";
            
            // Örnek login request (gerçek credentials ile değiştir)
            var loginRequest = new
            {
                email = "administrator@localhost", // Gerçek email
                password = "***REMOVED***"    // Gerçek password
            };

            var json = System.Text.Json.JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(loginUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = System.Text.Json.JsonSerializer.Deserialize<IdentityLoginResponse>(responseContent);
                return loginResponse?.accessToken ?? string.Empty;
            }
            
            Console.WriteLine($"❌ Login failed: {response.StatusCode}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting JWT token: {ex.Message}");
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
