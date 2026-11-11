using FluentAssertions;
using Microsoft.Extensions.Configuration;
using server.application.debug;
using Xunit;

namespace server.tests;

public class DebugSettingsTests
{
    [Fact]
    public void FromConfiguration_WhenSectionMissing_UsesDefaults()
    {
        var configuration = new ConfigurationBuilder().Build();

        var settings = DebugSettings.FromConfiguration(configuration);

        settings.Should().Be(new DebugSettings(Enabled: true, AlwaysEnabled: false));
    }

    [Fact]
    public void FromConfiguration_ReadsValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Debug:Enabled"] = "false",
                ["Debug:AlwaysEnabled"] = "true",
            })
            .Build();

        var settings = DebugSettings.FromConfiguration(configuration);

        settings.Should().Be(new DebugSettings(Enabled: false, AlwaysEnabled: true));
    }
}
