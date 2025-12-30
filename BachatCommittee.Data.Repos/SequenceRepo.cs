// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Data.Db;
using awisk.common.Data.Db.Interfaces;
using BachatCommittee.Data.Repos.Interfaces;

namespace BachatCommittee.Data.Repos;

public class SequenceRepo(IRepositorySettings repositorySettings) : RepositoryBasePostgreSql(repositorySettings.ConnectionString), ISequenceRepo
{
    private static readonly HashSet<string> AllowedSequences = new(StringComparer.OrdinalIgnoreCase)
    {
        "UserNumberSequence"
        // Add other allowed sequences here
    };

    public async Task<long> GetNextSequenceValueAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        var cleanName = sequenceName.Trim('"');
        if (!AllowedSequences.Contains(cleanName))
        {
            throw new ArgumentException($"Sequence '{cleanName}' is not allowed.", nameof(sequenceName));
        }

        var sql = $"SELECT nextval('\"{cleanName}\"');";
        var result = await QueryFirstOrDefaultAsync<long?>(sql, null, System.Data.CommandType.Text).ConfigureAwait(false);
        return result ?? 0;
    }

    public async Task<string> GetUserSequenceValueAsync(string sequenceName = "UserNumberSequence", CancellationToken cancellationToken = default)
    {
        var seqValue = await GetNextSequenceValueAsync(sequenceName).ConfigureAwait(false);
        var code = $"Usr{DateTime.UtcNow:yyMM}{seqValue}";
        return code;
    }
}
