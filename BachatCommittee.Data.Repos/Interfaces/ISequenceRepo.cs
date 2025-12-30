// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BachatCommittee.Data.Repos.Interfaces;

public interface ISequenceRepo
{
    Task<long> GetNextSequenceValueAsync(string sequenceName, CancellationToken cancellationToken = default);
    Task<string> GetUserSequenceValueAsync(string sequenceName = "UserNumberSequence", CancellationToken cancellationToken = default);
}

