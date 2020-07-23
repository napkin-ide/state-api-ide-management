using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Fathym;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using LCU.StateAPI.Utilities;
using LCU.Personas.Client.Applications;
using LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.IDE
{
	[Serializable]
	[DataContract]
	public class SetActivityRequest
	{
		[DataMember]
		public virtual string Activity { get; set; }
	}

	public class SetActivity
	{
        protected ApplicationManagerClient appMgr;

        public SetActivity(ApplicationManagerClient appMgr)
        {
            this.appMgr = appMgr;
        }

		[FunctionName("SetActivity")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = IDEManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<IDEState, SetActivityRequest, IDEStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
				log.LogInformation($"Set Activity {reqData.Activity}");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.SetActivity(appMgr, stateDetails.EnterpriseAPIKey, reqData.Activity);

                return Status.Success;
            });
        }
	}
}
