using Hems_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hems_Backend.Application.FunctionalTests;

using static Testing;

[TestFixture]
public abstract class BaseTestFixture
{
    [SetUp]
    public async Task TestSetUp()
    {
        // Her test öncesi sadece test verilerini temizle
        await CleanupTestData();
    }

    [TearDown]
    public async Task TestTearDown()
    {
        // Her test sonrası sadece test verilerini temizle
        await CleanupTestData();
    }

    private async Task CleanupTestData()
    {
        // Artık test database kullanıyoruz, temizlik gerekmiyor!
        // Test database her test için ayrı oluşturuluyor ve test sonunda siliniyor
        Console.WriteLine("🧹 Test database kullanılıyor - temizlik gerekmiyor!");
        await Task.CompletedTask;
    }
}
