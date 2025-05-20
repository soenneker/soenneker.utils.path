using Soenneker.Utils.Path.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;


namespace Soenneker.Utils.Path.Tests;

[Collection("Collection")]
public class PathUtilTests : FixturedUnitTest
{
    private readonly IPathUtil _util;

    public PathUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IPathUtil>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
