using FluentValidation;

public class PassQualityCheckCommandValidator : AbstractValidator<PassQualityCheckCommand>
{
    public PassQualityCheckCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.FunctionalCheckNotes).NotEmpty().WithMessage("Phải ghi kết quả kiểm tra chức năng.");
        RuleFor(x => x.CosmeticCheckNotes).NotEmpty().WithMessage("Phải ghi kết quả kiểm tra ngoại hình.");
        RuleFor(x => x.OriginalIssueResolvedNotes).NotEmpty().WithMessage("Phải xác nhận lỗi ban đầu đã được khắc phục.");
    }
}