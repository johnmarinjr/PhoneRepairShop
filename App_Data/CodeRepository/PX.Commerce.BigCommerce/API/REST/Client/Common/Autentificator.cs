using RestSharp;
using RestSharp.Authenticators;
using System.Linq;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class Autentificator : IAuthenticator
    {
        private readonly string _xAuthClient;
        private readonly string _xAuthTocken;

        public Autentificator(string xAuthClient, string xAuthTocken)
        {
            _xAuthClient = xAuthClient;
            _xAuthTocken = xAuthTocken;
        }

        public void Authenticate(IRestClient client, IRestRequest request)
        {
            if(!request.Parameters.Any(x => x.Name == "X-Auth-Client"))
                request.AddHeader("X-Auth-Client", _xAuthClient);
            if (!request.Parameters.Any(x => x.Name == "X-Auth-Token"))
                request.AddHeader("X-Auth-Token", _xAuthTocken);
        }
    }
}
