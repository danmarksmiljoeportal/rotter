using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Xml;

namespace Rotte.WsTrust
{
    /// <summary>
    /// Section handler til konflikt service konfigurationen
    /// </summary>
    public class RotteredenConfigurationSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Creates a configuration section handler.
        /// </summary>
        public object Create(object parent, object configContext, XmlNode section)
        {
            var config = new RotteredenServiceConfiguration();

            config.LoadConfiguration(section);

            return config;
        }
    }
    /// <summary>
    /// Configuration for rottereden services
    /// </summary>
    public class RotteredenServiceConfiguration
    {
        /// <summary>
        /// Navnet på konfigurationssektionen i .config
        /// </summary>
        private const string SectionName = "RotteredenService";

        #region Properties

        /// <summary>
        /// Endpoint adresse til DAI tema service
        /// </summary>
        public EndpointAddress RotteWSEndpoint { get; private set; }

        /// <summary>
        /// Public key af tema service certifikatet
        /// </summary>
        public X509Certificate2 RotteWSEncryptionCertificate { get; private set; }

        /// <summary>
        /// Endpoint adresse til Identify user name auth endpoint
        /// </summary>
        public EndpointAddress IdentifyUserNameEndpoint { get; private set; }

        /// <summary>
        /// Endpoint adresse til Identify x509 auth endpoint
        /// </summary>
        public EndpointAddress IdentifyCertificateEndpoint { get; private set; }

        /// <summary>
        /// Public key af service communication certifikatet på Identify
        /// </summary>
        public X509Certificate2 IdentifyEncryptionCertificate { get; private set; }

        public X509Certificate2 ActAsCertificate { get; private set; }

        #endregion

        #region Public

        /// <summary>
        /// Opretter en instans af konfigurationen
        /// </summary>
        public static RotteredenServiceConfiguration Get()
        {
            var config = (RotteredenServiceConfiguration)ConfigurationManager.GetSection(SectionName);

            return config;
        }

        /// <summary>
        /// Indlæser konfigurationen fra web.config
        /// </summary>
        public void LoadConfiguration(XmlNode section)
        {
            if (section == null) throw new ArgumentNullException("section");

            // Diadem
            var rotteredenlement = GetChildElementFromName(section, "RotteWS", true);
            RotteWSEndpoint = CreateEndpointAddress(
                GetChildElementInnerTextAsUri(rotteredenlement, "ServiceUrl", true),
                GetChildElementInnerText(rotteredenlement, "EndpointIdentity", true));
            RotteWSEncryptionCertificate = GetChildElementInnerTextAsX509Certificate(rotteredenlement, "EncryptionCertificate", true);

            // AD FS 2.0 
            var adfs2Element = GetChildElementFromName(section, "Identify", true);
            IdentifyUserNameEndpoint = CreateEndpointAddress(
                GetChildElementInnerTextAsUri(adfs2Element, "UserNameEndpoint", true),
                GetChildElementInnerText(adfs2Element, "EndpointIdentity", true));
            IdentifyCertificateEndpoint = CreateEndpointAddress(
                GetChildElementInnerTextAsUri(adfs2Element, "CertificateEndpoint", true),
                GetChildElementInnerText(adfs2Element, "EndpointIdentity", true));
            IdentifyEncryptionCertificate = GetChildElementInnerTextAsX509Certificate(adfs2Element, "EncryptionCertificate", true);

            // Indlæs act as cert
            var actAsElement = GetChildElementFromName(adfs2Element, "ActAsCertificate", true);
            var storeLocation =
                (StoreLocation)
                Enum.Parse(typeof(StoreLocation), GetAttributeValueAsString(actAsElement, "storeLocation", true));
            var storeName = (StoreName)
                Enum.Parse(typeof(StoreName), GetAttributeValueAsString(actAsElement, "storeName", true));
            var findType = (X509FindType)
                Enum.Parse(typeof(X509FindType), GetAttributeValueAsString(actAsElement, "x509FindType", true));
            var findValue = GetAttributeValueAsString(actAsElement, "findValue", true);

            ActAsCertificate = CertUtil.GetCertificate(storeLocation, storeName, findType, findValue);
        }

        #endregion

        #region Private

        private static XmlElement GetChildElementFromName(XmlNode parentElement, string elementName, bool required)
        {
            if (parentElement == null) throw new ArgumentNullException("parentElement");
            if (elementName == null) throw new ArgumentNullException("elementName");

            var element = parentElement.SelectSingleNode(elementName);

            if (element == null)
            {
                if (required)
                    throw new ApplicationException("Element must exist: " + elementName);

                return null;
            }

            return (XmlElement)element;
        }

        private static string GetChildElementInnerText(XmlNode parentElement, string elementName, bool required)
        {
            var element = GetChildElementFromName(parentElement, elementName, required);

            if (element == null)
                return null;

            var innerText = element.InnerText;

            if (string.IsNullOrEmpty(innerText))
            {
                if (required)
                    throw new ApplicationException("Element inner text cannot be empty: " + elementName);
            }

            return innerText;
        }

        private static int GetChildElementInnerTextAsInt(XmlNode parentElement, string elementName, bool required)
        {
            var innerText = GetChildElementInnerText(parentElement, elementName, required);

            if (string.IsNullOrEmpty(innerText))
                return 0;

            int returnValue;
            if (!int.TryParse(innerText, out returnValue))
                throw new ApplicationException("Value is not a valid integer:" + innerText);

            return returnValue;
        }

        private static Uri GetChildElementInnerTextAsUri(XmlNode parentElement, string elementName, bool required)
        {
            var innerText = GetChildElementInnerText(parentElement, elementName, required);

            if (string.IsNullOrEmpty(innerText))
                return null;

            Uri returnValue;
            if (!Uri.TryCreate(innerText, UriKind.Absolute, out returnValue))
                throw new ApplicationException("Value is not a valid Uri:" + innerText);

            return returnValue;
        }

        private static X509Certificate2 GetChildElementInnerTextAsX509Certificate(XmlNode parentElement, string elementName, bool required)
        {
            var innerText = GetChildElementInnerText(parentElement, elementName, required);

            if (string.IsNullOrEmpty(innerText))
                return null;

            var bytes = Convert.FromBase64String(innerText);

            var certificate = new X509Certificate2(bytes);

            return certificate;
        }

        private static EndpointAddress CreateEndpointAddress(Uri serviceUrl, string dnsName)
        {
            return new EndpointAddress(serviceUrl, EndpointIdentity.CreateDnsIdentity(dnsName));
        }

        private static string GetAttributeValueAsString(XmlElement element, string attributeName, bool required)
        {
            if (element == null) throw new ArgumentNullException("element");
            if (attributeName == null) throw new ArgumentNullException("attributeName");

            var attributeValue = element.GetAttribute(attributeName);

            if (string.IsNullOrEmpty(attributeValue))
            {
                if (required)
                    throw new ApplicationException("Attribute is missing on element: " + element.Name + ". Attribute: " + attributeName);
            }

            return attributeValue;
        }

        #endregion
    }
}
