// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using FluentMigrator.Runner;

namespace BachatCommittee.Data.Migration.Test.Tests;

[TestClass]
public class MigrationTests
{

    IServiceProvider _serviceProvider = default!;

    [TestInitialize]
    public void TestIntitalize()
    {
        _serviceProvider = CreateServices(Settings.TestDbConnectionString);
    }

    private static ServiceProvider CreateServices(string connectionString)
    {
        //load the test assemblies so the fluentmigrator will be able to find the migrations
        AppDomain.CurrentDomain.Load(Constants.MIGRATIONS_NAMESPACE);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.IsDynamic == false).ToArray();

        return new ServiceCollection()
            // Add common FluentMigrator services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                // Add PostgreSQL support to FluentMigrator
                .AddPostgres()

                // Set the connection string
                .WithGlobalConnectionString(connectionString)
                // Define the assembly containing the migrations
                //.ScanIn(typeof(AddLogTable).Assembly).For.Migrations())
                .ScanIn(assemblies).For.Migrations().For.EmbeddedResources())

            // Enable logging to console in the FluentMigrator way
            .AddLogging(lb => lb.AddFluentMigratorConsole())

            // Build the service provider
            .BuildServiceProvider(false);
    }

    [TestMethod]
    public void MigrateUpDownTest()
    {
        //Arrange
        bool errorOccurred = false;
        string errorMessage = "";

        // Instantiate the runner
        var runner = _serviceProvider.GetRequiredService<IMigrationRunner>();

        //Act
        try
        {
            // Execute the migrations
            runner.MigrateUp();
            runner.MigrateDown(0);
        }
        catch (Exception ex)
        {
            errorMessage = ex.ToString();
            errorOccurred = true;
        }

        //Asset
        Assert.IsFalse(errorOccurred, errorMessage);

    }
}
