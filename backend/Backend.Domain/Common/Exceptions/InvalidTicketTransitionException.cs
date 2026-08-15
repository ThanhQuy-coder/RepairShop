using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Domain.Common.Exceptions
{
    public class InvalidTicketTransitionException : DomainException
    {
        public TicketStatus From { get; }
        public TicketStatus To { get; }

        public InvalidTicketTransitionException(TicketStatus from, TicketStatus to)
            : base($"Không thể chuyển trạng thái phiếu sửa chữa từ '{from}' sang '{to}'.")
        {
            From = from;
            To = to;
        }
    }
}