// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BachatCommittee.Models.Enums;

namespace BachatCommittee.Models.DTOs.Requests;

public class UpdateUserRequestDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Gender { get; set; } = string.Empty;

    [Required]
    public UserType UserType { get; set; } = UserType.User;

    public bool IsActive { get; set; } = true;

    public List<string> Roles { get; set; } = new();
}
