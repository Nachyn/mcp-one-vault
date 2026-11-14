using AutoFixture.Xunit2;

namespace OneVault.UnitTests.Shared;

public sealed class InlineAutoMoqDataAttribute(params object?[]? values)
    : InlineAutoDataAttribute(new AutoMoqDataAttribute(), values ?? [null]);