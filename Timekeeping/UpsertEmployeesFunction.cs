using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Web.Http;

namespace Timekeeping
{
    public static class UpsertEmployeesFunction
    {
        [FunctionName("UpsertEmployees")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string cosmosConnStr = Environment.GetEnvironmentVariable("AzureCosmosDBConnStr", EnvironmentVariableTarget.Process);

            log.LogInformation("C# HTTP trigger -- Upsert Employee");

            Employee employee = new Employee();
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation($"Payload in request: {requestBody}");

                employee = JsonConvert.DeserializeObject<Employee>(requestBody);
            }
            catch (Exception ex)
            {
                log.LogError($"Error in deserializing: {ex.Message}");
                return new InternalServerErrorResult();
            }

            try
            {
                CosmosClient cosmosClient = new CosmosClient(cosmosConnStr);
                Container container = cosmosClient.GetContainer("Timekeeping", "Employees");
                await container.CreateItemAsync(employee);
            }
            catch (Exception e)
            {
                log.LogError($"Error: {e.Message}");
                return new InternalServerErrorResult();
            }

            return new OkObjectResult($"{employee.name}'s employee Data written to Cosmos DB");

        }
    }
}
