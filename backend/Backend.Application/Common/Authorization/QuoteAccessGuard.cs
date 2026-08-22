using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Quotes;

namespace RepairShop.Application.Common.Authorization;

/// <summary>API Spec Tuần 2: "PATCH /api/quotes/{id}/respond — Authorization: Customer (chỉ quote của mình)".</summary>
public static class QuoteAccessGuard
{
    public static void EnsureCustomerOwnsQuote(Quote quote, Customer? customer)
    {
        if (customer is null || quote.RepairTicket.CustomerId != customer.Id)
            throw new ForbiddenException("Bạn chỉ được phản hồi báo giá thuộc về chính mình.");
    }
}