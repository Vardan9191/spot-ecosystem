namespace Spot.Api.DTOs;

public record TaskItemDto(Guid Id, Guid ListId, string Title, bool IsCompleted, string Quantity);

public record CreateTaskItemRequest(string Title, string Quantity = "1");

public record TaskListDto(
    Guid Id,
    Guid UserId,
    string Title,
    CategoryDto? Category,
    double? Latitude,
    double? Longitude,
    int RadiusMeters,
    bool IsActive,
    List<TaskItemDto> Items
);

public record CreateTaskListRequest(
    Guid UserId,
    string Title,
    Guid? CategoryId,
    double? Latitude,
    double? Longitude,
    int RadiusMeters = 150,
    bool IsActive = true
);

public record UpdateTaskListRequest(
    string Title,
    Guid? CategoryId,
    double? Latitude,
    double? Longitude,
    int RadiusMeters,
    bool IsActive
);
