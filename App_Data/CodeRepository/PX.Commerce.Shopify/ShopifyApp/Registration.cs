using Autofac;
using Autofac.Core;
using IdentityServer4;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PX.Commerce.Shopify
{
	public class ServiceRegistration : Module
    {
		protected override void Load(ContainerBuilder builder)
		{
			Client shopifyAppClient = ShopifyAppConnectedApplication.ShopifyAppClientForRegistration();

			builder
				.RegisterInstance(shopifyAppClient)
				.Keyed<Client>(Guid.Parse(ShopifyAppConnectedApplication.ShopifyAppClientId));
		}
	}
}
