# Introduction 
The function creates a storage account and enabling it to be a static website. it gets a name of storage account to be created within specified resource group.
It expect single parameter to be provided either as a json body, ot as a query parameter. `SiteName=<101yoav104>`
The response is a json structure:
`{"siteName":"<name of the created storage>","connectionString":"<fully qualified connection string to the newly created storage>"}`

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

