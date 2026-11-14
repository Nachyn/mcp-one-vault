using AutoFixture;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OneVault.Integration.Tests.Shared;

public sealed class AutoDataAttribute() : AutoFixture.Xunit2.AutoDataAttribute(CreateFixture)
{
    private static IFixture CreateFixture()
    {
        var fixture = new Fixture();
        fixture.Inject<IFixture>(fixture);
        var nextInt = 0;
        var nextLong = 0L;
        fixture.Register(() => Interlocked.Increment(ref nextInt));
        fixture.Register(() => Interlocked.Increment(ref nextLong));
        fixture.Customize<WebApplicationFactory<Program>>(options => options.OmitAutoProperties());
        return fixture;
    }
}