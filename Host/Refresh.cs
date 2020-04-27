using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Fathym;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Runtime.Serialization;
using Fathym.API;
using System.Collections.Generic;
using System.Linq;
using LCU.Personas.Client.Applications;
using LCU.Personas.Client.Identity;
using LCU.StateAPI.Utilities;
using System.Security.Claims;
using LCU.Personas.Client.Enterprises;
using LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.Host
{
    [Serializable]
    [DataContract]
    public class RefreshRequest : BaseRequest
    { }

    public class Refresh
    {
        protected ApplicationDeveloperClient appDev;

        protected ApplicationManagerClient appMgr;

        protected IdentityManagerClient idMgr;

        public Refresh(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, IdentityManagerClient idMgr)
        {
            this.appDev = appDev;

            this.appMgr = appMgr;

            this.idMgr = idMgr;
        }

        [FunctionName("Refresh")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = IDEManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            var stateDetails = StateUtils.LoadStateDetails(req);

            if (stateDetails.StateKey == "settings")
            {
                return await stateBlob.WithStateHarness<IDESettingsState, RefreshRequest, IDESettingsStateHarness>(req, signalRMessages, log,
                    async (harness, refreshReq, actReq) =>
                {
                    log.LogInformation($"Refresh");
                    
                    await harness.Ensure(appDev, stateDetails.EnterpriseAPIKey);

                    await harness.LoadActivities(appMgr, stateDetails.EnterpriseAPIKey);

                    await harness.ConfigureSideBarEditActivity(appMgr, stateDetails.EnterpriseAPIKey, stateDetails.Host);

                    await harness.LoadLCUs(appMgr, stateDetails.EnterpriseAPIKey);

                    await harness.ClearConfig();

                    return Status.Success;
                });
            }
            else
            {
                return await stateBlob.WithStateHarness<IDEState, RefreshRequest, IDEStateHarness>(req, signalRMessages, log,
                    async (harness, refreshReq, actReq) =>
                {
                    log.LogInformation($"Refresh");

                    await harness.Ensure(appMgr, idMgr, stateDetails.EnterpriseAPIKey, stateDetails.Username);

                    harness.SetUsername(stateDetails.Username);

                    return Status.Success;
                });
            }
        }
    }
}
