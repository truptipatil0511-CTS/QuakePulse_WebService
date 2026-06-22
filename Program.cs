using Microsoft.AspNetCore.HttpOverrides;
using Polly;
using QuakePulse_WebService.Caching;
using QuakePulse_WebService.Common;
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

            // Bind strongly-typed settings
            builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
            builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection("Cors"));
            builder.Services.Configure<CorrelationIdSettings>(builder.Configuration.GetSection("CorrelationId"));
            builder.Services.Configure<HealthCheckSettings>(builder.Configuration.GetSection("HealthCheck"));
            builder.Services.Configure<EarthquakeDefaults>(builder.Configuration.GetSection("EarthquakeDefaults"));

            var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();
            var corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
            var healthCheckSettings = builder.Configuration.GetSection("HealthCheck").Get<HealthCheckSettings>() ?? new HealthCheckSettings();

            var usgsApiBaseAddress = builder.Configuration["ExternalApis:UsgsEarthquakeApi:BaseAddress"]
                ?? throw new InvalidOperationException("USGS API base address is not configured.");

            builder.Services.AddControllers();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(corsSettings.PolicyName, policy =>
                {
                    if (corsSettings.AllowedOrigins is { Length: > 0 })
                        policy.WithOrigins(corsSettings.AllowedOrigins).AllowAnyHeader().AllowAnyMethod();
                    else
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            builder.Services.AddScoped<IEarthquakeOrchestrator, EarthquakeOrchestrator>();
            builder.Services.AddScoped<IEarthquakeService, EarthquakeService>();

            builder.Services.AddHttpClient<IUsgsApiService, UsgsApiService>(client =>
            {
                client.BaseAddress = new Uri(usgsApiBaseAddress);
                client.Timeout = TimeSpan.FromSeconds(apiSettings.HttpClientTimeoutSeconds);
            })
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(apiSettings.RetryAttempts, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            var cacheSettings = builder.Configuration.GetSection("CacheSettings").Get<CacheSettings>();

            if (cacheSettings?.Enabled == true)
            {
                builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<Program>>();
                    var connStr = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";

                    var options = ConfigurationOptions.Parse(connStr);
                    options.AbortOnConnectFail = builder.Configuration.GetValue("Redis:AbortOnConnectFail", false);
                    options.ConnectRetry = builder.Configuration.GetValue("Redis:ConnectRetry", 3);
                    options.ConnectTimeout = builder.Configuration.GetValue("Redis:ConnectTimeoutMs", 5000);
                    options.Ssl = connStr.Contains("6380") || connStr.Contains("ssl=true", StringComparison.OrdinalIgnoreCase);

                    logger.LogInformation("Connecting to Redis: {Endpoint} (SSL: {Ssl})",
                        options.EndPoints.FirstOrDefault()?.ToString() ?? connStr, options.Ssl);

                    return ConnectionMultiplexer.Connect(options);
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
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseSerilogRequestLogging();
            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors(corsSettings.PolicyName);
            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks(healthCheckSettings.Path);

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