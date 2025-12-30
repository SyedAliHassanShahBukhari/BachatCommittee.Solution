// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Migrators;

namespace BachatCommittee.Data.Migrator;

public class Program
{
    private const string MIGRATIONS_NAMESPACE = "BachatCommittee.Data.Migrations";
    private const string APP_NAME = "BachatCommittee.Data.Migrator";
    public static void Main(string[] args)
    {
        PostgreSqlMigrator.Migrator(args, MIGRATIONS_NAMESPACE, APP_NAME);
    }
}
