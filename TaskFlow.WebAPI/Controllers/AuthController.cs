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

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if(request is null) return BadRequest("Пустое тело запроса");
        var response = await _authService.LoginAsync(request);
        // Если пользователь не найден  или пароль неверный 
        if (response is null) return Unauthorized(new { message = "Неверное имя пользователя или пароль" });
        // Возвращаем успешный ответ с токеном
        return Ok(response);
    }

}
