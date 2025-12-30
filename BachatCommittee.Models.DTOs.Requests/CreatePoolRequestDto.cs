// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BachatCommittee.Models.DTOs.Requests;

public class CreatePoolRequestDto
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Code { get; set; } = string.Empty;

    [StringLength(80)]
    public string? TimeZone { get; set; }
}
