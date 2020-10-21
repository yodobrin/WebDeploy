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

/*
1. Enable MSI once the function app is deployed
2. Grant access to the newly created MSI to the AzureSQL - this is granted on a server level
3. Grant access to this msi, in the required DB.
DROP USER IF EXISTS [<name of msi>]
GO
CREATE USER [<name of msi>] FROM EXTERNAL PROVIDER;
GO
ALTER ROLE db_datareader ADD MEMBER [<name of msi>];
ALTER ROLE db_datawriter ADD MEMBER [<name of msi>];
GRANT EXECUTE TO [<name of msi>]
GO
**/

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Microsoft.Azure.Services.AppAuthentication;


namespace WebDeploy
{
    public static class QuerySql
    {
        [FunctionName("QuerySql")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            log.LogInformation("QuerySql function processed a request.");

            string ServerName = req.Query["ServerName"];
            string DatabaseName = req.Query["DatabaseName"];
            string query = req.Query["query"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            ServerName = ServerName ?? data?.ServerName;
            DatabaseName = DatabaseName ?? data?.DatabaseName;
            query = query ?? data?.Query;

            log.LogInformation($"QuerySql got parameters :\n Query - {query}\n Database - {DatabaseName}\n ServerName - {ServerName}");


            string ConnectionString = "Server=tcp:" + ServerName + ";Initial Catalog=" + DatabaseName + ";Persist Security Info=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            string result = "check logs for output query";
            

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(Environment.GetEnvironmentVariable("SQL_TOKEN_RESOURCE"));            

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.AccessToken = accessToken;
                connection.Open(); 
                using (SqlCommand sqlCommand1 = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = sqlCommand1.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                log.LogInformation(reader.GetValue(i).ToString());
                            }
                        }

                    }
                }

            }
            return new OkObjectResult($"{result}");
        }
    }
}
