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
- Open Microsoft Management Console and Certificate Store
![image.png](/assets/images/01.png)
- Export `mydomain.com.pfx` with private key
![image.png](/assets/images/02.png)

#### 1.3. Use the domain and encryption certificate to create Identify connection
Sending new domain and `mydomain.com.pfx` file to DMP to create the Identify connection

#### 1.4. Install the certificates to Certificate Store on Server
RotteWS Encryption, Identify Encryption and ActAs certificate are located at ` /certificates` folder and contact DMP to get the password of `Rottereden_ActAs_prod.pfx` and `Rottereden_ActAs_test.pfx`

- Install `mydomain.com.pfx`, `Rottereden_ActAs_prod.pfx` and `Rottereden_ActAs_test.pfx` to Personal (My) store, then grant the access for the both certificates to IIS_IUSRS user
![image.png](/assets/images/05.png)

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

3. system.identityModel and system.identityModel.services elements
![image.png](/assets/images/07.png)

- Update mydomain.com for audienceUris and wsFederation/realm element
- Update `Thumprint of mydomain.com certificate` which already install to People (My) store above step

4. Update Application_Start event in Global.asax
Just follow exact code as below
![image.png](/assets/images/08.png)


#### 1.6. Contact RotteWS from C# code 
`Rotte.WsTrust` library project is located `\src\Exmample\Rotte.WsTrust` consists of WsFed authentication and Rotte service contracts code, download it and add to your project as libarary
`Rotte.WsTrust` already add RotteWS service reference by Visual Studio
![image.png](/assets/images/09.png)


In below code, `Page_Load` event method shows how get RotteWS service instance from ActAs security token in the web app 
![image.png](/assets/images/10.png)

## 2. How to integrate with a desktop app

