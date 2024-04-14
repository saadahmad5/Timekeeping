using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Web.Http;
using Microsoft.Azure.Cosmos;

namespace Timekeeping
{
    public static class InsertTimeCardFunction
    {
        [FunctionName("InsertWorkHours")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string cosmosConnStr = Environment.GetEnvironmentVariable("AzureCosmosDBConnStr", EnvironmentVariableTarget.Process);

            log.LogInformation("C# HTTP trigger -- Insert TimeCard");

            TimeCard timeCard = new TimeCard();
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation($"Payload in request: {requestBody}");

                timeCard = JsonConvert.DeserializeObject<TimeCard>(requestBody);
            }
            catch (Exception ex)
            {
                log.LogError($"Error in deserializing: {ex.Message}");
                return new InternalServerErrorResult();
            }

            try
            {
                CosmosClient cosmosClient = new CosmosClient(cosmosConnStr);
                Container container = cosmosClient.GetContainer("Timekeeping", "TimeCards");
                await container.CreateItemAsync(timeCard);
            }
            catch (Exception e)
            {
                log.LogError($"Error: {e.Message}");
                return new InternalServerErrorResult();
            }

            return new OkObjectResult($"{timeCard.uniqueId} time card was successfully inserted for {timeCard.date}");
        }
    }
}
