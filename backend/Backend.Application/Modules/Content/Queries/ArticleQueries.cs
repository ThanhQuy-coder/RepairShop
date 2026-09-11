using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Content.Queries;

public record GetArticlesQuery(bool? Published, int Page = 1, int PageSize = 20) : IRequest<ArticleListResponse>;

public class GetArticlesQueryHandler : IRequestHandler<GetArticlesQuery, ArticleListResponse>
{
    private readonly IArticleRepository _articleRepository;
    public GetArticlesQueryHandler(IArticleRepository articleRepository) => _articleRepository = articleRepository;

    public async Task<ArticleListResponse> Handle(GetArticlesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _articleRepository.SearchAsync(request.Published, request.Page, request.PageSize);
        var responses = items.Select(a => new ArticleListItemResponse(a.Id, a.Title, a.ImageUrl, a.CreatedAt)).ToList();
        return new ArticleListResponse(responses, total);
    }
}

public record GetArticleByIdQuery(Guid Id) : IRequest<ArticleResponse>;

public class GetArticleByIdQueryHandler : IRequestHandler<GetArticleByIdQuery, ArticleResponse>
{
    private readonly IArticleRepository _articleRepository;
    public GetArticleByIdQueryHandler(IArticleRepository articleRepository) => _articleRepository = articleRepository;

    public async Task<ArticleResponse> Handle(GetArticleByIdQuery request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id) ?? throw new NotFoundException("Bài viết", request.Id);

        // Public không được xem bài viết CHƯA publish qua route công khai — check ở Controller, không ở đây
        // (Handler này dùng chung cho cả Admin lẫn Public, phân biệt ở tầng route)
        return new ArticleResponse(article.Id, article.Title, article.Content, article.ImageUrl, article.IsPublished, article.CreatedAt);
    }
}