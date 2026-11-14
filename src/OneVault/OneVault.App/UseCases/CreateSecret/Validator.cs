using System.Text;
using FluentValidation;
using OneVault.App.Rules;

namespace OneVault.App.UseCases.CreateSecret;

public static partial class CreateSecret
{
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly UTF8Encoding Utf8 = new(false, true);

        public Validator()
        {
            RuleFor(x => x.Text).Cascade(CascadeMode.Stop).NotEmpty()
                .Must(text => !string.IsNullOrWhiteSpace(text))
                .WithMessage("Text must not be blank.")
                .Must(IsValidText)
                .WithMessage("Text must be valid Unicode and at most 65536 UTF-8 bytes.");
            RuleFor(x => x.TtlSeconds)
                .InclusiveBetween(SecretRules.MinTtlSeconds, SecretRules.MaxTtlSeconds);
        }

        private static bool IsValidText(string text)
        {
            try
            {
                return Utf8.GetByteCount(text) <= SecretRules.MaxTextBytes;
            }
            catch (EncoderFallbackException)
            {
                return false;
            }
        }
    }
}