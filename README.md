# Logging in to the Rotter service
The following page helps understand what is needed to ensure that your application can access the Rottedatabase web service with a user in context. A specific identifiable user must always be in context as this user must be granted a token by DMP's Identity Provider (henceforth "Identify") so that Rottedatabasen is aware of who the user is and can react and log data according to this context. DMP will no longer accept the use of user certificates (i.e. system to system login). 

As there are big differences in regards to integrating a web application and a desktop application with the Rotter WCF service, we have split this artickle up into two parts that explain each of these in turn.

# 1. How to integrate a web app to the Rotter service

A typical scenario where your web app needs to access the Rotter service (RotteWS) is:

1. A user logs in to your web application using your local identity provider proxied through Identify.
2. The web application sends the token from `#1` to the Identify to exchange for another SAML token. We can call this an ActAs token.
3. The web application uses the ActAs SAML token to access the Rotter service on behalf of the user.

For the sake of simplicity, this guideline is going to use one signing certificate for both #1 and #2. In reality, it is not uncommon that two separate certificates are used.

Identify uses WSTrust 1.4 so your web application needs to use a technology that has support for it such as .NET Framework (4.5 or later is recommended) or Java. Please note that .NET Core does not have sufficient support for WSTrust.

In this guideline, we will be using .NET for sample code. To use the sample application, you need to:

1. Prerequisite: your web application is deployed at **https://mydomain.com**.
2. Prepare a signing certificate that can do digital signature and data encipherment such as FOCES certificates. A self-signed certificate can work as well. In this example, we use a self-signed certificate whose CN is **mydomain.com signing**.
3. Contact DMP to establish a trust relationship between your web application and Identify.
4. Install certificates to the Certificate Store on Windows Server
5. Update the sample's configuration
6. Contact RotteWS from C# code

## 1.1. Create a self-signed certificate

You can create a self-signed certificate using command line per the guideline at https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/certreq_1. Certutil is another tool on Windows that you can use.

Steps to create a certificate are:

1. Download [request.inf](/assets/request.inf) (replace mydomain.com by your domain)
2. Open command prompt with administrator privileges
3. Navigate to the folder that contains request.inf file
  Run `certreq -new request.inf mydomain.com.cer`
  ![image.png](/assets/images/00.png)
4. Open Microsoft Management Console and Certificate Store
  ![image.png](/assets/images/01.png)
5. If you are generating the certificate on the same machine that you want to run the sample, you can skip this step. Otherwise, export the certificate with private key to a file named `mydomain.com.pfx`.
  ![image.png](/assets/images/02.png)
6. Also export the public key to a .cer file. You will need to provide the public key to DMP when you need to establish a trust relationship between your web application and its Miljoeportal.dk identity provider.

## 1.2. Establish a trust relationship between your app and the identity provider

You need to send your **mydomain.com signing** (the .cer file which contains the public key only) and its CN (which is **mydomain.com signing** in this example; actually, the CN value can be found in the .cer file) to DMP so that they can create a trust relationship with your application.

## 1.3. Install certificates to Certificate Store on your server

- Install the **mydomain.com signing** (`mydomain.com.pfx`) to the Personal (My) store
![image.png](/assets/images/05.png)

- Grant the access to the certificate for the IIS_IUSRS user. Right click on the certificate => All Tasks -> Manage Private Keys -> Add IIS_IUSRS user

- Install `log-in.miljoeportal.dk.cer` to the Trusted People store
![image.png](/assets/images/06.png)

## 1.4. Set up website configuration

Looking at Web.Demo.Config and Web.Prod.Config of Demo and Prod of Rottehullet websites as example for configuration, located at `/src/Example/WsFed.Web`.

The configuration must have elements:

1. configSections element
Declare configuration element included in web.config: system.identityModel, system.identityModel.services, RotteredenService

2. RotteredenService element
Contains the information of RotteWS configuration and the STS configuration. You need to use the thumbprint of the **mydomain.com signing** certificate for findValue of the `ActAsCertificate` element.
![image.png](/assets/images/03.png)

- RotteWS/ServiceUrl is RotteWS url
  - Prod: https://services-rottereden.miljoeportal.dk/Service.svc
  - Demo: https://services-rottereden.demo.miljoeportal.dk/Service.svc

- RotteWS/EncryptionCertificate is base64 content of `/certificates/Encryption/services.rottereden.miljoeportal.dk.cer` file.

- Identify/EncryptionCertificate is base64 content of `/certificates/Identify/log-in.miljoeportal.dk.cer` file
![image.png](/assets/images/04.png)

