// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Threading.RateLimiting;
using awisk.common.Interfaces;
using awisk.common.ServiceCollection;
using awisk.common.Services;
using BachatCommittee.API.Authorization;
using BachatCommittee.API.Services;
using BachatCommittee.Data.Db;
using BachatCommittee.Data.Db.ServiceCollectionExt;
using BachatCommittee.Models.Classes;
using BachatCommittee.ServiceCollection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

namespace BachatCommittee.API;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        try
        {
            Log.Information("Starting BachatCommittee API");

            var builder = WebApplication.CreateBuilder(args);

            // Use Serilog for logging
            builder.Host.UseSerilog();

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.InitSwaggerGenWithToken(new()
            {
                Title = "BachatCommittee",
                Version = "v1",
                ContactName = "awisk Team",
                ContactEmail = "hello@awisk.com",
                ContactUrl = "https://awisk.com/contact.html"
            });

            AppSettings settings = new();
            builder.Configuration.Bind(settings);
            settings.ConnectionString = builder.Configuration.GetConnectionString("ConnectionString")!;

            // Register as both AppSettings AND ApplicationSettings
            builder.Services.AddSingleton(settings);

            builder.Services.AddPostgreSqlDbContextDefault<AppDbContext, AppUser>(settings.ConnectionString, "BachatCommittee.API");
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultAuthenticationForApi(settings.JwtSettings);

            // Add authorization with permission handler and policy provider
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            builder.Services.AddAuthorization();

            builder.Services.AddHttpClient<IApiService, ApiService>();
            builder.Services.AddHttpContextAccessor();

            // Add memory caching
            builder.Services.AddMemoryCache();

            // Add health checks
            builder.Services.AddHealthChecks()
                .AddNpgSql(
                    connectionString: settings.ConnectionString,
                    name: "postgresql",
                    tags: ["ready", "db", "postgresql"]);

            // Add rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                // Global rate limiter: 100 requests per minute per user/connection
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.Id,
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                // Policy for authenticated users with higher limits
                options.AddPolicy("authenticated", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.Id,
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 200,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            builder.Services.AddBachatCommitteeRepos(settings.ConnectionString);
            builder.Services.AddBachatCommitteeServicesAPI();
            DapperPostgresQuotingFix.EnableQuotedIdentifiers();

            // Register action discovery startup service
            builder.Services.AddHostedService<ActionDiscoveryStartupService>();

            // CORS
            builder.Services.AddCors(opts =>
            {
                opts.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins([

                        "http://localhost:5173",
                        "https://localhost:5173",
                        "https://localhost:7266",
                        "https://localhost:7215",
                        "https://localhost:7143",
                        "http://localhost:5209",
                        "https://localhost:7159",
                        "http://localhost:5237"
                    ])
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (settings.ApiSettings.ShowOpenApiDocs)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseCors("AllowFrontend");
            app.UseHttpsRedirection();

            // Enable rate limiting (must be after UseRouting and before UseAuthentication)
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseStaticFiles();

            // Add health check endpoints
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(x => new
                        {
                            name = x.Key,
                            status = x.Value.Status.ToString(),
                            exception = x.Value.Exception?.Message,
                            duration = x.Value.Duration.ToString()
                        }),
                        totalDuration = report.TotalDuration.ToString()
                    });
                    await context.Response.WriteAsync(result).ConfigureAwait(true);
                }
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false // Live endpoint returns 200 if the API is running
            });

            app.MapControllers();

            Log.Information("BachatCommittee API started successfully");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "BachatCommittee API terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
