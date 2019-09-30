# media-services-v3
Windows Azure Media Services v3.

Purpouse of this project is to do the following:
* Upload video / Inject from blob
* Encode video
* Download vider / Export to blob
* Clean any remaining AMS resources

All in .NET Core using the v3 of the Azure Media Services SDK

Not included in git is the video named ignire.mp4, placed in media-services-v3\media-services-v3 folder and a appsettings.json in same folder. 
Appsettings should be:
```json
{
    "AadClientId": "<guid>", <- App registration in Azure AD
    "AadSecret": "<password>", <- App registration in Azure AD
    "AadEndpoint": "https://login.microsoftonline.com",
    "AadTenantId": "<guid>", <- Azure AD 
    "AccountName": "<media service name>", <- Azure name of AMS
    "ArmAadAudience": "https://management.core.windows.net/",
    "ArmEndpoint": "https://management.azure.com/",
    "Region": "<region>", <- Azure region where AMS is
    "ResourceGroup": "<name>", <- Aure resource group where AWM is
    "SubscriptionId": "<guid>" <- Azure subscription
}
```