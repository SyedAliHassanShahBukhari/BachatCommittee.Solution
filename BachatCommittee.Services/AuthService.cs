// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using awisk.common.DTOs.Responses;
using awisk.common.Helpers;
using BachatCommittee.Models.Classes;
using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.Enums;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BachatCommittee.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUserStore<AppUser> _userStore;
    private readonly IUserEmailStore<AppUser> _emailStore;
    private readonly AppSettings _appSettings;

    public AuthService(
        UserManager<AppUser> userManager,
        IUserStore<AppUser> userStore,
        AppSettings appSettings)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = (IUserEmailStore<AppUser>)_userStore;
        _appSettings = appSettings;
    }

    public async Task<TokenResponseDto?> TryLoginAsync(LoginRequestDto model)
    {
        if (model is null)
        {
            return null;
        }

        var user = await _userManager.FindByNameAsync(model.Username).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var ok = await _userManager.CheckPasswordAsync(user, model.Password).ConfigureAwait(false);
        if (!ok)
        {
            return null;
        }

        var token = await GenerateJwtTokenAsync(user).ConfigureAwait(false);
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

        return new TokenResponseDto
        {
            FullName = UniversalOpertaions.IfNullEmptyString(user.FullName),
            Id = UniversalOpertaions.IfNullEmptyString(user.Id),
            Roles = UniversalOpertaions.IfNullEmptyString(string.Join(",", roles)),
            Token = token,
            Username = UniversalOpertaions.IfNullEmptyString(user.UserName),
            Email = UniversalOpertaions.IfNullEmptyString(user.Email),
        };
    }

    public async Task<GenericResponseDto<TokenResponseDto>>
        TryRegisterAsync(RegisterRequestDto model, Guid creatorUserId, bool forbidElevatedUserTypes)
    {
        if (model is null)
        {
            return new GenericResponseDto<TokenResponseDto> { StatusCode = System.Net.HttpStatusCode.BadRequest, Message = "Invalid registration data." };
        }

        if (forbidElevatedUserTypes &&
            (model.UserType == UserType.Developer || model.UserType == UserType.SuperAdmin))
        {
            return new GenericResponseDto<TokenResponseDto> { StatusCode = System.Net.HttpStatusCode.Forbidden, Message = $"Not allowed to create user with type {model.UserType}" };
        }

        var existing = await _userManager.FindByNameAsync(model.Username).ConfigureAwait(false);
        if (existing is not null)
        {
            return new GenericResponseDto<TokenResponseDto> { StatusCode = System.Net.HttpStatusCode.Conflict, Message = "Username is already registered." };
        }

        var user = CreateUser();
        await _userStore.SetUserNameAsync(user, model.Username, CancellationToken.None).ConfigureAwait(false);
        await _emailStore.SetEmailAsync(user, model.Email, CancellationToken.None).ConfigureAwait(false);

        user.FullName = model.FullName;
        user.Gender = model.Gender;
        user.UserTypeId = model.UserType;
        user.IsActive = true;
        user.IsVerified = true;
        user.IsDeleted = false;
        user.CreatedOn = DateTime.UtcNow;
        user.CreatedBy = creatorUserId;

        var createResult = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            return new GenericResponseDto<TokenResponseDto> { StatusCode = System.Net.HttpStatusCode.BadRequest, Message = $"Registration failed.{string.Join("|", createResult.Errors.Select(e => e.Description).ToList())}" };
        }

        // assign role by enum description (your existing extension)
        await _userManager.AddToRoleAsync(user, model.UserType.ToDescription()).ConfigureAwait(false);

        var jwt = await GenerateJwtTokenAsync(user).ConfigureAwait(false);
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

        var payload = new TokenResponseDto
        {
            FullName = UniversalOpertaions.IfNullEmptyString(user.FullName),
            Id = UniversalOpertaions.IfNullEmptyString(user.Id),
            Roles = UniversalOpertaions.IfNullEmptyString(string.Join(",", roles)),
            Token = jwt,
            Username = UniversalOpertaions.IfNullEmptyString(user.UserName),
            Email = UniversalOpertaions.IfNullEmptyString(user.Email),
        };

        return new GenericResponseDto<TokenResponseDto> { StatusCode = System.Net.HttpStatusCode.OK, Message = "Registration successful.", Response = payload };
    }

    public async Task<bool> IsUserNameAvailableAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        var user = await _userManager.FindByNameAsync(username).ConfigureAwait(false);
        return user is null;
    }

    private static AppUser CreateUser()
    {
        try { return Activator.CreateInstance<AppUser>()!; }
        catch
        {
            throw new InvalidOperationException(
                $"Can't create an instance of '{nameof(AppUser)}'. Ensure it is non-abstract and has a parameterless ctor.");
        }
    }

    private async Task<string> GenerateJwtTokenAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

        var claims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Email, UniversalOpertaions.IfNullEmptyString(user?.Email)),
                new (JwtRegisteredClaimNames.Jti, UniversalOpertaions.NewGuidStr()),
                new (ClaimTypes.NameIdentifier, UniversalOpertaions.IfNullEmptyString(user?.Id)),
                new (ClaimTypes.Name, UniversalOpertaions.IfNullEmptyString(user?.FullName)),
                new ("UserName", UniversalOpertaions.IfNullEmptyString(user?.UserName))
            };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var jwt = _appSettings.JwtSettings;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(jwt.Expiry),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> ChangeUserPassword(string userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            var response = await _userManager.ResetPasswordAsync(user, token, password).ConfigureAwait(false);
            return response.Succeeded;
        }
        return false;
    }

    /// <summary>
    /// Validates that the input string contains at least:
    /// one lowercase, one uppercase, one digit, and one allowed special character.
    /// Allowed special characters: !@^*()-_=+[]{};:,
    /// </summary>
    public bool IsValidPassword(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Regex breakdown:
        // (?=.*[a-z])  → at least one lowercase
        // (?=.*[A-Z])  → at least one uppercase
        // (?=.*\d)     → at least one number
        // (?=.*[!@^*()\-_=+\[\]{};:,]) → at least one allowed special
        // ^[A-Za-z\d!@^*()\-_=+\[\]{};:,]+$ → only allowed characters
        var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@^*()-_=+{};:,])[A-Za-z\d!@^*()-_=+{};:,]+$";
        return Regex.IsMatch(input, pattern);
    }
}
