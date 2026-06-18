using Microsoft.AspNetCore.HttpOverrides;
using Polly;
using QuakePulse_WebService.Caching;
using QuakePulse_WebService.Integration;
using QuakePulse_WebService.Mappers.Profiles;
using QuakePulse_WebService.Orchestration;
using QuakePulse_WebService.Services;
using Serilog;
using StackExchange.Redis;

internal class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, config) =>
                config.ReadFrom.Configuration(context.Configuration)
                      .ReadFrom.Services(services)
                      .Enrich.FromLogContext());

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            builder.Services.AddApplicationInsightsTelemetry(
                options =>
                {
                    options.EnableAdaptiveSampling = true;
                });

            builder.Services.AddProblemDetails();
            builder.Services.AddHealthChecks();

            var usgsApiBaseAddress = builder.Configuration["ExternalApis:UsgsEarthquakeApi:BaseAddress"]
                ?? throw new InvalidOperationException("USGS API base address is not configured.");

            builder.Services.AddControllers();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularApp", policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
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
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            var cacheSettings = builder.Configuration.GetSection("CacheSettings").Get<CacheSettings>();

            if (cacheSettings?.Enabled == true)
            {
                builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
                {
                    var redisConfig = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
                    return ConnectionMultiplexer.Connect(redisConfig);
                });

                builder.Services.AddScoped<ICacheService, RedisCacheService>();
            }
            else
            {
                builder.Services.AddScoped<ICacheService, NullCacheService>();
            }

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddAutoMapper(typeof(EarthquakeProfile));

            var app = builder.Build();

            app.UseForwardedHeaders();
            app.UseSerilogRequestLogging();
            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAngularApp");
            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks("/api/health");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}