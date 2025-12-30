// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BachatCommittee.Data.Migration.Test;

public class Settings
{
    public static string DbServer { get; set; } = "localhost";
    public static string DbPort { get; set; } = "5432";
    public static string DbUsername { get; set; } = "postgres";
    public static string DbPassword { get; set; } = "sunbonn";
    public static string DatabaseName { get; set; } = "BachatCommittee_TestDb";

    public static string PostgresDbConnectionString
    {
        get { return $"Host={DbServer};Port={DbPort};Database=postgres;Username={DbUsername};Password={DbPassword}"; }
    }

    public static string TestDbConnectionString
    {
        get { return $"Host={DbServer};Port={DbPort};Database={DatabaseName};Username={DbUsername};Password={DbPassword}"; }
    }
}
