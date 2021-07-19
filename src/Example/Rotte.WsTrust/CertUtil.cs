using System;
using System.Security.Cryptography.X509Certificates;

namespace Rotte.WsTrust
{
    /// <summary>
    /// Utility til at hive certifikater ud af certifikatstore't på maskinen
    /// </summary>
    public static class CertUtil
    {
        /// <summary>
        /// Loader et certifikat ud fra en store location, et navn på storet og en identifikation
        /// af det ønskede certifikat
        /// </summary>
        public static X509Certificate2 GetCertificate(StoreLocation storeLocation,
                                                      StoreName storeName,
                                                      X509FindType x509FindType,
                                                      string findValue)
        {
            var store = new X509Store(storeName, storeLocation);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                var certificates = store.Certificates.Find(x509FindType, findValue, false);

                var queryString = CreateQueryString(storeLocation, storeName,
                                                    x509FindType, findValue);

                if (certificates.Count < 1)
                    throw new ApplicationException("No certificates match query. " + queryString);

                if (certificates.Count > 1)
                    throw new ApplicationException("More than one certificate matches query: " + queryString);

                return certificates[0];
            }
            finally
            {
                store.Close();
            }
        }

        private static string CreateQueryString(StoreLocation storeLocation,
                                                StoreName storeName,
                                                X509FindType x509FindType,
                                                string findValue)
        {
            var queryString = string.Format("StoreLocation: {0}, StoreName: {1}, X509FindType: {2}, FindValue: {3}",
                                            storeLocation,
                                            storeName,
                                            x509FindType,
                                            findValue);

            return queryString;
        }
    }
}
