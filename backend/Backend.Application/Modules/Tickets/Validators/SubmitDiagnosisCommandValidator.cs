using FluentValidation;

public class SubmitDiagnosisCommandValidator : AbstractValidator<SubmitDiagnosisCommand>
{
    public SubmitDiagnosisCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.DiagnosisResult).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.RootCause).MaximumLength(500);
        RuleFor(x => x.RecommendedRepair).MaximumLength(1000);
        RuleFor(x => x.RequiredPartsNote).MaximumLength(500);
    }
}