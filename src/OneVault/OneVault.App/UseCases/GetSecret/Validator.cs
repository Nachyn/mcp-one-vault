using FluentValidation;

namespace OneVault.App.UseCases.GetSecret;

public static partial class GetSecret
{
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Token).Cascade(CascadeMode.Stop).NotEmpty().Length(43)
                .Must(token => token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
                .WithMessage("Invalid secret token.");
        }
    }
}