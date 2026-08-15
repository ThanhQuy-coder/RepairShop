namespace RepairShop.Domain.Common.Exceptions
{
    public class InvalidTicketOperationException : DomainException
    {
        public InvalidTicketOperationException(string message) : base(message) { }
    }
}