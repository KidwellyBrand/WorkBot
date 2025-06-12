using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using WorkBot;
using WorkBot.Settings;
using WorkBot.Storage;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;

NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

/*
 * Для сборки DEB-пакета использовать команду:
 * dotnet msbuild /t:CreateDeb /p:PackageDir=debian /p:WarningLevel=0
 */

// Выполнение миграции базы данных
using (var db = new DB())
{
    db.Database.Migrate();
    log.Info("База данных инициализирована");
}


// Чтение из файла конфигурации токен доступа к Telegram API
string token = Config.Get("Token");

if (string.IsNullOrEmpty(token))
{
    throw new Exception("Не задан параметр конфигурации 'Token'");
}

Bot bot = new(token);
bot.Start();

// Возможность запуска как службы Windows
IHostBuilder builder = Host.CreateDefaultBuilder(args).UseWindowsService();

// Настройка протоколирования на NLog
builder.ConfigureLogging( options =>
{
    options.ClearProviders();
    options.AddNLog();
});

IHost host = builder.Build();

await host.RunAsync();
