// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using awisk.common.DTOs.Responses;
using awisk.common.Interfaces;
using BachatCommittee.Models.Classes;
using BachatCommittee.Models.DTOs.Requests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BachatCommittee.UI.AdminPortal.Controllers;

public class AuthController(
    IApiService apiService,
    AppSettings settings,
    ILogger<AuthController> logger) : Controller
{
    private readonly IApiService _apiService = apiService;
    private readonly AppSettings _settings = settings;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestDto model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Call API to authenticate
            var response = await _apiService.PostAsync<LoginRequestDto, GenericResponseDto<TokenResponseDto>>(
                new Uri("auth/login", UriKind.Relative),
                model).ConfigureAwait(true);

            if (response?.StatusCode == System.Net.HttpStatusCode.Created && response.Response != null)
            {
                var tokenResponse = response.Response;

                // Create claims from JWT token
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(tokenResponse.Token);
                var claims = token.Claims.ToList();

                // Add additional claims
                claims.Add(new Claim("Token", tokenResponse.Token));

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    _settings.Auth.AuthScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(_settings.Auth.TokenExpire),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    _settings.Auth.AuthScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties).ConfigureAwait(true);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, response?.Message ?? "Invalid username or password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(true);
        Response.Cookies.Delete(_settings.Auth.AuthCookie);
        return RedirectToAction("Login", "Auth");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Email is required.");
            return View();
        }

        // TODO: Implement forgot password logic - call API endpoint if available
        // For now, just show a message
        ViewData["SuccessMessage"] = "If an account exists with this email, a password reset link has been sent.";
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
