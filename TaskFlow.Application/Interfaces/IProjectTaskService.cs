using TaskFlow.Application.Contracts.Tasks;

namespace TaskFlow.Application.Interfaces;

public interface IProjectTaskService
{
    /// <summary>
    /// Получить список задач с фильтрами.
    /// Admin видит все, обычный пользователь — только свои (как Owner проекта или Assignee).
    /// </summary>
    Task<IEnumerable<ProjectTaskResponse>> GetAllAsync(
        int? projectId = null,
        int? statusId = null,
        int? priorityId = null,
        int? assigneeId = null,
        int currentUserId = 0,
        bool isAdmin = false);

    /// <summary>
    /// Получить задачу по ID с проверкой прав доступа.
    /// </summary>
    Task<ProjectTaskResponse?> GetByIdAsync(int id, int currentUserId, bool isAdmin);

    /// <summary>
    /// Создать новую задачу. Проверяет, что пользователь имеет доступ к проекту.
    /// </summary>
    Task<ProjectTaskResponse> CreateAsync(int currentUserId, bool isAdmin, CreateProjectTaskRequest request);
    /// <summary>
    /// Обновить задачу. Доступно Assignee, Owner'у проекта или Admin'у.
    /// </summary>
    Task<ProjectTaskResponse> UpdateAsync(int id, int currentUserId, bool isAdmin, UpdateProjectTaskRequest request);

    /// <summary>
    /// Удалить задачу. Доступно Owner'у проекта или Admin'у.
    /// </summary>
    Task DeleteAsync(int id, int currentUserId, bool isAdmin);

    /// <summary>
    /// Быстрая смена статуса задачи. Доступно Assignee, Owner'у проекта или Admin'у.
    /// </summary>
    Task ChangeStatusAsync(int id, int currentUserId, bool isAdmin, int newStatusId);
}