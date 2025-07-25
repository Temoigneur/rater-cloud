using FluentValidation;
using SharedModels.Evaluation;

namespace SharedModels.Validators
{
    public class EvaluationRequestValidator : AbstractValidator<EvaluationRequest>
    {
        public EvaluationRequestValidator()
        {
            RuleFor(x => x.Classification)
                .NotEmpty().WithMessage("Classification is required.")
                .MaximumLength(100).WithMessage("Classification cannot exceed 100 characters.");

            RuleFor(x => x.Intent)
                .NotNull().WithMessage("Intent is required.")
                .ChildRules(intent =>
                {
                    intent.RuleFor(i => i.IntentType)
                        .NotEmpty().WithMessage("IntentType is required.");
                    intent.RuleFor(i => i.Intent)
                        .NotEmpty().WithMessage("Intent is required.");
                });

            RuleFor(x => x.Output)
                .NotNull().WithMessage("Output is required.");
        }
    }
}
