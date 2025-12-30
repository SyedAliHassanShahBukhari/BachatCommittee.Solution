// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.DTOs.Responses;
using BachatCommittee.Models.DTOs.Requests;

namespace BachatCommittee.Services.Interfaces;

public interface IAuthService
{
    Task<TokenResponseDto?> TryLoginAsync(LoginRequestDto model);

    /// <summary>
    /// Registers a user. If forbidElevatedUserTypes = true, blocks Developer/SuperAdmin.
    /// Returns (success, message, errors, tokenPayload).
    /// </summary>
    Task<GenericResponseDto<TokenResponseDto>> TryRegisterAsync(RegisterRequestDto model, Guid creatorUserId, bool forbidElevatedUserTypes);

    Task<bool> ChangeUserPassword(string userId, string password);

    Task<bool> IsUserNameAvailableAsync(string username);
    public bool IsValidPassword(string input);
}
