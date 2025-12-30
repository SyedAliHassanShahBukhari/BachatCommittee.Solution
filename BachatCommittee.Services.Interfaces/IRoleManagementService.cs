// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.DTOs.Responses;

namespace BachatCommittee.Services.Interfaces;

public interface IRoleManagementService
{
    Task<List<RoleResponseDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleResponseDto?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);
    Task<RoleResponseDto?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto model, CancellationToken cancellationToken = default);
    Task<bool> UpdateRoleAsync(UpdateRoleRequestDto model, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);
    Task<List<UserResponseDto>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default);
}
