using System.Data.Common;
using Hems_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Respawn;

namespace Hems_Backend.Application.FunctionalTests;

public class PostgreSQLTestDatabase : ITestDatabase
{
    private readonly string _connectionString = null!;
    private NpgsqlConnection _connection = null!;
    private Respawner _respawner = null!;

    public PostgreSQLTestDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        // Test için ayrı database kullan
        var baseConnectionString = configuration.GetConnectionString("Hems_BackendDb");
        Guard.Against.Null(baseConnectionString);

        // Test database adını unique yap
        var testDbName = $"HEMS_TEST_{Guid.NewGuid():N}";
        _connectionString = baseConnectionString.Replace("Database=HEMS", $"Database={testDbName}");
    }

    public async Task InitialiseAsync()
    {
        // Önce test database'i oluştur
        await CreateTestDatabaseAsync();

        _connection = new NpgsqlConnection(_connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var context = new ApplicationDbContext(options);

        // Test database'e migrate et
        context.Database.Migrate();

        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
        await _connection.CloseAsync();
    }

    public DbConnection GetConnection()
    {
        return _connection;
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }

    public async Task ResetAsync()
    {
        await _connection.OpenAsync();
        await _respawner.ResetAsync(_connection);
        await _connection.CloseAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        // Test database'i sil
        await DropTestDatabaseAsync();
    }

    private async Task CreateTestDatabaseAsync()
    {
        // Master database'e bağlan
        var masterConnectionString = _connectionString.Replace($"Database={_connectionString.Split(';').FirstOrDefault(s => s.Contains("Database="))?.Split('=')[1]}", "Database=postgres");
        
        using var masterConnection = new NpgsqlConnection(masterConnectionString);
        await masterConnection.OpenAsync();

        var dbName = _connectionString.Split(';').FirstOrDefault(s => s.Contains("Database="))?.Split('=')[1];
        
        // Test database'i oluştur
        var createDbCommand = $"CREATE DATABASE \"{dbName}\"";
        using var command = new NpgsqlCommand(createDbCommand, masterConnection);
        await command.ExecuteNonQueryAsync();
        
        await masterConnection.CloseAsync();
    }

    private async Task DropTestDatabaseAsync()
    {
        try
        {
            // Master database'e bağlan
            var masterConnectionString = _connectionString.Replace($"Database={_connectionString.Split(';').FirstOrDefault(s => s.Contains("Database="))?.Split('=')[1]}", "Database=postgres");
            
            using var masterConnection = new NpgsqlConnection(masterConnectionString);
            await masterConnection.OpenAsync();

            var dbName = _connectionString.Split(';').FirstOrDefault(s => s.Contains("Database="))?.Split('=')[1];
            
            // Test database'i sil
            var dropDbCommand = $"DROP DATABASE IF EXISTS \"{dbName}\"";
            using var command = new NpgsqlCommand(dropDbCommand, masterConnection);
            await command.ExecuteNonQueryAsync();
            
            await masterConnection.CloseAsync();
            
            Console.WriteLine($"✅ Test database silindi: {dbName}");
        }
        catch (Exception ex)
        {
            // Test database silme hatası önemli değil, log'la ve devam et
            Console.WriteLine($"❌ Test database silme hatası: {ex.Message}");
        }
    }
}
