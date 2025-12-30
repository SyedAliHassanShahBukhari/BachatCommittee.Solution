// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BachatCommittee.Models.DTOs.Responses;

public class PoolResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? TimeZone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
}
