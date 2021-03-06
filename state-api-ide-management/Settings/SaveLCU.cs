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
using LCU.Graphs;
using LCU.Graphs.Registry.Enterprises.IDE;
using System.Collections.Generic;
using LCU.Graphs.Registry.Enterprises;
using Fathym;
using System.Linq;
using System.Net.Http;
using Gremlin.Net.Process.Traversal;
using LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State;
using Fathym;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.Storage.Blob;
using LCU.StateAPI.Utilities;
using LCU.Personas.Client.Applications;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.Settings
{
    [Serializable]
    [DataContract]
    public class SaveLCURequest
    {
        [DataMember]
        public virtual LCUConfig LCU { get; set; }
    }

    public class SaveLCU
    {
        protected ApplicationDeveloperClient appDev;

        protected ApplicationManagerClient appMgr;

        public SaveLCU(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr)
        {
            this.appDev = appDev;
            
            this.appMgr = appMgr;
        }

        [FunctionName("SaveLCU")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = IDEManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<IDESettingsState, SaveLCURequest, IDESettingsStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"Saving LCU: {reqData.LCU}");

                var stateDetails = StateUtils.LoadStateDetails(req);

				await harness.SaveLCU(appDev, appMgr, stateDetails.EnterpriseLookup, stateDetails.Host, reqData.LCU);

                return Status.Success;
            });
        }
    }
}
