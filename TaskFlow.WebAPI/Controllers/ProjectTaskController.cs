using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlow.Application.Contracts.Tasks;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Все методы требуют аутентификации
public class ProjectTaskController(IProjectTaskService taskService) : ControllerBase
{
    private readonly IProjectTaskService _taskService = taskService;

    // Хелпер для извлечения данных текущего пользователя из JWT
    private (int UserId, bool IsAdmin) GetCurrentUser()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Не удалось определить ID пользователя");

        return (int.Parse(userIdStr), User.IsInRole("Admin"));
    }

    [HttpGet]// Не ссать FromQuery! Ms нас бережёт от SQLInjection! 
    public async Task<IActionResult> GetAll(
    [FromQuery] int? projectId = null,
    [FromQuery] int? statusId = null,
    [FromQuery] int? priorityId = null,
    [FromQuery] int? assigneeId = null) 
    {
        var (userId, isAdmin) = GetCurrentUser();
        var tasks = await _taskService.GetAllAsync(projectId, statusId, priorityId, assigneeId, userId, isAdmin);
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var task = await _taskService.GetByIdAsync(id, userId, isAdmin)
            ?? throw new ApplicationException("Задача не найдена"); // Fallback, хотя сервис тоже кидает BusinessException

        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectTaskRequest request)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var createdTask = await _taskService.CreateAsync(userId, isAdmin, request);
        return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectTaskRequest request)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var updatedTask = await _taskService.UpdateAsync(id, userId, isAdmin, request);
        return Ok(updatedTask);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var (userId, isAdmin) = GetCurrentUser();
        await _taskService.ChangeStatusAsync(id, userId, isAdmin, request.NewStatusId);
        return NoContent(); // 204 No Content — стандарт для успешного PATCH без возврата тела
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, isAdmin) = GetCurrentUser();
        await _taskService.DeleteAsync(id, userId, isAdmin);
        return NoContent();
    }
}
