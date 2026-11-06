using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.DTOs;
using RepairShop.Domain.Modules.Content;
using MediatR;

namespace RepairShop.Application.Modules.Content.Commands;

public record CreateArticleCommand(
    string Title,
    string Content,
    string? ImageUrl,
    bool IsPublished = false) : IRequest<ArticleResponse>;

public class CreateArticleCommandHandler : IRequestHandler<CreateArticleCommand, ArticleResponse>
{
    private readonly IArticleRepository _articleRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateArticleCommandHandler(IArticleRepository articleRepository, ICurrentUserService currentUser)
    {
        _articleRepository = articleRepository;
        _currentUser = currentUser;
    }

    public async Task<ArticleResponse> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = new Article(request.Title, request.Content, _currentUser.UserId!.Value, request.ImageUrl);
        if (request.IsPublished)
        {
            article.Publish();
        }
        await _articleRepository.AddAsync(article);
        await _articleRepository.SaveChangesAsync();

        return new ArticleResponse(article.Id, article.Title, article.Content, article.ImageUrl, article.IsPublished, article.CreatedAt);
    }
}

public record UpdateArticleCommand(
    Guid Id,
    string Title,
    string Content,
    string? ImageUrl,
    bool? IsPublished = null) : IRequest<ArticleResponse>;

public class UpdateArticleCommandHandler : IRequestHandler<UpdateArticleCommand, ArticleResponse>
{
    private readonly IArticleRepository _articleRepository;
    public UpdateArticleCommandHandler(IArticleRepository articleRepository) => _articleRepository = articleRepository;

    public async Task<ArticleResponse> Handle(UpdateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id) ?? throw new NotFoundException("Bài viết", request.Id);
        article.UpdateContent(request.Title, request.Content, request.ImageUrl);
        if (request.IsPublished.HasValue)
        {
            if (request.IsPublished.Value) article.Publish();
            else article.Unpublish();
        }
        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync();

        return new ArticleResponse(article.Id, article.Title, article.Content, article.ImageUrl, article.IsPublished, article.CreatedAt);
    }
}

public record ToggleArticlePublishCommand(Guid Id, bool Publish) : IRequest<Unit>;

public class ToggleArticlePublishCommandHandler : IRequestHandler<ToggleArticlePublishCommand, Unit>
{
    private readonly IArticleRepository _articleRepository;
    public ToggleArticlePublishCommandHandler(IArticleRepository articleRepository) => _articleRepository = articleRepository;

    public async Task<Unit> Handle(ToggleArticlePublishCommand request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id) ?? throw new NotFoundException("Bài viết", request.Id);
        if (request.Publish) article.Publish(); else article.Unpublish();
        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync();
        return Unit.Value;
    }
}

public record DeleteArticleCommand(Guid Id) : IRequest<Unit>;

public class DeleteArticleCommandHandler : IRequestHandler<DeleteArticleCommand, Unit>
{
    private readonly IArticleRepository _articleRepository;
    public DeleteArticleCommandHandler(IArticleRepository articleRepository) => _articleRepository = articleRepository;

    public async Task<Unit> Handle(DeleteArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id) ?? throw new NotFoundException("Bài viết", request.Id);
        article.MarkDeleted();
        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync();
        return Unit.Value;
    }
}
