// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Data.Db.Interfaces;

namespace BachatCommittee.Data.Db.ServiceCollectionExt;

public class RepositorySettings(string connectionString) : IRepositorySettings
{
    public string ConnectionString { get; set; } = connectionString;
}
