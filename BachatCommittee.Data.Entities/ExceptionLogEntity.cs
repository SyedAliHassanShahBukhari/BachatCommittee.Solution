// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Entities;

[Table("ExceptionLogs")]
public class ExceptionLogEntity
{
    [Key]
    public long LogId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
