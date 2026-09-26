using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace TRPO.ElectronicsStore.Data;

public static class DbFactory
{

    // Читает строку подключения из настроек и создаёт отдельный контекст для работы с базой.
    public static ShopContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("TRPO_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var localSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
            var settingsPath = File.Exists(localSettingsPath) ? localSettingsPath : Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            try
            {
                using var settingsFile = JsonDocument.Parse(File.ReadAllText(settingsPath));
                connectionString = settingsFile.RootElement.GetProperty("ConnectionString").GetString();
            }
            catch (Exception error) when (error is IOException or JsonException or KeyNotFoundException)
            {
                throw new InvalidOperationException("Не удалось прочитать настройки подключения. Проверьте appsettings.json.");
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Укажите строку подключения к базе данных в appsettings.json.");
        var options = new DbContextOptionsBuilder<ShopContext>().UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(130)).Options;
        return new ShopContext(options);
    }
}
