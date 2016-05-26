using GridDomain.Node.Configuration;

namespace GridDomain.Tests.Acceptance
{
    class AutoTestAkkaNetworkConfiguration : IAkkaNetworkConfiguration
    {
        public string Name => "LocalSystem";
        public string Host => "127.0.0.1";
        public int PortNumber => 8080;
    }
}