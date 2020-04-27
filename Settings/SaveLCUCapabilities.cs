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
using System.Collections.Generic;
using LCU.Graphs;
using LCU.Graphs.Registry.Enterprises.IDE;
using System.Linq;
using LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State;
using Fathym;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.WindowsAzure.Storage.Blob;
using LCU.StateAPI.Utilities;
using LCU.Personas.Client.Applications;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.Settings
{
    [Serializable]
    [DataContract]
    public class SaveLCUCapabilitiesRequest
    {
        [DataMember]
        public virtual LowCodeUnitConfiguration LCUConfig { get; set; }

        [DataMember]
        public virtual string LCULookup { get; set; }
    }

    public class SaveLCUCapabilities
    {
        protected ApplicationDeveloperClient appDev;

        protected ApplicationManagerClient appMgr;

        public SaveLCUCapabilities(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr)
        {
            this.appDev = appDev;
            
            this.appMgr = appMgr;
        }

        [FunctionName("SaveLCUCapabilities")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = IDEManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<IDESettingsState, SaveLCUCapabilitiesRequest, IDESettingsStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"Saving LCU Capabilities: {reqData.LCULookup} {reqData.LCUConfig}");

                var stateDetails = StateUtils.LoadStateDetails(req);

				await harness.SaveLCUCapabilities(appDev, appMgr, stateDetails.EnterpriseAPIKey, stateDetails.Host, reqData.LCULookup, reqData.LCUConfig);

                return Status.Success;
            });
        }
    }
}
