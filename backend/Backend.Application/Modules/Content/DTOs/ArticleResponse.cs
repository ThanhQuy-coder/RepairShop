namespace RepairShop.Application.Modules.Content.DTOs;

public record ArticleResponse(
    Guid Id,
    string Title,
    string Content,
    string? ImageUrl,
    bool IsPublished,
    DateTime CreatedAt);

public record ArticleListItemResponse(
    Guid Id,
    string Title,
    string? ImageUrl,
    DateTime CreatedAt,
    bool IsPublished = true,
    string? Summary = null);

public record ArticleListResponse(List<ArticleListItemResponse> Items, int Total);
