namespace TaskFlow.Application.Contracts.Projects;

public record UpdateProjectRequest(
    string Name,
    string? Description
);