using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Content;

public class Article : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public string? ImageUrl { get; private set; }
    public Guid AuthorId { get; private set; }
    public bool IsPublished { get; private set; } = false;

    private Article() { } // for EF Core

    public Article(string title, string content, Guid authorId, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Tiêu đề bài viết không được để trống.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Nội dung bài viết không được để trống.");

        Title = title;
        Content = content;
        AuthorId = authorId;
        ImageUrl = imageUrl;
    }

    public void UpdateContent(string title, string content, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Tiêu đề bài viết không được để trống.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Nội dung bài viết không được để trống.");

        Title = title;
        Content = content;
        ImageUrl = imageUrl;
        MarkUpdated();
    }

    public void Publish() { IsPublished = true; MarkUpdated(); }
    public void Unpublish() { IsPublished = false; MarkUpdated(); }
}