﻿using Octopus.Data.Storage.Configuration;
using Octopus.Node.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Node.Extensibility.HostServices.Mapping;
using Octopus.Server.Extensibility.Authentication.Guest.GuestAuth;

namespace Octopus.Server.Extensibility.Authentication.Guest.Configuration
{
    public class GuestConfigurationStore : ExtensionConfigurationStore<GuestConfiguration>, IGuestConfigurationStore
    {
        public static string SingletonId = "authentication-guest";

        readonly IGuestUserStateChecker guestUserStateChecker;

        public GuestConfigurationStore(
            IConfigurationStore configurationStore,
            IGuestUserStateChecker guestUserStateChecker,
            IResourceMappingFactory factory) : base(configurationStore, factory)
        {
            this.guestUserStateChecker = guestUserStateChecker;
        }

        public override string Id => SingletonId;

        public override void SetIsEnabled(bool isEnabled)
        {
            base.SetIsEnabled(isEnabled);
            guestUserStateChecker.EnsureGuestUserIsInCorrectState(isEnabled);
        }
    }
}