using Octopus.Client.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Client.Extensibility.Authentication.Guest.Configuration
{
    public class GuestConfigurationResource : ExtensionConfigurationResource
    {
        public GuestConfigurationResource()
        {
            Id = "authentication-guest";
        }
    }
}