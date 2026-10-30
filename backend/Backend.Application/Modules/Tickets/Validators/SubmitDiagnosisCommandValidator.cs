using FluentValidation;

public class SubmitDiagnosisCommandValidator : AbstractValidator<SubmitDiagnosisCommand>
{
    public SubmitDiagnosisCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.DiagnosisResult).MaximumLength(1000);
        RuleFor(x => x).Must(x =>
                (x.DiagnosisSymptomIds?.Count > 0) ||
                !string.IsNullOrWhiteSpace(x.DiagnosisResultOther) ||
                !string.IsNullOrWhiteSpace(x.DiagnosisResult))
            .WithMessage("Vui lòng chọn triệu chứng hoặc nhập nội dung khác.");
        RuleFor(x => x.RootCause).MaximumLength(500);
        RuleFor(x => x.RecommendedRepair).MaximumLength(1000);
        RuleFor(x => x.RequiredPartsNote).MaximumLength(500);
    }
}