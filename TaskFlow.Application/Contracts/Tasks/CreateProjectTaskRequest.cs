namespace TaskFlow.Application.Contracts.Tasks;

public record CreateProjectTaskRequest(
    string Title,
    string? Description,
    int ProjectId,
    int? AssigneeId,
    int StatusId = 1,      // Pending по умолчанию
    int PriorityId = 2,    // Medium по умолчанию
    DateTime? DueDate = null
);