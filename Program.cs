
using Microsoft.Extensions.Configuration;
using Polly;
using QuakePulse_WebService.Caching;
using QuakePulse_WebService.Integration;
using QuakePulse_WebService.Mappers.Profiles;
using QuakePulse_WebService.Orchestration;
using QuakePulse_WebService.Services;
using Serilog;
using StackExchange.Redis;
using System.Configuration;
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure logging based on environment
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Logging.AddDebug();
        }
        else
        {
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
        }

        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
        logger.LogInformation("=== Starting QuakePulse WebService Application ===");
        logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

        var usgsApiBaseAddress = builder.Configuration["ExternalApis:UsgsEarthquakeApi:BaseAddress"]
            ?? throw new InvalidOperationException("USGS API base address is not configured.");

        // Add services to the container
        builder.Services.AddControllers();

        // CORS — allow WebUI dev origins
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngularApp", policy =>
            {
             policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            //policy.WithOrigins(
            //        "http://localhost:4201",
            //        "https://localhost:4201")
            //      .AllowAnyHeader()
            //      .AllowAnyMethod();
        });
        });

        builder.Services.AddScoped<IEarthquakeOrchestrator, EarthquakeOrchestrator>();
        builder.Services.AddScoped<IEarthquakeService, EarthquakeService>();

        builder.Services.AddHttpClient<IUsgsApiService, UsgsApiService>(client =>
        {
            client.BaseAddress = new Uri(usgsApiBaseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(3, retryAttempt =>
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    return delay;
                }));

        // Read CacheSettings from appsettings.json
        var cacheSettings = builder.Configuration
            .GetSection("CacheSettings")
            .Get<CacheSettings>();

        
        if (cacheSettings?.Enabled == true)
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var serviceLogger = sp.GetRequiredService<ILogger<Program>>();
                var redisConfig = builder.Configuration.GetSection("Redis")["ConnectionString"];

                try
                {
                    serviceLogger.LogInformation("Connecting to Redis: {RedisConnection}",
                        redisConfig?.Replace("password=", "password=***") ?? "localhost:6379");
                    return ConnectionMultiplexer.Connect(redisConfig ?? "localhost:6379");
                }
                catch (Exception ex)
                {
                    serviceLogger.LogError(ex, "Failed to connect to Redis");
                    throw;
                }
            });

            builder.Services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            builder.Services.AddScoped<ICacheService, NullCacheService>();
        }
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddAutoMapper(typeof(EarthquakeProfile));



        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration);
        });

        var app = builder.Build();

        // Log configuration at startup
        var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
        appLogger.LogInformation("Application configured for environment: {Environment}", app.Environment.EnvironmentName);
        appLogger.LogInformation("Redis TTL (minutes): {TTL}", builder.Configuration.GetValue<int>("Redis:DefaultTTLMinutes"));

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            appLogger.LogInformation("Development environment - enabling Swagger and detailed logging");
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            appLogger.LogInformation("Production environment - Swagger disabled, minimal logging active");
        }

        app.UseHttpsRedirection();
        //app.UseCors("WebUI");
        app.UseCors("AllowAngularApp");
        app.UseAuthorization();
        app.MapControllers();

        appLogger.LogInformation("=== QuakePulse WebService Application Started Successfully ===");
        app.Run();

        appLogger.LogInformation("=== Shutting down QuakePulse WebService Application ===");
    }
}