- Identify endpoints: RemoteEndpoint, IssuedTokenEndpoint, UserNameEndpoint, CertificateEndpoint

1. system.identityModel and system.identityModel.services elements
![image.png](/assets/images/07.png)

- Update mydomain.com for audienceUris and wsFederation/realm element
- Update `Thumprint of mydomain.com certificate` using the thumbprint of the **mydomain.com signing** certificate.

1. Update Application_Start event in Global.asax

Just follow exact code as below
![image.png](/assets/images/08.png)

Must enable TLS 1.2 before contacting the service: 
`ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12`

## 1.5. Contact RotteWS from C# code

`Rotte.WsTrust` library project is located `\src\Exmample\Rotte.WsTrust` consists of WsFed authentication and Rotte service contracts code, download it and add to your web app project as library

`Rotte.WsTrust` already add RotteWS service reference by Visual Studio
![image.png](/assets/images/09.png)

In the below code, the `Page_Load` event method shows how get RotteWS service instance from the ActAs security token in the web app
![image.png](/assets/images/10.png)

# 2. How to integrate desktop app to the Rotter service

A typical scenario where your desktop app needs to access the Rotter service (RotteWS) is:

1. Your desktop app authenticates a user against a local identity provider using Windows authentication, or authenticates against Identify using username & password.
2. Your app sends the token in `#1` to Identify to exchange for another SAML token.
3. Your app uses the new SAML token to access the Rotter service on behalf of the user.

Identify uses WSTrust 1.4 so your desktop application needs to use a technology that has support for it such as .NET Framework (4.5 or later is recommended) or Java. Please note that .NET Core does not have sufficient support for WSTrust.

Following step by step to setup WsFed for desktop app:

- Setup application configuration
- Login with DMP (Identify) user
- Login with Windows Auth (ADFS)

To start with, you need to Contact DMP to establish a trust relationship between your application and Identify. The setup between your app and your local identity provider is out of the scope of this guideline.

## 2.1. Setup application configuration

Looking at App.config as example for configuration, located at `/src/Example/WsFed.WpfApp`

The configuration must have elements:

1. configSections element
Declare configuration element included in App.config:  RotteredenService

2. RotteredenService element
Contains the information of RotteWS configuration and Identify configuration
![image.png](/assets/images/11.png)

- RotteWS/ServiceUrl is RotteWS url
\+ Prod: https://services-rottereden.miljoeportal.dk/Service.svc
\+ Demo: https://services-rottereden.demo.miljoeportal.dk/Service.svc

- RotteWS/EncryptionCertificate is base64 content of `/certificates/Encryption/services.rottereden.miljoeportal.dk.cer` file 

- Identify/EncryptionCertificate is base64 content of `/certificates/Identify/log-in.miljoeportal.dk.cer` file

- Identify endpoints: RemoteEndpoint, IssuedTokenEndpoint, UserNameEndpoint, CertificateEndpoint

3. appSettings element

- `PartnerUrl` is url of Partnerorganisationer as example from NatureService, each element is ADFS windows information of each partner: Display name, windows endpoint and identity endpoint
![image.png](/assets/images/12.png)

Must enable TLS 1.2 before contacting the service: `ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12`

And example is for loading Partnerorganisationer from `PartnerUrl` as below code
![image.png](/assets/images/13.png)

`Rotte.WsTrust` library project is located `\src\Exmample\Rotte.WsTrust` consists of WsFed authentication and Rotte service contracts code, download it and add to your desktop application project as libarary
`Rotte.WsTrust` already add RotteWS service reference by Visual Studio

## 2.2. Login with DMP (Identify) user

Using the username and password is provided by Miljoeportal.dk identity provider system to request the token for RotteWS
![image.png](/assets/images/14.png)


## 2.3. Login with Windows Auth (ADFS)

Using Windows domain account of user to get local windows token and coordinate with Identify to issue/exchange the token which access RotteWS. This is overview flow
![image.png](/assets/images/ADFS_Windows_Auth.png)

Here is the checklist (from Nature) to ensure that your ADFS login will work: https://naturappl.blob.core.windows.net/version3/NatureLoginGuideline.pdf

Finally, the code is as follows:
![image.png](/assets/images/15.png)

In example app, selecting Partnerorganisationer to get windows endpoint and identity endpoint to get local windows token `WsFactory.GetLocalWindowsToken` before exchaning Identify token `WsFactory.ExchangeLocalWindowsTokenToIdentifyToken`. Then we create RotteWS from Identify token
