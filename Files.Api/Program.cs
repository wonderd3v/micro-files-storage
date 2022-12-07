using Files.Application.BlobStorage.Interfaces;
using Files.Infrastructure.Common;
using Files.Infrastructure.Services;
using Microsoft.OpenApi.Models;
using Serilog;

StaticLogger.EnsureInitialized();
Log.Information("Azure Storage API Booting Up...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, config) =>
    {
        config.WriteTo.Console()
        .ReadFrom.Configuration(builder.Configuration);
    });

    builder.Services.AddControllers(options => options.EnableEndpointRouting = true);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Files Microservice", Version = "v1" }));

    builder.Services.AddTransient<IBlobStorage, BlobStorage>();
    Log.Information("Services has been successfully added...");

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "HealthChecks v1");
    });

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();

    app.UseEndpoints(endPoints =>
    {
        endPoints.MapControllers();
    });

    app.Run();
    Log.Information("API is now ready to serve files to and from Azure Cloud Storage...");
}
catch (Exception ex) when (!ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
{
    StaticLogger.EnsureInitialized();
    Log.Fatal(ex, "Unhandled Exception");
}
finally
{
    StaticLogger.EnsureInitialized();
    Log.Information("Azure Storage API Shutting Down...");
    Log.CloseAndFlush();
}