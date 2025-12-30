// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Entities;

/// <summary>
/// Represents a discovered controller action in the system.
/// </summary>
[Table("Actions")]
public class ActionEntity : BaseGuidEntity
{
    public string ControllerName { get; set; } = string.Empty;

    public string ActionName { get; set; } = string.Empty;

    public string HttpMethod { get; set; } = string.Empty; // GET, POST, PUT, DELETE, PATCH

    public string? Route { get; set; }

    public string? Description { get; set; }
}

