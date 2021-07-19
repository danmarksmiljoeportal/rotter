using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Rotte.WsTrust
{
    /// <summary>
    /// https://github.com/thinktecture/Thinktecture.IdentityModel/blob/master/source/Thinktecture.IdentityModel.Wcf/Bindings/CertificateWSTrustBindings.cs
    /// </summary>
    public class CertificateWSTrustBinding : WSTrustBinding
    {
        public CertificateWSTrustBinding()
            : this(SecurityMode.Message)
        { }

        public CertificateWSTrustBinding(SecurityMode securityMode)
            : base(securityMode)
        { }

        protected override void ApplyTransportSecurity(HttpTransportBindingElement transport)
        {
            transport.AuthenticationScheme = AuthenticationSchemes.Anonymous;

            var element = transport as HttpsTransportBindingElement;
            if (element != null)
            {
                element.RequireClientCertificate = true;
            }
        }

        protected override SecurityBindingElement CreateSecurityBindingElement()
        {
            if (SecurityMode.Message == base.SecurityMode)
            {
                return SecurityBindingElement.CreateMutualCertificateBindingElement();
            }
            if (SecurityMode.TransportWithMessageCredential == base.SecurityMode)
            {
                return SecurityBindingElement.CreateCertificateOverTransportBindingElement();
            }

            return null;
        }
    }
}
