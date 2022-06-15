using Autofac;
using Autofac.Core;
using IdentityServer4;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PX.Commerce.BigCommerce
{
	public class ServiceRegistration : Module
    {
		protected override void Load(ContainerBuilder builder)
		{
			Client bigCommerceAppClient = BigCommerceAppConnectedApplication.BigCommerceAppClientForRegistration();

			builder
				.RegisterInstance(bigCommerceAppClient)
				.Keyed<Client>(Guid.Parse(BigCommerceAppConnectedApplication.BigCommerceAppClientId));
		}
	}
}
