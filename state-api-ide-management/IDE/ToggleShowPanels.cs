using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Fathym;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.WindowsAzure.Storage.Blob;
using LCU.StateAPI.Utilities;
using LCU.Personas.Client.Applications;
using LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.IDE
{
	[Serializable]
	[DataContract]
	public class ToggleShowPanelsRequest
	{
		[DataMember]
		public virtual string Action { get; set; }

		[DataMember]
		public virtual string Group { get; set; }
	}

	public class ToggleShowPanels
    {
        [FunctionName("ToggleShowPanels")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = IDEManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<IDEState, ToggleShowPanelsRequest, IDEStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
				log.LogInformation($"Toggling Show Panels: {reqData.Group} {reqData.Action}");

                var stateDetails = StateUtils.LoadStateDetails(req);

				await harness.ToggleShowPanels(reqData.Group, reqData.Action);

                return Status.Success;
            });
        }
    }
}
