using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // <-- Этот атрибут требует валидный JWT-токен в заголовке
public class TestController : ControllerBase
{
    [HttpGet("secure-data")]
    public IActionResult GetSecureData()
    {
        return Ok(new { message = "Доступ разрешен! Вы успешно аутентифицированы." });
    }
}