/*
Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment.
THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute the object code form of the Sample Code, provided that. 
You agree: 
	(i) to not use Our name, logo, or trademarks to market Your software product in which the Sample Code is embedded;
    (ii) to include a valid copyright notice on Your software product in which the Sample Code is embedded; and
	(iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims or lawsuits, including attorneys’ fees, that arise or result from the use or distribution of the Sample Code
**/

// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;


using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using System.Collections.Generic;


using System.Text;


namespace webdeploy
{
    public static class DeployWeb
    {
        static string KEY1 = "key1";
        static string WEB = "$web";
        
        [FunctionName("DeployWeb")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("DeployWeb  processing a request.");
            
            IAzure azure = CreateAzureContect(log);
            if(azure == null)
            {
                log.LogInformation("DeployWeb cannot create a azure context");
                return new UnauthorizedResult();
            }
            string siteName = req.Query["SiteName"];            

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            siteName = siteName ?? data?.SiteName;

            string region = Environment.GetEnvironmentVariable("RG_LOCATION");
            string rgName = Environment.GetEnvironmentVariable("RG_NAME");
           

            string saConnection = CreateStaticWeb(siteName, rgName, region, azure, log);

            string endpoint = Upload2Site(saConnection,log);
            
            dynamic result = new System.Dynamic.ExpandoObject();
            result.siteName = siteName;
            result.connectionString = saConnection;
            result.endPoint = endpoint;
            string response = JsonConvert.SerializeObject(result);
            string responseMessage = $"{response}";

            return new OkObjectResult(responseMessage);
        }

        private static string GetStorageConnectionString(string keyValue, string accountName, ILogger log)
        {
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={keyValue};EndpointSuffix=core.windows.net";
            log.LogInformation($"DeployWeb: Created connection string to storage {connectionString}");
            return connectionString;  
        }

        // copy all files from source blob container specified in settings to the target $web container
        private static string Upload2Site(string targetCS, ILogger log)
        {
            string endpoint = string.Empty;
            string sourceCS = Environment.GetEnvironmentVariable("SOURCE_SITE_CS");
            string sourceContainer = Environment.GetEnvironmentVariable("SOURCE_SITE_CONTAINER");

            string sasToken = Environment.GetEnvironmentVariable("SAS");

            log.LogInformation($"DeployWeb::Upload2Site got CS, source: {sourceCS}\n target: {targetCS}");
            BlobServiceClient sourceBlobServiceClient = new BlobServiceClient(sourceCS); 
            BlobServiceClient targetBlobServiceClient = new BlobServiceClient(targetCS); 
            log.LogInformation($"DeployWeb::Upload2Site got 2 blob service clients");

            BlobContainerClient sourceContainerClient = sourceBlobServiceClient.GetBlobContainerClient(sourceContainer); 
            BlobContainerClient targetContainerClient = targetBlobServiceClient.GetBlobContainerClient(WEB);

            // BlobStaticWebsite site = 
            log.LogInformation($"DeployWeb::Upload2Site got 2 blob container clients");

            foreach (BlobItem item in sourceContainerClient.GetBlobs())
            {
 
                BlobClient source = sourceContainerClient.GetBlobClient(item.Name);
                Uri uri = new Uri($"{source.Uri}{sasToken}");
                BlobClient destination = targetContainerClient.GetBlobClient(item.Name);             
                destination.StartCopyFromUri(uri);
                
            }
            
            endpoint = $"https://{targetBlobServiceClient.AccountName}.z16.web.core.windows.net/";

            return endpoint;
        }


        private static string CreateStaticWeb(string storageName, string rgName, string region, IAzure azureContext, ILogger log)
        {
            string connectionString = string.Empty;

            Microsoft.Azure.Management.Storage.Fluent.IStorageAccount storageAccount = 
                        azureContext.StorageAccounts.Define(storageName).
                        WithRegion(region).
                        WithExistingResourceGroup(rgName).
                        WithGeneralPurposeAccountKindV2().
                        Create();
            // Get connection keys
            IReadOnlyList<Microsoft.Azure.Management.Storage.Fluent.Models.StorageAccountKey> saKeys  = storageAccount.GetKeys();
            string key = string.Empty;
            foreach (Microsoft.Azure.Management.Storage.Fluent.Models.StorageAccountKey item in saKeys)
            {
                // Use the key1
                if(item.KeyName.Equals(KEY1)) key = item.Value; 
            }
            // need to set the storage properties to enable static web site
            connectionString = GetStorageConnectionString(key,storageName,log);
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);            
            BlobServiceProperties myProps = blobServiceClient.GetProperties();
        
            myProps.StaticWebsite.Enabled = true;
            myProps.StaticWebsite.IndexDocument = "index.html";            
            myProps.StaticWebsite.ErrorDocument404Path = "error/index.html";  
            log.LogInformation(myProps.StaticWebsite.ToString());
                                  
            
            blobServiceClient.SetProperties(myProps);
            log.LogInformation("DeployWeb: got static web enabled");            

            return connectionString;
        }
        private static IAzure CreateAzureContect(ILogger log)
        {
            string clientId = Environment.GetEnvironmentVariable("AZURE_AUTH_CLIENT");
            string clientSecret = Environment.GetEnvironmentVariable("AZURE_AUTH_SECRET");
            string tenantId = Environment.GetEnvironmentVariable("AZURE_AUTH_TENANT");
            
            try
            {                
                // Authenticate                
                AzureEnvironment environment = AzureEnvironment.AzureGlobalCloud;                
                var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret,tenantId,environment);

                var azure = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                return azure;
            }
            catch (Exception ex)
            {
                log.LogCritical($"Cannot login {clientId} with exception {ex.ToString()}");            
            }
            return null;
        }
    }
}
