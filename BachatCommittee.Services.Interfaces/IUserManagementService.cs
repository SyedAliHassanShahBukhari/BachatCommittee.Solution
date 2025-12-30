// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.DTOs.Responses;

namespace BachatCommittee.Services.Interfaces;

public interface IUserManagementService
{
    Task<List<UserResponseDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserResponseDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserResponseDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<UserResponseDto> CreateUserAsync(RegisterRequestDto model, Guid creatorUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(UpdateUserRequestDto model, Guid updaterUserId, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> AssignRolesAsync(string userId, List<string> roles, CancellationToken cancellationToken = default);
    Task<bool> RemoveRolesAsync(string userId, List<string> roles, CancellationToken cancellationToken = default);
    Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
}
