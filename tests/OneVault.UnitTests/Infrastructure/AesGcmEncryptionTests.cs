using System.Security.Cryptography;
using System.Text;
using OneVault.Infrastructure;
using OneVault.UnitTests.Shared;

namespace OneVault.UnitTests.Infrastructure;

public class AesGcmEncryptionTests
{
    [Theory]
    [AutoMoqData]
    public void RoundTripsGeneratedText(string text, AesGcmEncryption sut)
    {
        using (sut)
        {
            var payload = sut.Encrypt(text);
            sut.Decrypt(payload).Should().Be(text);
            sut.Encrypt(text).Nonce.Should().NotEqual(payload.Nonce);
            payload.Ciphertext.Should().NotEqual(Encoding.UTF8.GetBytes(text));
        }
    }

    [Theory]
    [InlineAutoMoqData("")]
    [InlineAutoMoqData("Секрет 🔑\nsecond line")]
    public void RoundTripsEmptyAndUnicodeText(string text, AesGcmEncryption sut)
    {
        using (sut)
        {
            sut.Decrypt(sut.Encrypt(text)).Should().Be(text);
        }
    }

    [Theory]
    [InlineAutoMoqData("ciphertext")]
    [InlineAutoMoqData("tag")]
    [InlineAutoMoqData("nonce")]
    public void RejectsTampering(string field, string text, AesGcmEncryption sut)
    {
        using (sut)
        {
            var payload = sut.Encrypt(text);
            var bytes = field switch
            {
                "tag" => payload.Tag, "nonce" => payload.Nonce, _ => payload.Ciphertext
            };
            bytes[0] ^= 1;

            var act = () => sut.Decrypt(payload);

            act.Should().Throw<CryptographicException>();
        }
    }

    [Theory]
    [AutoMoqData]
    public void DifferentProcessKeysCannotDecrypt(string text, AesGcmEncryption first,
        AesGcmEncryption second)
    {
        using (first)
        using (second)
        {
            var payload = first.Encrypt(text);
            var act = () => second.Decrypt(payload);
            act.Should().Throw<CryptographicException>();
        }
    }
}