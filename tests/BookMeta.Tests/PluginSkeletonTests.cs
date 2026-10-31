using Xunit;

namespace BookMeta.Tests;

public class PluginSkeletonTests
{
    [Fact]
    public void PluginId_EstStable()
    {
        Assert.Equal(new Guid("097ed1ba-f0ae-496f-8f56-e54599fe54b1"), Plugin.PluginId);
    }

    [Fact]
    public void Configuration_ParDefaut_ActiveExtractionEpub()
    {
        Assert.True(new PluginConfiguration().EnableEpubCoverExtraction);
    }
}
