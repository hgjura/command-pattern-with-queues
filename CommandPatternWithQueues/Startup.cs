using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;

[assembly: FunctionsStartup(typeof(CommandPatternWithQueues.ExecutingFunctions.Startup))]

namespace CommandPatternWithQueues.ExecutingFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            FunctionSettingsEternalDurable.FunctionStartupConfigure(builder);
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionSettingsEternalDurable.FunctionConfigureAppConfiguration(builder);
        }
    }
}
