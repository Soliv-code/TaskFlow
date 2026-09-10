using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Projects;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Context;

namespace TaskFlow.Infrastructure.Services;

public class ProjectService(AppDbContext _context) : IProjectService
{
    /// <summary>
    /// Получить список проектов.
    /// Admin видит все, обычный пользователь — только свои (где он Owner).
    /// </summary>
    public async Task<IEnumerable<ProjectResponse>> GetAllAsync(int currentUserId, bool isAdmin)
    {
        var query = _context.Projects
            .Include(p => p.Owner)
            .AsQueryable();

        // Если не админ - фильтруем только свои проекты:
        if (!isAdmin)
        {
            query = query.Where(p => p.ProjectMembers.Any(pm => pm.UserId == currentUserId));
        }

        var projects = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToResponse);
    }

    /// <summary>
    /// Получить проект по ID с проверкой прав доступа.
    /// </summary>
    public async Task<ProjectResponse?> GetByIdAsync(int id, int currentUserId, bool isAdmin)
    {
        var project = await _context.Projects
            .Include(p => p.Owner)
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new BusinessException($"Проект не найден c id: {id} не найден!");

        // Проверяем права: админ видит всё, обычный юзер — только если он участник
        if (!isAdmin && !project.ProjectMembers.Any(pm => pm.UserId == currentUserId))
            throw new BusinessException("У вас нет доступа к этому проекту");

        return MapToResponse(project);
    }

    /// <summary>
    /// Создать новый проект. Текущий пользователь автоматически становится Owner.
    /// </summary>
    public async Task<ProjectResponse> CreateAsync(int currentUserId, CreateProjectRequest request)
    {
        // Проверяем уникальность названия проекта для этого владельца
        if (await _context.Projects.AnyAsync(p => p.OwnerId == currentUserId && p.Name == request.Name))
            throw new BusinessException($"Проект с названием '{request.Name}' уже существует");

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Автоматически добавляем создателя как Owner в ProjectMembers
        var ownerRole = await _context.ProjectMemberRoles
            .FirstOrDefaultAsync(r => r.Name == "Owner")
            ?? throw new BusinessException("Роль 'Owner' не найдена в справочнике");

        var member = new ProjectMember
        {
            ProjectId = project.Id,
            UserId = currentUserId,
            RoleId = ownerRole.Id,
            JoinedAt = DateTime.UtcNow
        };

        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();

        // Загружаем Owner для маппинга
        await _context.Entry(project).Reference(p => p.Owner).LoadAsync();

        return MapToResponse(project);
    }

    /// <summary>
    /// Обновить проект. Доступно только Owner'у или Admin'у.
    /// </summary>
    public async Task<ProjectResponse> UpdateAsync(int id, int currentUserId, bool isAdmin, UpdateProjectRequest request)
    {
        var project = await _context.Projects
            .Include(p => p.Owner)
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new BusinessException("Проект не найден");

        // Проверяем права: только Owner или Admin
        if (!isAdmin && !await IsProjectOwnerAsync(project.Id, currentUserId))
            throw new BusinessException("У вас нет прав для редактирования этого проекта");

        // Проверяем уникальность названия (если оно изменилось)
        if (project.Name != request.Name &&
            await _context.Projects.AnyAsync(p => p.OwnerId == project.OwnerId && p.Name == request.Name))
        {
            throw new BusinessException($"Проект с названием '{request.Name}' уже существует");
        }

        // Обновляем поля
        project.Name = request.Name;
        project.Description = request.Description;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(project);
    }

    /// <summary>
    /// Удалить проект. Доступно только Owner'у или Admin'у.
    /// Если в проекте есть задачи — выбросит BusinessException.
    /// </summary>

    public async Task DeleteAsync(int id, int currentUserId, bool isAdmin)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectTasks)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new BusinessException("Проект не найден");

        // Проверяем права: только Owner или Admin
        if (!isAdmin && !await IsProjectOwnerAsync(project.Id, currentUserId))
            throw new BusinessException("У вас нет прав для удаления этого проекта");

        if (project.ProjectTasks.Any())
            throw new BusinessException($"Невозможно удалить проект: в нём есть задачи ({project.ProjectTasks.Count} шт.). Сначала удалите или переместите их.");

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Проверка, является ли пользователь владельцем проекта
    /// </summary>
    private async Task<bool> IsProjectOwnerAsync(int projectId, int userId)
    {
        var ownerRole = await _context.ProjectMemberRoles
            .FirstOrDefaultAsync(r => r.Name == "Owner");

        if (ownerRole is null)
            return false;

        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && pm.RoleId == ownerRole.Id);
    }

    /// <summary>
    /// Маппинг сущности Project в DTO ProjectResponse
    /// </summary>
    private static ProjectResponse MapToResponse(Project project)
    {
        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.OwnerId,
            project.Owner.Username,
            project.CreatedAt,
            project.UpdatedAt
        );
    }
}