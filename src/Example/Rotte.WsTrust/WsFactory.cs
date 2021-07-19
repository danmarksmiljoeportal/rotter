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

        public static SecurityToken GetActAsSecurityToken()
        {
            return GetSecurityToken(Config.RotteWSEndpoint);
        }

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

        private static SecurityToken GetSecurityToken(EndpointAddress endpointAddress)
        {
            // Tag logon principal
            var identity = Thread.CurrentPrincipal.Identity as ClaimsIdentity;
            if (identity == null) throw new ApplicationException("ClaimsPrincipal must exist");
            // Hiv fat i token'et
            var bc = identity.BootstrapContext as BootstrapContext;
            if (bc == null)
                throw new ApplicationException("Cannot get boostrap context from current identity.");
            var newToken = WsTrustClient2013.RequestSecurityTokenWithX509(
                Config.IdentifyCertificateEndpoint,
                Config.IdentifyEncryptionCertificate,
                endpointAddress,
                Config.ActAsCertificate,
                EnsureBootstrapSecurityToken(bc));
            return newToken;
        }

        private static SecurityToken EnsureBootstrapSecurityToken(BootstrapContext bootstrapContext)
        {
            if (bootstrapContext.SecurityToken != null)
                return bootstrapContext.SecurityToken;
            if (string.IsNullOrWhiteSpace(bootstrapContext.Token))
                return null;
            var handlers = FederatedAuthentication.FederationConfiguration.IdentityConfiguration.SecurityTokenHandlers;
            return handlers.ReadToken(new XmlTextReader(new StringReader(bootstrapContext.Token)));
        }
    }
}
