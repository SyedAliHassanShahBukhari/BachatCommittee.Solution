// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Npgsql;

namespace BachatCommittee.Data.Repos.Base;

/// <summary>
/// Optimized repository base class for PostgreSQL with full CancellationToken support.
/// Provides async methods with proper cancellation token propagation and optimized database access.
/// </summary>
public abstract class RepositoryBasePostgreSqlOptimized(string connectionString)
{
    protected readonly string ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

    /// <summary>
    /// Creates and opens a new database connection.
    /// </summary>
    protected NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }

    /// <summary>
    /// Executes a query and returns the results as an enumerable.
    /// </summary>
    protected async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QueryAsync<T>(
            new CommandDefinition(
                sql,
                parameters,
                commandType: commandType,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns the first result or default value.
    /// </summary>
    protected async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(
                sql,
                parameters,
                commandType: commandType,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns a single result or throws if no result.
    /// </summary>
    protected async Task<T> QuerySingleAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleAsync<T>(
            new CommandDefinition(
                sql,
                parameters,
                commandType: commandType,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns a single result or default, throws if more than one result.
    /// </summary>
    protected async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<T>(
            new CommandDefinition(
                sql,
                parameters,
                commandType: commandType,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a command and returns the number of affected rows.
    /// </summary>
    protected async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                commandType: commandType,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an entity by its primary key using Dapper.Contrib.
    /// </summary>
    protected async Task<T?> GetByIdAsync<T, TKey>(
        TKey id,
        CancellationToken cancellationToken = default) where T : class
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Dapper.Contrib doesn't have built-in cancellation token support
        // So we use a workaround with CommandDefinition
        var tableName = GetTableName<T>();
        var keyProperty = GetKeyProperty<T>();
        var sql = $"SELECT * FROM \"{tableName}\" WHERE \"{keyProperty}\" = @Id";

        return await connection.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Inserts an entity using Dapper.Contrib and returns the entity with generated ID.
    /// Note: Dapper.Contrib.InsertAsync doesn't support CancellationToken, so we wrap it.
    /// </summary>
    protected async Task<T> InsertAsync<T>(
        T entity,
        CancellationToken cancellationToken = default) where T : class
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Dapper.Contrib.InsertAsync doesn't support CancellationToken directly
        // Wrap it to support cancellation
        var id = await Task.Run(
            async () => await SqlMapperExtensions.InsertAsync(connection, entity).ConfigureAwait(false),
            cancellationToken)
            .ConfigureAwait(false);

        // Set the ID on the entity if it's a numeric ID
        var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("LogId");
        if (idProperty != null && idProperty.CanWrite)
        {
            var idValue = Convert.ChangeType(id, idProperty.PropertyType, System.Globalization.CultureInfo.InvariantCulture);
            idProperty.SetValue(entity, idValue);
        }

        return entity;
    }

    /// <summary>
    /// Inserts multiple entities using Dapper.Contrib.
    /// </summary>
    protected async Task InsertAsync<T>(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default) where T : class
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Insert entities one by one to ensure cancellation token support
        foreach (var entity in entities)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InsertAsync(entity, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Updates an entity using Dapper.Contrib.
    /// Note: Dapper.Contrib.UpdateAsync returns bool, doesn't support CancellationToken directly.
    /// </summary>
    protected async Task<bool> UpdateAsync<T>(
        T entity,
        CancellationToken cancellationToken = default) where T : class
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Dapper.Contrib.UpdateAsync doesn't support CancellationToken directly, returns bool
        // Wrap it to support cancellation
        return await Task.Run(
            async () => await SqlMapperExtensions.UpdateAsync(connection, entity).ConfigureAwait(false),
            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an entity using Dapper.Contrib.
    /// Note: Dapper.Contrib.DeleteAsync doesn't support CancellationToken directly.
    /// </summary>
    protected async Task<bool> DeleteAsync<T>(
        T entity,
        CancellationToken cancellationToken = default) where T : class
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Dapper.Contrib.DeleteAsync doesn't support CancellationToken directly
        // Wrap it to support cancellation
        return await Task.Run(
            async () => await SqlMapperExtensions.DeleteAsync(connection, entity).ConfigureAwait(false),
            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the table name for an entity type using Dapper.Contrib attributes.
    /// </summary>
    private static string GetTableName<T>()
    {
        var type = typeof(T);
        var tableAttr = (Dapper.Contrib.Extensions.TableAttribute?)type
            .GetCustomAttributes(typeof(Dapper.Contrib.Extensions.TableAttribute), true)
            .FirstOrDefault();

        return tableAttr?.Name ?? type.Name;
    }

    /// <summary>
    /// Gets the key property name for an entity type.
    /// </summary>
    private static string GetKeyProperty<T>()
    {
        var type = typeof(T);
        var keyProperty = type.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttributes(typeof(KeyAttribute), true).Length != 0)
            ?? type.GetProperties().FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            ?? type.GetProperties().FirstOrDefault(p => p.Name.Equals($"{type.Name}Id", StringComparison.OrdinalIgnoreCase));

        return keyProperty?.Name ?? "Id";
    }

    // Synchronous methods for backward compatibility (kept but marked obsolete)
    [Obsolete("Use QueryAsync instead")]
    protected IEnumerable<T> Query<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text)
    {
        using var connection = CreateConnection();
        connection.Open();
        return connection.Query<T>(sql, parameters, commandType: commandType);
    }

    [Obsolete("Use QueryFirstOrDefaultAsync instead")]
    protected T? QueryFirstOrDefault<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text)
    {
        using var connection = CreateConnection();
        connection.Open();
        return connection.QueryFirstOrDefault<T>(sql, parameters, commandType: commandType);
    }

    [Obsolete("Use GetByIdAsync instead")]
    protected T? GetById<T, TKey>(TKey id) where T : class
    {
        using var connection = CreateConnection();
        connection.Open();
        return SqlMapperExtensions.Get<T>(connection, id);
    }

    [Obsolete("Use InsertAsync instead")]
    protected T Insert<T>(T entity) where T : class
    {
        using var connection = CreateConnection();
        connection.Open();
        var id = SqlMapperExtensions.Insert(connection, entity);

        var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("LogId");
        if (idProperty != null && idProperty.CanWrite)
        {
            var idValue = Convert.ChangeType(id, idProperty.PropertyType, System.Globalization.CultureInfo.InvariantCulture);
            idProperty.SetValue(entity, idValue);
        }

        return entity;
    }

    [Obsolete("Use InsertAsync instead")]
    protected void Insert<T>(IEnumerable<T> entities) where T : class
    {
        using var connection = CreateConnection();
        connection.Open();
        foreach (var entity in entities)
        {
            SqlMapperExtensions.Insert(connection, entity);
        }
    }

    [Obsolete("Use UpdateAsync instead")]
    protected bool Update<T>(T entity) where T : class
    {
        using var connection = CreateConnection();
        connection.Open();
        return SqlMapperExtensions.Update(connection, entity);
    }

    [Obsolete("Use DeleteAsync instead")]
    protected bool Delete<T>(T entity) where T : class
    {
        using var connection = CreateConnection();
        connection.Open();
        return SqlMapperExtensions.Delete(connection, entity);
    }
}

