using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Tasks;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Context;

namespace TaskFlow.Infrastructure.Services;

public class ProjectTaskService(AppDbContext _context) : IProjectTaskService
{
    public async Task<IEnumerable<ProjectTaskResponse>> GetAllAsync(
        int? projectId = null, int? statusId = null, int? priorityId = null,
        int? assigneeId = null, int currentUserId = 0, bool isAdmin = false)
    {
        var query = _context.ProjectTasks
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .AsQueryable();

        if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
        if (statusId.HasValue) query = query.Where(t => t.StatusId == statusId.Value);
        if (priorityId.HasValue) query = query.Where(t => t.PriorityId == priorityId.Value);
        if (assigneeId.HasValue) query = query.Where(t => t.AssigneeId == assigneeId.Value);

        // Безопасность: не-админ видит задачи только своих проектов или назначенные на него
        if (!isAdmin)
        {
            query = query.Where(t =>
                t.Project.ProjectMembers.Any(pm => pm.UserId == currentUserId) ||
                t.AssigneeId == currentUserId);
        }

        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return tasks.Select(MapToResponse);
    }

    public async Task<ProjectTaskResponse?> GetByIdAsync(int id, int currentUserId, bool isAdmin)
    {
        var task = await _context.ProjectTasks
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.Project)
                .ThenInclude(p => p.ProjectMembers)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new BusinessException("Задача не найдена");

        if (!isAdmin &&
            !task.Project.ProjectMembers.Any(pm => pm.UserId == currentUserId) &&
            task.AssigneeId != currentUserId)
            throw new BusinessException("У вас нет доступа к этой задаче");

        return MapToResponse(task);
    }

    public async Task<ProjectTaskResponse> CreateAsync(int currentUserId, bool isAdmin, CreateProjectTaskRequest request)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId)
            ?? throw new BusinessException("Проект не найден");

        // Проверка доступа к проекту (участник или админ)
        if (!isAdmin && !project.ProjectMembers.Any(pm => pm.UserId == currentUserId))
            throw new BusinessException("У вас нет прав для создания задач в этом проекте");

        // Проверка, что Assignee (если указан) является участником проекта
        if (request.AssigneeId.HasValue &&
            !project.ProjectMembers.Any(pm => pm.UserId == request.AssigneeId.Value))
            throw new BusinessException("Назначенный исполнитель не является участником проекта");

        // Проверка существования статуса и приоритета
        if (!await _context.ProjectTaskStatuses.AnyAsync(s => s.Id == request.StatusId))
            throw new BusinessException("Указанный статус не существует");
        if (!await _context.ProjectTaskPriorities.AnyAsync(p => p.Id == request.PriorityId))
            throw new BusinessException("Указанный приоритет не существует");

        var task = new ProjectTask
        {
            Title = request.Title,
            Description = request.Description,
            ProjectId = request.ProjectId,
            AssigneeId = request.AssigneeId,
            StatusId = request.StatusId,
            PriorityId = request.PriorityId,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync();

        // Подгружаем связи для маппинга
        await _context.Entry(task).Reference(t => t.Status).LoadAsync();
        await _context.Entry(task).Reference(t => t.Priority).LoadAsync();
        await _context.Entry(task).Reference(t => t.Project).LoadAsync();
        if (task.AssigneeId.HasValue)
            await _context.Entry(task).Reference(t => t.Assignee).LoadAsync();

        return MapToResponse(task);
    }

    public async Task<ProjectTaskResponse> UpdateAsync(int id, int currentUserId, bool isAdmin, UpdateProjectTaskRequest request)
    {
        var task = await _context.ProjectTasks
            .Include(t => t.Project)
                .ThenInclude(p => p.ProjectMembers)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new BusinessException("Задача не найдена");

        // Проверка прав: участник проекта или Assignee
        if (!isAdmin &&
            !task.Project.ProjectMembers.Any(pm => pm.UserId == currentUserId) &&
            task.AssigneeId != currentUserId)
            throw new BusinessException("У вас нет прав для редактирования этой задачи");

        // Проверка, что новый Assignee (если указан) является участником проекта
        if (request.AssigneeId.HasValue &&
            !task.Project.ProjectMembers.Any(pm => pm.UserId == request.AssigneeId.Value))
            throw new BusinessException("Назначенный исполнитель не является участником проекта");

        task.Title = request.Title;
        task.Description = request.Description;
        task.StatusId = request.StatusId;
        task.PriorityId = request.PriorityId;
        task.AssigneeId = request.AssigneeId;
        task.DueDate = request.DueDate;

        await _context.SaveChangesAsync();
        return MapToResponse(task);
    }

    public async Task ChangeStatusAsync(int id, int currentUserId, bool isAdmin, int newStatusId)
    {
        var task = await _context.ProjectTasks
            .Include(t => t.Project)
                .ThenInclude(p => p.ProjectMembers)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new BusinessException("Задача не найдена");

        if (!isAdmin &&
            !task.Project.ProjectMembers.Any(pm => pm.UserId == currentUserId) &&
            task.AssigneeId != currentUserId)
            throw new BusinessException("У вас нет прав для изменения статуса этой задачи");

        if (!await _context.ProjectTaskStatuses.AnyAsync(s => s.Id == newStatusId))
            throw new BusinessException("Указанный статус не существует");

        task.StatusId = newStatusId;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int currentUserId, bool isAdmin)
    {
        var task = await _context.ProjectTasks
            .Include(t => t.Project)
                .ThenInclude(p => p.ProjectMembers)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new BusinessException("Задача не найдена");

        // Удалять может только Owner/Admin проекта или глобальный Admin
        if (!isAdmin && !await HasProjectRoleAsync(task.ProjectId, currentUserId, "Owner", "Admin"))
            throw new BusinessException("У вас нет прав для удаления этой задачи");

        _context.ProjectTasks.Remove(task);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Проверка, имеет ли пользователь одну из указанных ролей в проекте
    /// </summary>
    private async Task<bool> HasProjectRoleAsync(int projectId, int userId, params string[] roleNames)
    {
        return await _context.ProjectMembers
            .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
            .Join(_context.ProjectMemberRoles,
                  pm => pm.RoleId,
                  r => r.Id,
                  (pm, r) => r.Name)
            .AnyAsync(name => roleNames.Contains(name));
    }

    private static ProjectTaskResponse MapToResponse(ProjectTask task)
    {
        return new ProjectTaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.StatusId,
            task.Status.Name,
            task.PriorityId,
            task.Priority.Name,
            task.ProjectId,
            task.Project.Name,
            task.AssigneeId,
            task.Assignee?.Username,
            task.CreatedAt,
            task.DueDate
        );
    }
}