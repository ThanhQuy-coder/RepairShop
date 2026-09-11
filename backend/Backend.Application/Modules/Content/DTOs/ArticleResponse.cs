namespace RepairShop.Application.Modules.Content.DTOs;

public record ArticleResponse(Guid Id, string Title, string Content, string? ImageUrl, bool IsPublished, DateTime CreatedAt);
public record ArticleListItemResponse(Guid Id, string Title, string? ImageUrl, DateTime CreatedAt);
public record ArticleListResponse(List<ArticleListItemResponse> Items, int Total);