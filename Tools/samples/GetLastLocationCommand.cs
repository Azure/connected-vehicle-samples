namespace My.Extensions
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Vehicle.DataContracts;
    using Microsoft.Azure.Vehicle.Sdk;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;

    [Priority(1)]
    [ClaimRequirement("Services", "TypeA")]
    public class GetLastLocationCommand : Command
    {
        public override string ExtensionName => "GetLastLocationCommand";

        public async override Task<WebCommandResponsePayload> ExecuteCommandAsync(JToken requestBody, RequestDetails requestDetails, RequestMessageHeaders headers, IExtensionGatewayClient client, ILogger log)
        {
            try
            {
                GeoLocation location = await client.GetLastLocationAsync();
                JToken resultObject = location == null ? WebCommandResponsePayload.EmptyPayload : JToken.FromObject(location);
                WebCommandResponsePayload result = new WebCommandResponsePayload(WebCommandStatus.Success, resultObject);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
