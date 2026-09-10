using TaskFlow.Application.Contracts.Projects;
using TaskFlow.Application.Contracts.Tasks;

namespace TaskFlow.Application.Interfaces;

public interface IProjectService
{
    /// <summary>
    /// Получить список проектов.
    /// Admin видит все, обычный пользователь — только свои (где он Owner).
    /// </summary>
    Task<IEnumerable<ProjectResponse>> GetAllAsync(int currentUserId, bool isAdmin);

    /// <summary>
    /// Получить проект по ID с проверкой прав доступа.
    /// </summary>
    Task<ProjectResponse?> GetByIdAsync(int id, int currentUserId, bool isAdmin);

    /// <summary>
    /// Создать новый проект. Текущий пользователь автоматически становится Owner.
    /// </summary>
    Task<ProjectResponse> CreateAsync(int currentUserId, CreateProjectRequest request);


    /// <summary>
    /// Обновить проект. Доступно только Owner'у или Admin'у.
    /// </summary>
    Task<ProjectResponse> UpdateAsync(int id, int currentUserId, bool isAdmin, UpdateProjectRequest request);

    /// <summary>
    /// Удалить проект. Доступно только Owner'у или Admin'у.
    /// Если в проекте есть задачи — выбросит BusinessException.
    /// </summary>
    Task DeleteAsync(int id, int currentUserId, bool isAdmin);
}