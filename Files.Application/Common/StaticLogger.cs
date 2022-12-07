using Serilog;
using Serilog.Core;

namespace Files.Infrastructure;

public class StaticLogger
{
    public static void EnsureInitialized()
    {
        if (Log.Logger is not Logger)
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug().Enrich.FromLogContext()
               .WriteTo.Console()
               .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
               .CreateLogger();
        }
    }
}