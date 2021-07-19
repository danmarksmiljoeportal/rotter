using System;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using WSTrustChannel = System.ServiceModel.Security.WSTrustChannel;
using WSTrustChannelFactory = System.ServiceModel.Security.WSTrustChannelFactory;

namespace Rotte.WsTrust
{
    public static class WsTrustClient2013
    {
        private static readonly RotteredenServiceConfiguration Config = RotteredenServiceConfiguration.Get();

        public static SecurityToken RequestSecurityTokenWithX509(
            EndpointAddress stsAddress,
            X509Certificate2 stsEncryptionCert,
            EndpointAddress serviceAddress,
            X509Certificate2 clientCertificate,
            SecurityToken actAsSecurityToken = null)
        {
            // Certificate binding med ws-trust 1.3 og message security
            var stsBinding = new CertificateWSTrustBinding(SecurityMode.TransportWithMessageCredential);
            // Lav channel factory
            var channelFactory = new WSTrustChannelFactory(
                stsBinding,
                stsAddress);

            if (channelFactory.Credentials == null)
                throw new ApplicationException("ChannelFactory credentials cannot be null");

            // Sæt sts'ens encryption cert
            channelFactory.Credentials.ServiceCertificate.ScopedCertificates.Add(
                stsAddress.Uri,
                stsEncryptionCert);

            // Sæt client cert som mapper til en bruger i adfs'en
            channelFactory.Credentials.ClientCertificate.Certificate = clientCertificate;
            channelFactory.TrustVersion = TrustVersion.WSTrust13;
            // Konfigurer Windows Identity Foundation
            // Lav channel og brug act as hvis der er et act as token med
            var channel = (WSTrustChannel)channelFactory.CreateChannel();
            try
            {
                // Lav RST med symmetric keys og saml 1.1 token
                var requestSecurityToken = new RequestSecurityToken(RequestTypes.Issue)
                {
                    AppliesTo = new EndpointReference(serviceAddress.ToString()),
                    TokenType = TokenTypes.Saml11TokenProfile11,
                    KeyType = KeyTypes.Symmetric
                };

                // Påhæft evt. ActAs token
                if (actAsSecurityToken != null)
                    requestSecurityToken.ActAs = new SecurityTokenElement(actAsSecurityToken);

                // Send request (RST) og modtag response (RSTR)
                var securityToken = channel.Issue(requestSecurityToken);

                return securityToken;
            }
            finally
            {
                CloseChannel(channel);
            }
        }
        public static WS2007FederationHttpBinding GetServiceBinding(WS2007HttpBinding stsBinding,
            bool negotiateServiceCertificate)
        {
            var serviceBinding = new WS2007FederationHttpBinding(
                WSFederationHttpSecurityMode.TransportWithMessageCredential,
                false);

            var messageSecurity = serviceBinding.Security.Message;

            // Saml 1.1 tokens
            messageSecurity.IssuedTokenType = TokenTypes.Saml11TokenProfile11;
            // Adresse til STS
            //messageSecurity.IssuerAddress = Constants.StsAddressUserName;
            messageSecurity.IssuerAddress = Config.IdentifyCertificateEndpoint;
            // Negotiation slået fra
            messageSecurity.NegotiateServiceCredential = negotiateServiceCertificate;
            // Symmetric keys
            messageSecurity.IssuedKeyType = SecurityKeyType.SymmetricKey;
            // Binding til STS
            messageSecurity.IssuerBinding = stsBinding;

            return serviceBinding;
        }
        public static void CloseChannel(object channel)
        {
            var communicationObject = (ICommunicationObject)channel;

            if (communicationObject.State == CommunicationState.Created ||
                communicationObject.State == CommunicationState.Opened)
            {
                communicationObject.Close();
            }
            else
            {
                communicationObject.Abort();
            }
        }

        public static WS2007HttpBinding GetStsBinding()
        {
            var stsBinding = new WS2007HttpBinding(SecurityMode.Message);
            stsBinding.Security.Message.EstablishSecurityContext = false;

            return stsBinding;
        }
    }
}
