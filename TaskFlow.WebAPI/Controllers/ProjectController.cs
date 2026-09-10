using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlow.Application.Contracts.Projects;
using TaskFlow.Application.Interfaces;
using TaskFlow.Infrastructure.Services;

namespace TaskFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectController(IProjectService projectService) : Controller
{
    private readonly IProjectService _projectService = projectService;
    private (int UserId, bool IsAdmin) GetCurrentUser()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Не удалось определить ID пользователя");

        return (int.Parse(userIdStr), User.IsInRole("Admin"));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var (userId, isAdmin) = GetCurrentUser();
        var projects = await _projectService.GetAllAsync(userId, isAdmin);
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var project = await _projectService.GetByIdAsync(id, userId, isAdmin)
            ?? throw new ApplicationException("Проект не найден");

        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var createdProject = await _projectService.CreateAsync(userId, request); // isAdmin здесь не нужен, создатель всегда становится Owner
        return CreatedAtAction(nameof(GetById), new { id = createdProject.Id }, createdProject);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequest request)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var updatedProject = await _projectService.UpdateAsync(id, userId, isAdmin, request);
        return Ok(updatedProject);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, isAdmin) = GetCurrentUser();
        await _projectService.DeleteAsync(id, userId, isAdmin);
        return NoContent();
    }


}
