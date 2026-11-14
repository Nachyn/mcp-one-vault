using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace OneVault.UnitTests.Shared;

public sealed class AutoMoqDataAttribute() : AutoDataAttribute(CreateFixture)
{
    private static IFixture CreateFixture()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var nextInt = 0;
        var nextLong = 0L;
        fixture.Register(() => Interlocked.Increment(ref nextInt));
        fixture.Register(() => Interlocked.Increment(ref nextLong));
        fixture.Customize<ManualTimeProvider>(options => options.OmitAutoProperties());
        fixture.Register<TimeProvider>(() => fixture.Create<ManualTimeProvider>());
        return fixture;
    }
}