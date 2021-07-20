using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace WsFed.WpfApp
{
    public class PartnerorganisationerConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsRequired = false, IsKey = false, IsDefaultCollection = true)]
        public PartnerorgCollection Partnerorgs
        {
            get { return ((PartnerorgCollection)(base[""])); }
            set { base["Partnerorgs"] = value; }
        }

        public void ReadXml(XmlReader reader)
        {
            reader.Read();
            this.DeserializeSection(reader);
        }
    }

    [ConfigurationCollection(typeof(Partnerorg), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate,
        AddItemName = "Partnerorg")]
    public class PartnerorgCollection : ConfigurationElementCollection
    {
        public const string ItemPropertyName = "Partnerorg";

        protected override ConfigurationElement CreateNewElement()
        {
            return new Partnerorg();
        }

        protected override string ElementName
        {
            get { return ItemPropertyName; }
        }

        public Partnerorg this[int index]
        {
            get { return (Partnerorg)base.BaseGet(index); }
        }

        protected override bool IsElementName(string elementName)
        {
            return (elementName == "");
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Partnerorg)element).DisplayName;
        }
    }

    public class Partnerorg : ConfigurationElement
    {
        public const string DisplayNameString = "displayName";
        public const string WindowsEndpointString = "windowsEndpoint";
        public const string IdentityString = "identity";

        [ConfigurationProperty(DisplayNameString)]
        public string DisplayName
        {
            get { return (string)base[DisplayNameString]; }
            set { base[DisplayNameString] = value; }
        }

        [ConfigurationProperty(WindowsEndpointString)]
        public string WindowsEndpoint
        {
            get { return (string)base[WindowsEndpointString]; }
            set { base[WindowsEndpointString] = value; }
        }

        [ConfigurationProperty(IdentityString)]
        public string Identity
        {
            get { return (string)base[IdentityString]; }
            set { base[IdentityString] = value; }
        }
    }

    public static class PartnerConfiguration
    {
        public static async Task<List<Partnerorg>> ReadAsync(string partnerUrl)
        {
            var http = new HttpClient();
            var xml = await http.GetStringAsync(partnerUrl).ConfigureAwait(false);
            xml = xml.Replace("\r", string.Empty).Replace("\n", string.Empty);
           
            var section = new PartnerorganisationerConfigurationSection();
            using (var sr = new StringReader(xml))
            {
                using (var xr = XmlReader.Create(sr))
                {
                    section.ReadXml(xr);
                }
            }

            return section.Partnerorgs.Cast<Partnerorg>().OrderBy(p => p.DisplayName).ToList();
        }
    }
}
