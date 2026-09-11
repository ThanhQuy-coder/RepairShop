using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.DTOs;
using RepairShop.Domain.Modules.Content;
using MediatR;

namespace RepairShop.Application.Modules.Content.Commands;

public record CreateArticleCommand(string Title, string Content, string? ImageUrl) : IRequest<ArticleResponse>;

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
        await _articleRepository.AddAsync(article);
        await _articleRepository.SaveChangesAsync();

        return new ArticleResponse(article.Id, article.Title, article.Content, article.ImageUrl, article.IsPublished, article.CreatedAt);
    }
}

public record UpdateArticleCommand(Guid Id, string Title, string Content, string? ImageUrl) : IRequest<ArticleResponse>;

public class UpdateArticleCommandHandler : IRequestHandler<UpdateArticleCommand, ArticleResponse>
{
    private readonly IArticleRepository _articleRepository;
    public UpdateArticleCommandHandler(IArticleRepository articleRepository) => _articleRepository = articleRepository;

    public async Task<ArticleResponse> Handle(UpdateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id) ?? throw new NotFoundException("Bài viết", request.Id);
        article.UpdateContent(request.Title, request.Content, request.ImageUrl);
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