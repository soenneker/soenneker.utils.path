using Soenneker.Utils.Path.Abstract;
using Soenneker.Tests.HostedUnit;


namespace Soenneker.Utils.Path.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class PathUtilTests : HostedUnitTest
{
    private readonly IPathUtil _util;

    public PathUtilTests(Host host) : base(host)
    {
        _util = Resolve<IPathUtil>(true);
    }

    [Test]
    public void Default()
    {

    }
}
