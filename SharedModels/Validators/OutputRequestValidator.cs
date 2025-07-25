using FluentValidation;
using SharedModels.Request;

namespace SharedModels.Validators
{
    public class OutputRequestValidator : AbstractValidator<OutputRequest>
    {
        public OutputRequestValidator()
        {
            RuleFor(x => x.OutputType)
                .NotEmpty().WithMessage("OutputType is required.")
                .Must(x => x.ToLower() == "album" || x.ToLower() == "track" || x.ToLower() == "artist")
                .WithMessage("OutputType must be 'album', 'track', or 'artist'.");

            When(x => x.OutputType.ToLower() == "album", () =>
            {
                RuleFor(x => x)
                    .Must(x => !string.IsNullOrWhiteSpace(x.Id) || 
                              (!string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.ArtistName)))
                    .WithMessage("Either Album Id or both Name and ArtistName are required for OutputType 'album'.");
            });

            When(x => x.OutputType.ToLower() == "track", () =>
            {
                RuleFor(x => x)
                    .Must(x => !string.IsNullOrWhiteSpace(x.Id) || 
                              (!string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.ArtistName)))
                    .WithMessage("Either Track Id or both Name and ArtistName are required for OutputType 'track'.");
            });

            When(x => x.OutputType.ToLower() == "artist", () =>
            {
                RuleFor(x => x.Id)
                    .NotEmpty().WithMessage("Artist Id is required for OutputType 'artist'.");
            });
        }
    }
}
