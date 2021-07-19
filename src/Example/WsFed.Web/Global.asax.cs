using System;
using System.Collections.Generic;
using System.IdentityModel;
using System.IdentityModel.Services;
using System.IdentityModel.Tokens;
using System.Net;

namespace WsFed.Web
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            FederatedAuthentication.FederationConfigurationCreated +=
                FederatedAuthentication_FederationConfigurationCreated;
        }

        private void FederatedAuthentication_FederationConfigurationCreated(object sender, System.IdentityModel.Services.Configuration.FederationConfigurationCreatedEventArgs e)
        {
            var serviceCertificate = e.FederationConfiguration.ServiceCertificate;
            var sessionTransforms = new List<CookieTransform>(new CookieTransform[]
            {
                new DeflateCookieTransform(),
                new RsaEncryptionCookieTransform(serviceCertificate),
                new RsaSignatureCookieTransform(serviceCertificate)
            });
            var sessionHandler = new SessionSecurityTokenHandler(sessionTransforms.AsReadOnly());
            e.FederationConfiguration.IdentityConfiguration
                .SecurityTokenHandlers.AddOrReplace(sessionHandler);
        }
    }
}