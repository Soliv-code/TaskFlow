using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Contracts.Users;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Понимаем что только админы могут пользовать этот контроллер! 
public class AdminController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var success = await _authService.CreateUserAsync(request);

        if (!success)
            return BadRequest(new { message = "Пользователь с таким именем или email уже существует" });

        return Ok(new { message = $"Пользователь {request.Username} успешно создан с ролью {request.Role}" });
    }

    // Получаем количество просроченных токенов
    [HttpGet("tokens/expired/{userId?}")]
    public async Task<IActionResult> GetExpiredTokensCount(int? userId = null)
    {
        var expiredIds = await _authService.GetExpiredTokenIdsAsync(userId);

        var message = userId.HasValue
            ? $"Найдено просроченных токенов для пользователя с Id: {userId}"
            : "Найдено просроченных токенов во всей системе";
        return Ok(new
        {
            count = expiredIds.Count,
            expiredIds,
            message
        });
    }

    // Удаление просроченных токенов
    [HttpDelete("tokens/expired/{userId?}")]
    public async Task<IActionResult> DeleteExpiredTokens(int? userId = null)
    {
        var deletedIds = await _authService.DeleteExpiredTokensAsync(userId);

        var message = userId.HasValue
            ? $"Удалено просроченных токенов для пользователя с Id: {userId}"
            : "Удалено просроченных токенов во всей системе";

        return Ok(new 
        { 
            deletedCount = deletedIds.Count, 
            deletedIds,
            message 
        });
    }
}
