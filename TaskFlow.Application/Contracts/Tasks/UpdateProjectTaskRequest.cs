namespace TaskFlow.Application.Contracts.Tasks;

public record UpdateProjectTaskRequest(
    string Title,
    string? Description,
    int StatusId,
    int PriorityId,
    int? AssigneeId,
    DateTime? DueDate
);