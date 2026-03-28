namespace GymFlow.WebAPI.Controllers;

using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController(
    LoginUseCase loginUseCase,
    RefreshTokenUseCase refreshTokenUseCase,
    LogoutUseCase logoutUseCase) : ControllerBase
{
    private const string RefreshCookieName = "gymflow_refresh";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken ct)
    {
        var result = await loginUseCase.ExecuteAsync(request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        SetRefreshCookie(result.Extra!);
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var rawToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrWhiteSpace(rawToken))
            return Unauthorized(new { error = "No refresh token." });

        var result = await refreshTokenUseCase.ExecuteAsync(rawToken, ct);
        if (!result.IsSuccess)
        {
            ClearRefreshCookie();
            return Unauthorized(new { error = result.ErrorMessage });
        }

        SetRefreshCookie(result.Extra!);
        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var rawToken = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrWhiteSpace(rawToken))
            await logoutUseCase.ExecuteAsync(rawToken, ct);

        ClearRefreshCookie();
        return NoContent();
    }

    private void SetRefreshCookie(string rawToken) =>
        Response.Cookies.Append(RefreshCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddDays(7),
            Path     = "/api/auth",
        });

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Path     = "/api/auth",
        });
}