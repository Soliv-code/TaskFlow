using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Contracts.Authentication; // ПРОВЕРИТЬ ОБЯЗАТЕЛЬНО! Чтобы было using TaskFlow.Application.Contracts.Authentication;
                                                     //
                                                     // Если будет:
                                                     // using Microsoft.AspNetCore.Identity.Data;
                                                     // или
                                                     // Microsoft.AspNetCore.Identity.Data.LoginRequest
                                                     //
                                                     // Ты получишь тихую коллизию пространств имен, т.к.
                                                     // у MS есть в Identity.Data свой LoginRequest и скорее всего
                                                     // это там используется 
using TaskFlow.Application.Interfaces;

namespace TaskFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request is null) return BadRequest("Пустое тело запроса!");
        var response = await _authService.LoginAsync(request);
        // Если пользователь не найден  или пароль неверный 
        if (response is null) return Unauthorized(new { message = "Неверное имя пользователя или пароль" });
        // Возвращаем успешный ответ с токеном
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request);

        if (response is null)
            return Unauthorized(new { message = "Недействительный или истекший refresh token" });

        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        // Берем ID пользователя из JWT-токена, который пришел в заголовке
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { message = "Не удалось определить пользователя" });
        }
        await _authService.LogoutAsync(userId, request.RefreshToken);
        return Ok(new { message = "Выход выполнен успешно!" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        // Достаём ID пользователя из JWT-токена
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { message = "Не удалось определить пользователя" });
        }

        await _authService.ChangePasswordAsync(userId, request);

        return Ok(new { message = "Пароль успешно изменен" });
    }
}
