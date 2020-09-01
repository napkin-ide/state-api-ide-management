using Fathym;
using LCU.Personas.Client.Applications;
using LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State;
using LCU.StateAPI.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Blob;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.IDE
{
	[Serializable]
	[DataContract]
	public class SelectSideBarActionRequest
	{
		[DataMember]
		public virtual string Action { get; set; }

		[DataMember]
		public virtual string Group { get; set; }

		[DataMember]
		public virtual string Section { get; set; }
	}

	public class SelectSideBarAction
	{
        protected ApplicationManagerClient appMgr;

        public SelectSideBarAction(ApplicationManagerClient appMgr)
        {
            this.appMgr = appMgr;
        }

		[FunctionName("SelectSideBarAction")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = IDEManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<IDEState, SelectSideBarActionRequest, IDEStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
				log.LogInformation($"Selecting SideBar Action: {reqData.Group} {reqData.Action} {reqData.Section}");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.SelectSideBarAction(appMgr, stateDetails.EnterpriseLookup, reqData.Group, reqData.Action, reqData.Section);

                return Status.Success;
            });
        }
	}
}
