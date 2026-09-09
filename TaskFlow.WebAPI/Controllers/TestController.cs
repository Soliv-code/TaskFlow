using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [Authorize] // <-- Этот атрибут требует валидный JWT-токен в заголовке
    [HttpGet("secure-data")]
    public IActionResult GetSecureData()
    {
        return Ok(new { message = "Доступ разрешен! Вы успешно аутентифицированы." });
    }
    public IActionResult GetTest() 
    {
        return Ok("Достучался?");
    }

}