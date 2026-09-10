namespace TaskFlow.Application.Contracts.Projects;

public record ProjectResponse(
    int Id,
    string Name,
    string? Description,
    int OwnerId,
    string OwnerUsername,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);