// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BachatCommittee.Models.Enums;

public enum UserType
{
    Developer = 1,
    SuperAdmin = 2,
    Admin = 3,
    User = 4,
    Staff = 5,
    //if you change this enum, update 20251121201338_IdentityTables class in BachatCommittee.API.Migrations to keep them in sync
}
