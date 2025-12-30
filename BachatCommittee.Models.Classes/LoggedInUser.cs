// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Models.Enums;

namespace BachatCommittee.Models.Classes;

public class LoggedInUser
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public CustomerType Type { get; set; }
    public UserType UserType { get; set; }
}
