## 1. How to integrate with a web app
Currently, WsFed (SOAP) only supports .NET Framework (recommended 4.5 or later), doesn't work with other Framework: NET Core ... or languages: java and PHP ...

Following step by step to setup WsFed for ASP.NET .NET Framework:
- Create or buy new domain, eg mydomain.com
- Create the encryption certificate with CN: mydomain.com
- Use the domain and encryption certificate to create Identify connection
- Install the certificates to Certificate Store on Server/Local PC 
- Update website configuration
- Contact RotteWS from C# code 

#### 1.1. Create or buy new domain, eg mydomain.com
The client or DMP buy new domain from hosting vendor or provider, this domain is identity endpoint for Identify connection

#### 1.2. Create the encryption certificate with CN: mydomain.com

We can create self-certificate by command prompt, then export private certificate (.pfx) and public certificate based 64 (.cer) files. The guideline at here https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/certreq_1

Following steps:
- Download [request.inf](/assets/request.inf) (replace mydomain.com by your domain)
- Open command prompt with administrator privileges
- Navigate to cmd folder to the folder that contains request.inf file
-  Run `certreq -new request.inf mydomain.com.cer`
![image.png](/assets/images/00.png)
- Open Microsoft Management Console and Certificate Store
![image.png](/assets/images/01.png)
- Export `mydomain.com.pfx` with private key
![image.png](/assets/images/02.png)

#### 1.3. Use the domain and encryption certificate to create Identify connection
Sending new domain and `mydomain.com.pfx` file to DMP to create the Identify connection

#### 1.4. Install the certificates to Certificate Store on Server
RotteWS Encryption, Identify Encryption and ActAs certificate are located at ` /certificates` folder and contact DMP to get the password of `Rottereden_ActAs_prod.pfx` and `Rottereden_ActAs_test.pfx`

- Install `mydomain.com.pfx`, `Rottereden_ActAs_prod.pfx` and `Rottereden_ActAs_test.pfx` to Personal (My) store
![image.png](/assets/images/05.png)

- Grant the access for the both certificates to IIS_IUSRS user. Right click the certificate => All Tasks -> Manage Private Keys -> Add IIS_IUSRS user

- Install `log-in.miljoeportal.dk.cer` to Trust People store
![image.png](/assets/images/06.png)

#### 1.5. Setup website configuration

Looking at Web.Demo.Config and Web.Prod.Config of Demo and Prod of Rottehullet websites as example for configuration, located at `/src/Example/WsFed.Web`

The configuration must have elements:

1. configSections element 
Declare configuration element included in web.config: system.identityModel, system.identityModel.services, RotteredenService

2. RotteredenService element
Contains the information of RotteWS configuration and Identify configuration
![image.png](/assets/images/03.png)

- RotteWS/ServiceUrl is RotteWS url
\+ Prod: https://services-rottereden.miljoeportal.dk/Service.svc
\+ Demo: https://services-rottereden.demo.miljoeportal.dk/Service.svc

- RotteWS/EncryptionCertificate is based64 content of `/certificates/Encryption/services.rottereden.miljoeportal.dk.cer` file 

- Identify/EncryptionCertificate is based64 content of `/certificates/Identify/log-in.miljoeportal.dk.cer` file
![image.png](/assets/images/04.png)

- Identify endpoints: RemoteEndpoint, IssuedTokenEndpoint, UserNameEndpoint, CertificateEndpoint

3. system.identityModel and system.identityModel.services elements
![image.png](/assets/images/07.png)

- Update mydomain.com for audienceUris and wsFederation/realm element
- Update `Thumprint of mydomain.com certificate` which already install to People (My) store above step

4. Update Application_Start event in Global.asax
Just follow exact code as below
![image.png](/assets/images/08.png)

Must enable TLS 1.2 before contacting the service: 
`ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12`

#### 1.6. Contact RotteWS from C# code 
`Rotte.WsTrust` library project is located `\src\Exmample\Rotte.WsTrust` consists of WsFed authentication and Rotte service contracts code, download it and add to your web app project as libarary
`Rotte.WsTrust` already add RotteWS service reference by Visual Studio
![image.png](/assets/images/09.png)


In below code, `Page_Load` event method shows how get RotteWS service instance from ActAs security token in the web app 
![image.png](/assets/images/10.png)

## 2. How to integrate with a desktop app
As Web app, WsFed (SOAP) on Desktop app only supports .NET Framework (recommended 4.5 or later), doesn't work with other Framework: NET Core ... or languages: java and PHP ...

Following steps:
- Setup application configuration
- Login with DMP (Identify) user

#### 2.1. Setup application configuration
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

- RotteWS/EncryptionCertificate is based64 content of `/certificates/Encryption/services.rottereden.miljoeportal.dk.cer` file 

- Identify/EncryptionCertificate is based64 content of `/certificates/Identify/log-in.miljoeportal.dk.cer` file

- Identify endpoints: RemoteEndpoint, IssuedTokenEndpoint, UserNameEndpoint, CertificateEndpoint

3. appSettings element
- `PartnerUrl` is url of Partnerorganisationer as example from NatureService, each element is ADFS windows information of each partner: Display name, windows endpoint and identity endpoint
![image.png](/assets/images/12.png)


Must enable TLS 1.2 before contacting the service: `ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12` 
And example is for loading Partnerorganisationer from `PartnerUrl` as below code
![image.png](/assets/images/13.png)

`Rotte.WsTrust` library project is located `\src\Exmample\Rotte.WsTrust` consists of WsFed authentication and Rotte service contracts code, download it and add to your desktop application project as libarary
`Rotte.WsTrust` already add RotteWS service reference by Visual Studio

#### 2.2. Login with DMP (Identify) user
Using the username and password is provided by DMP Identify system to request the token for RotteWS
![image.png](/assets/images/14.png)


#### 2.3. Login with Windows Auth (ADFS)
Using Windows domain account of user to get local windows token and coordinate with Identify to issue/exchange the token which access RotteWS. This is overview flow
![image.png](/assets/images/ADFS_Windows_Auth.png)

Here is the checklist (from Nature) to ensure that your ADFS login will work: https://naturappl.blob.core.windows.net/version3/NatureLoginGuideline.pdf

Finally, the code is as follows:
![image.png](/assets/images/15.png)

In example app, selecting Partnerorganisationer to get windows endpoint and identity endpoint to get local windows token `WsFactory.GetLocalWindowsToken` before exchaning Identify token `WsFactory.ExchangeLocalWindowsTokenToIdentifyToken`. Then we create RotteWS from Identify token


