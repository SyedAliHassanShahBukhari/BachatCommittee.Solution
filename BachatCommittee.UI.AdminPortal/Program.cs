// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Interfaces;
using awisk.common.ServiceCollection;
using awisk.common.Services;
using BachatCommittee.Data.Db.ServiceCollectionExt;
using BachatCommittee.Models.Classes;
using BachatCommittee.ServiceCollection;

namespace BachatCommittee.UI.AdminPortal;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        AppSettings settings = new();
        builder.Configuration.Bind(settings);
        settings.ConnectionString = builder.Configuration.GetConnectionString("ConnectionString")!;
        builder.Services.AddSingleton(settings);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddHttpClient<IApiService, ApiService>(client =>
        {
            client.BaseAddress = new Uri($"{settings.ApiSettings.BaseUrl}/api/v1/");
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddBachatCommitteeRepos(settings.ConnectionString);
        builder.Services.AddBachatCommitteeServicesWebPortal();
        builder.Services.AddDefaultAuthentication(settings.Auth, settings.JwtSettings);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(settings.ApiSettings.AllowedOrigins) // or whatever your frontend origin is
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseCors("AllowFrontend");

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.Run();
    }
}
