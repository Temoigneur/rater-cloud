using System.Net;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rater.Services;
using SharedModels.Validators;
using SpotifyAPI.Web;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection; // For AddPolicyHandler extension
using Polly; // For Policy class
using Polly.Extensions.Http; // For HandleTransientHttpError()

namespace Rater
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Clear existing configuration
            builder.Configuration.Sources.Clear();
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Determine port based on service name
            int port = 5235; // Default to Rater port
            if (typeof(Program).Assembly.GetName().Name == "Evals")
                port = 5236;
            else if (typeof(Program).Assembly.GetName().Name == "Perplexity")
                port = 5237;

            // Use Kestrel
            builder.WebHost.UseKestrel()
                .ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(port);
                });

            // Add global HTTP client configuration to fix socket initialization issues
            builder.Services.AddHttpClient();

            // Configure Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Add services to the container
            builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new DefaultNamingStrategy()
                    };
                    options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                })
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<OutputRequestValidator>());

            // Add Distributed Memory Cache for session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddMemoryCache();

            // Configure TLS versions explicitly to ensure compatibility with Spotify's servers
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            // Register SpotifyClient separately
            builder.Services.AddSingleton<ISpotifyClient>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var clientId = configuration["Spotify:ClientId"];
                var clientSecret = configuration["Spotify:ClientSecret"];

                var config = SpotifyClientConfig.CreateDefault()
                    .WithAuthenticator(new ClientCredentialsAuthenticator(clientId, clientSecret));

                return new SpotifyClient(config);
            });

            // Register the SpotifyPlayCountService as both ISpotifyPlayCountService and IApifyService
            builder.Services.AddHttpClient<SpotifyPlayCountService>()
                .AddPolicyHandler(GetRetryPolicy())
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 10,
                    EnableMultipleHttp2Connections = true,
                    ConnectTimeout = TimeSpan.FromSeconds(30)
                });
            builder.Services.AddSingleton<SpotifyPlayCountService>();
            builder.Services.AddSingleton<ISpotifyPlayCountService>(provider => provider.GetRequiredService<SpotifyPlayCountService>());
            builder.Services.AddSingleton<IApifyService>(provider => provider.GetRequiredService<SpotifyPlayCountService>());

            // Keep ApifyService registered for backward compatibility (but don't use it as IApifyService)
            builder.Services.AddHttpClient<ApifyService>()
                .AddPolicyHandler(GetRetryPolicy())
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 10,
                    EnableMultipleHttp2Connections = true,
                    ConnectTimeout = TimeSpan.FromSeconds(30)
                });
            builder.Services.AddSingleton<ApifyService>();

            // Then register SpotifyService which depends on IApifyService
            builder.Services.AddSingleton<ISpotifyService, SpotifyService>();

            // Finally register OpenAIService which depends on ISpotifyService
            builder.Services.AddScoped<IOpenAIService, OpenAIService>();

            // Register RaterPerplexityService for more accurate intent determination
            builder.Services.AddScoped<IPerplexityService, RaterPerplexityService>();

            // Register HTTP clients for Spotify APIs
            builder.Services.AddHttpClient("SpotifyWeb", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHttpClient("SpotifyPrivate", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Register Session Services
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            // Configure CORS specifically for the external frontend
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ExternalFrontend", corsBuilder =>
                {
                    corsBuilder.WithOrigins(
                            "http://localhost:8080",
                            "http://127.0.0.1:8080",
                            "http://localhost:5235",
                            "http://localhost:5173", // Add any additional origins your Rater frontend might use
                            "http://localhost:8084"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // Add Swagger for API documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rater API", Version = "v1" });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rater API v1"));
            }

            app.UseRouting();

            // Apply CORS policy before routing endpoints with CORS metadata.
            app.UseCors("ExternalFrontend");

            app.UseAuthorization();
            app.UseSession();

            // Configure static files to prevent caching in development
            if (app.Environment.IsDevelopment())
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        // Disable caching for all static files
                        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                        ctx.Context.Response.Headers.Append("Expires", "0");
                    }
                });
            }
            else
            {
                app.UseStaticFiles();
            }

            // Map controllers for API endpoints.
            app.MapControllers();

            // Fallback for SPA - should be last.
            app.MapFallbackToFile("index.html");

            app.Run();
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            // Retry policy with exponential backoff for transient errors (e.g., 5xx or timeouts)
            return HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests) // Handle rate-limiting (429)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
