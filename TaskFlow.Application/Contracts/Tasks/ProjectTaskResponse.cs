namespace TaskFlow.Application.Contracts.Tasks;

public record ProjectTaskResponse(
    int Id,
    string Title,
    string? Description,
    int StatusId,
    string StatusName,
    int PriorityId,
    string PriorityName,
    int ProjectId,
    string ProjectName,
    int? AssigneeId,
    string? AssigneeUsername,
    DateTime? CreatedAt,
    DateTime? DueDate
);