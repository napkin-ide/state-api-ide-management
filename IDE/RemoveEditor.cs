using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Fathym;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;
using LCU.StateAPI.Utilities;
using LCU.Personas.Client.Applications;
using LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.IDE
{
	[Serializable]
	[DataContract]
	public class RemoveEditorRequest
	{
		[DataMember]
		public virtual string EditorLookup { get; set; }
	}

	public class RemoveEditor
	{
        protected ApplicationManagerClient appMgr;

        public RemoveEditor(ApplicationManagerClient appMgr)
        {
            this.appMgr = appMgr;
        }

		[FunctionName("RemoveEditor")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = IDEManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<IDEState, RemoveEditorRequest, IDEStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
				log.LogInformation($"Removing Editor {reqData.EditorLookup}");

                var stateDetails = StateUtils.LoadStateDetails(req);

				await harness.RemoveEditor(reqData.EditorLookup);

                return Status.Success;
            });
        }
	}
}
