# Introduction 
The function creates a storage account and enabling it to be a static website. it gets a name of storage account to be created within specified resource group.
It expect single parameter to be provided either as a json body, ot as a query parameter. `SiteName=<101yoav104>`
The response is a json structure:
`{"siteName":"<name of the created storage>","connectionString":"<fully qualified connection string to the newly created storage>", "endPoint": "<your new site url>"}`

## Limitations
The number of storage accounts per subscription per region is limited, as well as the number of resources per resource group.
Please refer to [documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/azure-subscription-service-limits) for more information.

### Create SPN per subscription
az account set --subscription <name or id>
az ad sp create-for-rbac --sdk-auth > <give it a name>.azureauth

Use the following values from the created file, and include them in the local.settings.json

`"AZURE_AUTH_CLIENT" : "<clientId>",`

`"AZURE_AUTH_SECRET" : "<clientSecret>",`

`"AZURE_AUTH_TENANT" : "<tenantId>",`

`"RG_LOCATION":"<requested region>",`

`"RG_NAME" : "<resource group name>"`

### Create container SAS token for your template website
Lets say, you have a storage account with the default web site, you wish to deploy. The function will copy from the private azure storage, to the newly created $web.
For this function to work, it requires access to the source blob storage. The reason is, I'm using `StartCopyFromUri` method, which does not move the data through the compute. (similar to the way `azcopy copy` works).
So, we need to add two more settings to the local.setting.json file. The fastest way to obtain SAS token to a container is using Azure Storage Explorer, right click on the container and 'Get Shared Access Signeture ...'
add it to your file:
`"SOURCE_SITE_CONTAINER": "sample-site",`

`"SAS" : "<your newly created token>"`


