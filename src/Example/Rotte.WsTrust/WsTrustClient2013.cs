using System;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Text;
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
            var stsBinding = new WS2007HttpBinding(SecurityMode.TransportWithMessageCredential);
            stsBinding.Security.Message.EstablishSecurityContext = false;

            return stsBinding;
        }

        public static SecurityToken RequestSecurityTokenWithUsername(
            EndpointAddress stsAddress,
            X509Certificate2 stsEncryptionCert,
            EndpointAddress serviceAddress,
            string dmpUserName,
            string dmpPassword)
        {
            // Certificate binding med ws-trust 1.3 og message security
            var stsBinding = new UserNameWSTrustBinding
            {
                SecurityMode = SecurityMode.TransportWithMessageCredential,
                TrustVersion = TrustVersion.WSTrust13
            };

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

            // Sæt client credentials som mapper til en dmp bruger
            channelFactory.Credentials.UserName.UserName = dmpUserName;
            channelFactory.Credentials.UserName.Password = dmpPassword;
            channelFactory.TrustVersion = TrustVersion.WSTrust13;
            // Konfigurer Windows Identity Foundation
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

                // Send request (RST) og modtag response (RSTR)
                var securityToken = channel.Issue(requestSecurityToken);

                return securityToken;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                CloseChannel(channel);
            }
        }

        /// <summary>
        /// Requester et security token fra AD FS 2.0 sts'en via 
        /// user name authentication 
        /// Token'et kan derpå vedhæftes et kald til en service.
        /// Der kan angives
        /// et optionelt ActAs security token. Hvis dette token angives
        /// vil kaldet til STS'en blive et ActAs kald
        /// </summary>
        public static SecurityToken RequestSecurityTokenWithWindowsAuth(
            EndpointAddress stsAddress,
            EndpointAddress serviceAddress)
        {
            // Windows binding og ws-trust 1.3
            var stsBinding = new WindowsWSTrustBinding
            {
                SecurityMode = SecurityMode.Message,
                TrustVersion = TrustVersion.WSTrust13
            };

            // Lav channel factory
            var channelFactory = new WSTrustChannelFactory(
                stsBinding,
                stsAddress);

            if (channelFactory.Credentials == null)
                throw new ApplicationException("ChannelFactory credentials cannot be null");

            // Lav channel og brug act as hvis der er et act as token med
            var channel = (WSTrustChannel)channelFactory.CreateChannel();
            try
            {
                // Lav RST med symmetric keys og saml 2.0 token
                var requestSecurityToken = new RequestSecurityToken(RequestTypes.Issue)
                {
                    AppliesTo = new EndpointReference(serviceAddress.ToString()),
                    TokenType = TokenTypes.Saml11TokenProfile11,
                    KeyType = KeyTypes.Symmetric
                };

                // Send request (RST) og modtag response (RSTR)
                var securityToken = channel.Issue(requestSecurityToken);

                return securityToken;
            }
            finally
            {
                CloseChannel(channel);
            }
        }

        /// <summary>
        /// Requester et security token fra AD FS 2.0 sts'en via 
        /// Issued Token authentication
        /// Token'et kan derpå vedhæftes et kald til en service.
        /// Der kan angives et optionelt ActAs security token. 
        /// Hvis dette token angives vil kaldet til STS'en blive et ActAs kald
        /// </summary>
        public static SecurityToken RequestSecurityTokenWithWindowsAuthToken(
            EndpointAddress stsAddress,
            X509Certificate2 stsEncryptionCert,
            EndpointAddress serviceAddress,
            EndpointAddress localStsAddress,
            SecurityToken windowsAuthToken)
        {
            // What binding should I use here for the issuer binding?
            var stsBinding = CreateWindowsAuthTokenBinding(localStsAddress);

            // Lav channel factory
            var channelFactory = new WSTrustChannelFactory(
                stsBinding,
                stsAddress);

            if (channelFactory.Credentials != null)
            {
                channelFactory.Credentials.ServiceCertificate.ScopedCertificates.Add(
                    stsAddress.Uri, stsEncryptionCert);
            }

            // Lav channel og brug act as hvis der er et act as token med
            var channel = (WSTrustChannel)channelFactory.CreateChannelWithIssuedToken(windowsAuthToken);
            try
            {
                // Lav RST med symmetric keys og saml 2.0 token
                var requestSecurityToken = new RequestSecurityToken(RequestTypes.Issue)
                {
                    AppliesTo = new EndpointReference(serviceAddress.ToString()),
                    TokenType = TokenTypes.Saml11TokenProfile11,
                    KeyType = KeyTypes.Symmetric
                };

                // Send request (RST) og modtag response (RSTR)
                var securityToken = channel.Issue(requestSecurityToken);

                return securityToken;
            }
            finally
            {
                CloseChannel(channel);
            }
        }

        private static CustomBinding CreateWindowsAuthTokenBinding(EndpointAddress localStsAddress)
        {
            // Opret security binding element
            var securityElement = SecurityBindingElement.CreateIssuedTokenForCertificateBindingElement(
                new IssuedSecurityTokenParameters
                {
                    KeySize = 256,
                    // 256 bit keysize
                    KeyType = SecurityKeyType.SymmetricKey,
                    // Symmetriske nøgler
                    TokenType = TokenTypes.Saml11TokenProfile11, // SAML 2.0 tokens
                    IssuerAddress = localStsAddress,
                    IssuerBinding = new WS2007HttpBinding(SecurityMode.Message)
                });
            securityElement.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic256Sha256;
            securityElement.SecurityHeaderLayout = SecurityHeaderLayout.Strict;
            securityElement.IncludeTimestamp = true;
            securityElement.KeyEntropyMode = SecurityKeyEntropyMode.CombinedEntropy;
            securityElement.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncryptAndEncryptSignature;
            securityElement.MessageSecurityVersion =
                MessageSecurityVersion.
                    WSSecurity11WSTrust13WSSecureConversation13WSSecurityPolicy12BasicSecurityProfile10;
            securityElement.RequireSignatureConfirmation = true;

            var textmessageEncoding = new TextMessageEncodingBindingElement
            {
                WriteEncoding = Encoding.UTF8,
                MessageVersion = MessageVersion.Soap12WSAddressing10
            };

            var httpTransport = new HttpTransportBindingElement
            {
                AuthenticationScheme = AuthenticationSchemes.Anonymous,
                ProxyAuthenticationScheme = AuthenticationSchemes.Anonymous
            };

            return new CustomBinding(securityElement, textmessageEncoding, httpTransport);
        }
    }
}
