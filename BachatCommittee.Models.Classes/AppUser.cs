// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using awisk.common.Classes;
using BachatCommittee.Models.Enums;

namespace BachatCommittee.Models.Classes;

public class AppUser : ApplicationUser
{
    [StringLength(50, MinimumLength = 3)]
    public string Number { get; set; } = string.Empty;
    [Required]
    [StringLength(100, MinimumLength = 5)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;
    [Required]
    [DefaultValue(UserType.User)]
    public UserType UserTypeId { get; set; } = UserType.User;
    public string ProfilePic { get; set; } = string.Empty;
    [Required]
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
}

