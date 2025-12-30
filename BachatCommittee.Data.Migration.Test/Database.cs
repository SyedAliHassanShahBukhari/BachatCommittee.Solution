// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Npgsql;

namespace BachatCommittee.Data.Migration.Test;

public class Database
{
    public static void CreateDatabase(string postgresDbConnectionString, string databaseName)
    {
        using var connection = new NpgsqlConnection(postgresDbConnectionString);
        connection.Open();

        // Escape database name to prevent SQL injection (PostgreSQL uses double quotes for identifiers)
        var escapedDatabaseName = $"\"{databaseName.Replace("\"", "\"\"")}\"";
        var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE {escapedDatabaseName}";
        cmd.ExecuteNonQuery();
    }

    public static void DropDatabase(string postgresDbConnectionString, string databaseName)
    {
        using var connection = new NpgsqlConnection(postgresDbConnectionString);
        connection.Open();

        // Terminate all connections to the database (using parameterized query for safety)
        var terminateCmd = connection.CreateCommand();
        terminateCmd.CommandText = @"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = $1
              AND pid <> pg_backend_pid()";
        terminateCmd.Parameters.Add(new NpgsqlParameter { Value = databaseName });
        terminateCmd.ExecuteNonQuery();

        // Drop the database (escape identifier)
        var escapedDatabaseName = $"\"{databaseName.Replace("\"", "\"\"")}\"";
        var dropCmd = connection.CreateCommand();
        dropCmd.CommandText = $"DROP DATABASE IF EXISTS {escapedDatabaseName}";
        dropCmd.ExecuteNonQuery();
    }

    public static void Execute(string connectionString, string sql)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
