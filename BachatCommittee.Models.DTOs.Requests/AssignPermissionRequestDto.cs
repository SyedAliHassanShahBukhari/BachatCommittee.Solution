// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BachatCommittee.Models.DTOs.Requests;

/// <summary>
/// Request DTO for assigning permissions to roles or users.
/// </summary>
public class AssignPermissionRequestDto
{
    [Required]
    public List<Guid> PermissionIds { get; set; } = new();
    
    /// <summary>
    /// Optional expiration date (for user permissions only).
    /// </summary>
    public DateTime? ExpiresOn { get; set; }
}

