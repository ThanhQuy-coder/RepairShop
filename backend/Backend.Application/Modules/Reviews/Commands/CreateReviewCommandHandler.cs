using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RepairShop.Application.Modules.Reviews.Commands;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateReviewCommandHandler> _logger;

    public CreateReviewCommandHandler(IRepairTicketRepository ticketRepository, ICustomerRepository customerRepository,
        ICurrentUserService currentUser, ILogger<CreateReviewCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _customerRepository = customerRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ReviewResponse> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        // Ownership: chỉ đánh giá được ticket của chính mình (đúng nguyên tắc Task 4.16)
        var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
        TicketAccessGuard.EnsureCustomerOwnsTicket(ticket, customer);

        // BR-11 enforce trong Domain: chỉ sau DELIVERED, tối đa 1 Review
        var review = ticket.CreateReview(request.Rating, request.Comment);
        _ticketRepository.TrackNewReview(review);

        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation("Ticket {TicketCode} nhận Review {Rating} sao", ticket.TicketCode, request.Rating);

        return new ReviewResponse(review.Id, review.Rating, review.Comment, review.CreatedAt);
    }
}