// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BachatCommittee.Models.DTOs.Requests;

public class AssignRolesRequestDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public List<string> Roles { get; set; } = new();
}
