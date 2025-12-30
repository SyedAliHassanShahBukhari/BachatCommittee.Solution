// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BachatCommittee.Data.Migration.Test;

[TestClass]
public static class AssemblyStart
{
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        //dotnet test PathToYourTestProject.dll-- 'TestRunParameters.Parameter(name="YourTestProject.Args",value="ServiceConfig:BaseAddress=http://localhost:8080 ServiceConfig:MaxMemory=2048")'
        var dbServer = Environment.GetEnvironmentVariable(Constants.ENV_VAR_DEV_DB_SERVER);
        if (!string.IsNullOrEmpty(dbServer))
        {
            Settings.DbServer = dbServer;
        }
        else
        {
            Console.WriteLine($"Could not find environment variable '{Constants.ENV_VAR_DEV_DB_SERVER}'. Defaulting the database server name to localhost");
            Settings.DbServer = "localhost";
        }

        Settings.DatabaseName = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0}_{1}",
            Constants.DATABASE_BASE_NAME,
            DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture));

        Console.WriteLine($"Database Server: {Settings.DbServer}");
        Console.WriteLine($"Database: {Settings.DatabaseName}");

        Database.CreateDatabase(Settings.PostgresDbConnectionString, Settings.DatabaseName);
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        System.Diagnostics.Debug.WriteLine($"Database Name: {Settings.DatabaseName}");

        Database.DropDatabase(Settings.PostgresDbConnectionString, Settings.DatabaseName);
    }
}
