using Rotte.WsTrust.RotteWS;
using System;
using System.IdentityModel.Services;
using System.IdentityModel.Tokens;
using System.IO;
using System.Security.Claims;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Xml;

namespace Rotte.WsTrust
{
    public class WsFactory
    {
        private static readonly RotteredenServiceConfiguration Config = RotteredenServiceConfiguration.Get();


        public static IService GetRotteWS(SecurityToken securityToken, TimeSpan commontimeout)
        {
            // Lav binding til STS'en
            var stsBinding = WsTrustClient2013.GetStsBinding();

            // Lav binding til servicen
            var serviceBinding = WsTrustClient2013.GetServiceBinding(stsBinding, false);

            serviceBinding.ReaderQuotas.MaxStringContentLength = 2147483647;
            serviceBinding.ReaderQuotas.MaxArrayLength = 2147483647;
            serviceBinding.ReaderQuotas.MaxBytesPerRead = 2147483647;
            serviceBinding.ReaderQuotas.MaxDepth = 2147483647;
            serviceBinding.MaxBufferPoolSize = 6553600;
            serviceBinding.MaxReceivedMessageSize = 2147483647;
            serviceBinding.CloseTimeout = commontimeout;
            serviceBinding.OpenTimeout = commontimeout;
            serviceBinding.ReceiveTimeout = commontimeout;
            serviceBinding.SendTimeout = commontimeout;
            // Lav channel til servicen
            // var channelFactory = new ChannelFactory<IConflictService>(serviceBinding, Constants.ServiceAddress);
            var channelFactory = new ChannelFactory<IService>(serviceBinding, Config.RotteWSEndpoint);

            foreach (OperationDescription op in channelFactory.Endpoint.Contract.Operations)
            {
                var dataContractBehavior = op.Behaviors.Find<DataContractSerializerOperationBehavior>();
                if (dataContractBehavior != null)
                {
                    dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
                }
            }

            if (channelFactory.Credentials == null)
                throw new ApplicationException("ChannelFactory must have credentials");

            //// Deaktiver service cert validering
            //channelFactory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode =
            //    X509CertificateValidationMode.None;
            //channelFactory.Credentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            channelFactory.Credentials.ServiceCertificate.ScopedCertificates.Add(Config.RotteWSEndpoint.Uri, Config.RotteWSEncryptionCertificate);
            channelFactory.Credentials.UseIdentityConfiguration = true;
            // Konfigurer Windows Identity Foundation
            //channelFactory.ConfigureChannelFactory();

            // Påhæft token til kald til service
            return channelFactory.CreateChannelWithIssuedToken(securityToken);
        }

        public static SecurityToken GetIdentifyActAsToken()
        {
            var identity = Thread.CurrentPrincipal.Identity as ClaimsIdentity;
            if (identity == null)
            {
                throw new ApplicationException("User didn't login yet");
            }

            var bc = identity.BootstrapContext as BootstrapContext;
            if (bc == null)
            {
                throw new ApplicationException("Cannot get boostrap context from current identity.");
            }

            return WsTrustClient2013.RequestSecurityTokenWithX509(
                Config.IdentifyCertificateEndpoint,
                Config.IdentifyEncryptionCertificate,
                Config.RotteWSEndpoint,
                Config.ActAsCertificate,
                EnsureBootstrapSecurityToken(bc));
        }

        private static SecurityToken EnsureBootstrapSecurityToken(BootstrapContext bootstrapContext)
        {
            if (bootstrapContext.SecurityToken != null)
            {
                return bootstrapContext.SecurityToken;
            }
            if (string.IsNullOrWhiteSpace(bootstrapContext.Token))
            {
                return null;
            }
            var handlers = FederatedAuthentication.FederationConfiguration.IdentityConfiguration.SecurityTokenHandlers;
            return handlers.ReadToken(new XmlTextReader(new StringReader(bootstrapContext.Token)));
        }

        public static SecurityToken GetIdentifyUserToken(string username, string password)
        {
            return WsTrustClient2013.RequestSecurityTokenWithUsername(
                Config.IdentifyUserNameEndpoint,
                Config.IdentifyEncryptionCertificate,
                Config.RotteWSEndpoint,
                username, password);
        }

        public static SecurityToken GetLocalWindowsToken(string windowsEndpoint, string identity)
        {
            var localStsAddress = new EndpointAddress(new Uri(windowsEndpoint), 
                EndpointIdentity.CreateDnsIdentity(identity));

            // Get local security token using Windows Authentication, 
            // since we are logged on the domain via the local IdP
            return WsTrustClient2013.RequestSecurityTokenWithWindowsAuth(
                localStsAddress, Config.IdentifyRemoteEndpoint);
        }

        public static SecurityToken ExchangeLocalWindowsTokenToIdentifyToken(
            string windowsEndpoint, string identity, SecurityToken localToken)
        {
            var localStsAddress = new EndpointAddress(new Uri(windowsEndpoint), 
                EndpointIdentity.CreateDnsIdentity(identity));

            return WsTrustClient2013.RequestSecurityTokenWithWindowsAuthToken(
                Config.IdentifyIssuedTokenEndpoint,
                Config.IdentifyEncryptionCertificate,
                Config.RotteWSEndpoint,
                localStsAddress,
                localToken);
        }
    }
}
