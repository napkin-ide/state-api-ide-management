using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using LCU.StateAPI;
using Microsoft.WindowsAzure.Storage.Blob;
using LCU.StateAPI.Utilities;
using LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.Host
{
    public class ConnectToState
    {
        [FunctionName("ConnectToState")]
        public static async Task<ConnectToStateResponse> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req, ILogger log,
            ClaimsPrincipal claimsPrincipal, //[LCUStateDetails]StateDetails stateDetails,
            [SignalR(HubName = IDEManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [SignalR(HubName = IDEManagementState.HUB_NAME)]IAsyncCollector<SignalRGroupAction> signalRGroupActions,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            var stateDetails = StateUtils.LoadStateDetails(req);

            if (stateDetails.StateKey == "settings")
                return await signalRMessages.ConnectToState<IDESettingsState>(req, log, claimsPrincipal, stateBlob, signalRGroupActions);
            else
                return await signalRMessages.ConnectToState<IDEState>(req, log, claimsPrincipal, stateBlob, signalRGroupActions);
        }
    }
}
