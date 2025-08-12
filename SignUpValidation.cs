using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Sample.ExternalIdentities
{
    public class SignUpValidation
    {
        [Function("SignUpValidation")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("SignUpValidation");
            // Allowed domains
            string[] allowedDomain = ["egmont.com", "powercon.dk"];

            // Check HTTP basic authorization
            if (!Authorize(req, log))
            {
                log.LogWarning("HTTP basic authentication validation failed.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await response.WriteStringAsync(JsonConvert.SerializeObject(new ResponseContent("ShowBlockPage", "Unauthorized.")));
                return response;
            }

            // Get the request body
            string requestBody = await req.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // If input data is null, show block page
            if (data == null)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync(JsonConvert.SerializeObject(new ResponseContent("ShowBlockPage", "There was a problem with your request.")));
                return response;
            }

            // Print out the request body
            log.LogInformation("Request body: " + requestBody);

            // Get the current user language 
            string language = (data.ui_locales == null || data.ui_locales.ToString() == "") ? "default" : data.ui_locales.ToString();
            log.LogInformation($"Current language: {language}");

            // If email claim not found, show block page. Email is required and sent by default.
            if (data.email == null || data.email.ToString() == "" || data.email.ToString().Contains("@") == false)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync(JsonConvert.SerializeObject(new ResponseContent("ShowBlockPage", "Email name is mandatory.")));
                return response;
            }

            // Get domain of email address
            string domain = data.email.ToString().Split("@")[1];

            // Check the domain in the allowed list
            if (!allowedDomain.Contains(domain.ToLower()))
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync(JsonConvert.SerializeObject(new ResponseContent("ShowBlockPage", $"You must have an account from '{string.Join(", ", allowedDomain)}' to register as an external user for Contoso.")));
                return response;
            }

            // If displayName claim doesn't exist, or it is too short, show validation error message. So, user can fix the input data.
            if (data.displayName == null || data.displayName.ToString().Length < 5)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await response.WriteStringAsync(JsonConvert.SerializeObject(new ResponseContent("ValidationError", "Please provide a Display Name with at least five characters.")));
                return response;
            }

            // Input validation passed successfully, return `Allow` response.
            // TO DO: Configure the claims you want to return
            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            var responseContent = new ResponseContent()
            {
                jobTitle = "This value return by the API Connector"
                // You can also return custom claims using extension properties.
                //extension_CustomClaim = "my custom claim response"
            };
            await successResponse.WriteStringAsync(JsonConvert.SerializeObject(responseContent));
            return successResponse;
        }

        private static bool Authorize(HttpRequestData req, ILogger log)
        {
            // Get the environment's credentials 
            string username = System.Environment.GetEnvironmentVariable("BASIC_AUTH_USERNAME", EnvironmentVariableTarget.Process);
            string password = System.Environment.GetEnvironmentVariable("BASIC_AUTH_PASSWORD", EnvironmentVariableTarget.Process);

            // Returns authorized if the username is empty or not exists.
            if (string.IsNullOrEmpty(username))
            {
                log.LogInformation("HTTP basic authentication is not set.");
                return true;
            }

            // Check if the HTTP Authorization header exist
            var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
            {
                log.LogWarning("Missing HTTP basic authentication header.");
                return false;
            }

            // Read the authorization header
            var auth = authHeader;

            // Ensure the type of the authorization header id `Basic`
            if (!auth.StartsWith("Basic "))
            {
                log.LogWarning("HTTP basic authentication header must start with 'Basic '.");
                return false;
            }

            // Get the the HTTP basinc authorization credentials
            var cred = System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(auth.Substring(6))).Split(':');

            // Evaluate the credentials and return the result
            return (cred[0] == username && cred[1] == password);
        }
    }
}
