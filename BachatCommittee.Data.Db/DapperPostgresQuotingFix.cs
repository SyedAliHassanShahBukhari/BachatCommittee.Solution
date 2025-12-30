// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Db;

public static class DapperPostgresQuotingFix
{
    public static void EnableQuotedIdentifiers()
    {
        // This tells Dapper.Contrib how to resolve table names globally
        SqlMapperExtensions.TableNameMapper = type =>
        {
            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            string rawName = tableAttr?.Name ?? type.Name + "s";

            // Remove any extra quotes that might already exist
            rawName = rawName.Replace("\"", "", StringComparison.OrdinalIgnoreCase);

            // If schema included (e.g., dbo.BidTerms)
            if (rawName.Contains('.', StringComparison.OrdinalIgnoreCase))
            {
                var parts = rawName.Split('.', 2);
                string schema = parts[0];
                string table = parts[1];
                return $"\"{schema}\".\"{table}\"";
            }

            // Otherwise, just quote the table name
            return $"\"{rawName}\"";
        };
    }
}
