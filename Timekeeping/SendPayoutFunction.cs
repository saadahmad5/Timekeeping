using System;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Timekeeping
{
    public class SendPayoutFunction
    {
        [FunctionName("SendPayouts")]
        public async Task RunAsync([TimerTrigger("0 0 18 * * *")] TimerInfo myTimer, ILogger log)
        //public async Task RunAsync([TimerTrigger("0 0 6 * * Fri")] TimerInfo myTimer, ILogger log)
        {
            string commSvcConnStr = Environment.GetEnvironmentVariable("AzureCommServiceConnStr", EnvironmentVariableTarget.Process);
            string cosmosConnStr = Environment.GetEnvironmentVariable("AzureCosmosDBConnStr", EnvironmentVariableTarget.Process);

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            EmailClient emailClient = new EmailClient(commSvcConnStr);

            CosmosClient cosmosClient = new CosmosClient(cosmosConnStr);
            Container employeeContainer = cosmosClient.GetContainer("Timekeeping", "Employees");
            Container timeCardContainer = cosmosClient.GetContainer("Timekeeping", "TimeCards");

            var employeeQueryDefinition = new QueryDefinition("SELECT * FROM c");
            var employeeQueryResultSetIterator = employeeContainer.GetItemQueryIterator<Employee>(employeeQueryDefinition);
            while (employeeQueryResultSetIterator.HasMoreResults)
            {
                var employeeResultSet = await employeeQueryResultSetIterator.ReadNextAsync();
                foreach (Employee employee in employeeResultSet)
                {
                    log.LogInformation($"Starting to compute payout for {employee.uniqueId}");

                    var timeCardQueryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.uniqueId = @uniqueId AND c.date >= @timestamp")
                        .WithParameter("@uniqueId", employee.uniqueId)
                        .WithParameter("@timestamp", DateTime.UtcNow - TimeSpan.FromDays(7)
                    );

                    var timeCardQueryResultSetIterator = timeCardContainer.GetItemQueryIterator<TimeCard>(timeCardQueryDefinition);
                    
                    double cummulativeHours = 0;
                    double totalPay = 0;

                    StringBuilder emailTemplate = new StringBuilder();
                    emailTemplate.AppendLine($"<html><body><h3>Payout for {employee.name} for the week of {DateOnly.FromDateTime(DateTime.Now)}</h3>");

                    while (timeCardQueryResultSetIterator.HasMoreResults)
                    {
                        var timeCardResultSet = await timeCardQueryResultSetIterator.ReadNextAsync();
                        emailTemplate.AppendLine("<br>");
                        foreach (TimeCard timeCard in timeCardResultSet)
                        {
                            log.LogInformation($"\t {timeCard.date} \t\t {timeCard.hours}");
                            emailTemplate.AppendLine($"<p>{timeCard.date} &emsp;&emsp; {timeCard.hours}</p>");
                            cummulativeHours += timeCard.hours;
                        }
                        emailTemplate.AppendLine("<br>");
                    }
                    log.LogInformation($"Total Hours for {employee.name}: \t\t {cummulativeHours} hours");

                    totalPay = cummulativeHours * employee.hourlyWage;

                    emailTemplate.AppendLine($"<h5>Hours worked this period: &emsp;&emsp; {cummulativeHours}</h5>");
                    emailTemplate.AppendLine($"<h4>Total Net Pay: &emsp;&emsp; ${totalPay}</h4></body></html>");

                    emailClient.Send(
                        WaitUntil.Completed,
                        senderAddress: "DoNotReply@1a8251b2-dcd2-42b5-acc8-0fee60381822.azurecomm.net",
                        recipientAddress: employee.emailAddress,
                        subject: $"{employee.name}'s Payout on {DateOnly.FromDateTime(DateTime.Now)}",
                        htmlContent: emailTemplate.ToString()
                    );

                    log.LogInformation($"Payout of ${totalPay} sent to {employee.emailAddress}\n");
                }
            }

        }
    }
}
