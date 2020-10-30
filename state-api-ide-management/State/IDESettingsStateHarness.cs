using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Fathym;
using LCU.Presentation.State.ReqRes;
using LCU.StateAPI.Utilities;
using LCU.StateAPI;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Collections.Generic;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.DevOps;
using LCU.Personas.Enterprises;
using LCU.Personas.Client.Applications;
using LCU.Personas.Client.Identity;
using Fathym.API;
using LCU.Graphs.Registry.Enterprises.IDE;
using LCU.Graphs.Registry.Enterprises.DataFlows;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State
{
    public class IDESettingsStateHarness : LCUStateHarness<IDESettingsState>
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public IDESettingsStateHarness(IDESettingsState state, ILogger log)
            : base(state ?? new IDESettingsState(), log)
        { }
        #endregion

        #region API Methods
        public virtual async Task AddDefaultDataAppsLCUs(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, string host)
        {
            var nideConfigured = await appDev.ConfigureNapkinIDEForDataApps(entLookup);

            if (nideConfigured.Status)
            {
                await LoadActivities(appMgr, entLookup);

                await LoadSideBarSections(appMgr, entLookup);

                await LoadSecionActions(appMgr, entLookup);

                await LoadLCUs(appMgr, entLookup);
            }
        }

        public virtual async Task AddDefaultDataFlowLCUs(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, string host)
        {
            var nideConfigured = await appDev.ConfigureNapkinIDEForDataFlows(entLookup);

            if (nideConfigured.Status)
            {
                await LoadActivities(appMgr, entLookup);

                await LoadSideBarSections(appMgr, entLookup);

                await LoadSecionActions(appMgr, entLookup);

                await LoadLCUs(appMgr, entLookup);
            }
        }

        public virtual async Task AddSideBarSection(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, string section)
        {
            await appDev.AddSideBarSection(section, entLookup, State.SideBarEditActivity);

            await LoadSideBarSections(appMgr, entLookup);
        }

        public virtual async Task ClearConfig()
        {
            State.Config.CurrentLCUConfig = null;

            State.Config.LCUConfig = new LowCodeUnitConfiguration();

            State.Config.ActiveFiles = new List<string>();

            State.Config.ActiveModules = new ModulePackSetup();

            State.Config.ActiveSolutions = new List<IdeSettingsConfigSolution>();
        }

        public virtual async Task ConfigureSideBarEditActivity(ApplicationManagerClient appMgr, string entLookup, string host)
        {
            if (!State.SideBarEditActivity.IsNullOrEmpty())
            {
                var sidBarSections = await appMgr.LoadIDESideBarSections(entLookup, State.SideBarEditActivity);

                State.SideBarSections = sidBarSections.Model;

                if (!State.EditSection.IsNullOrEmpty())
                {
                    var sidBarActions = await appMgr.LoadSectionActions(entLookup, State.SideBarEditActivity, State.EditSection);

                    State.SectionActions = sidBarActions.Model;
                }
            }
        }

        public virtual async Task DeleteActivity(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, string activityLookup)
        {
            await appDev.DeleteActivity(activityLookup, entLookup);

            await LoadActivities(appMgr, entLookup);
        }


        public virtual async Task DeleteLCU(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, string lcuLookup)
        {
            await appDev.DeleteLCU(lcuLookup, entLookup);

            //  TODO:  Need to delete other assets related to the LCU...  created apps, delete from filesystem, cleanup state??  Or what do we want to do with that stuff?

            await LoadLCUs(appMgr, entLookup);
        }

        public virtual async Task DeleteSectionAction(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, string action, string group)
        {
            await appDev.DeleteSectionAction(entLookup, State.EditSection, State.SideBarEditActivity, action, group);

            await LoadSecionActions(appMgr, entLookup);
        }

        public virtual async Task DeleteSideBarSection(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, string section)
        {
            await appDev.DeleteSideBarSection(section, entLookup, State.SideBarEditActivity);

            //  TODO:  Also need to delete all related side bar actions for sections

            await LoadSideBarSections(appMgr, entLookup);
        }

        public virtual async Task Ensure(ApplicationDeveloperClient appDev, string entLookup)
        {
            await appDev.EnsureIDESettings(entLookup);

            if (State.AddNew == null)
                State.AddNew = new IdeSettingsAddNew();

            if (State.Arch == null)
                State.Arch = new IdeSettingsArchitechtureState() { LCUs = new List<LCUConfig>() };

            if (State.Config == null)
                State.Config = new IdeSettingsConfigState()
                {
                    LCUConfig = new LowCodeUnitConfiguration()
                };
        }

        public virtual async Task LoadActivities(ApplicationManagerClient appMgr, string entLookup)
        {
            var acts = await appMgr.LoadIDEActivities(entLookup);

            State.Activities = acts.Model;
        }

        public virtual async Task LoadLCUs(ApplicationManagerClient appMgr, string entLookup)
        {
            var lcus = await appMgr.ListLCUs(entLookup);

            State.Arch.LCUs = lcus.Model;

            State.LCUSolutionOptions = State.Arch.LCUs?.ToDictionary(lcu => lcu.Lookup, lcu =>
            {
                var solutions = appMgr.ListLCUSolutions(entLookup, lcu.Lookup).Result;

                return solutions?.Model?.Select(sln => sln.Name)?.ToList() ?? new List<string>();
            });
        }

        public virtual async Task LoadLCUConfig(ApplicationManagerClient appMgr, string entLookup, string lcuLookup)
        {
            var lcuConfig = await appMgr.LoadLCUConfig(entLookup, lcuLookup);

            State.Config.LCUConfig = lcuConfig.Model;

            State.Config.CurrentLCUConfig = lcuLookup;

            State.Config.ActiveFiles = State.Config.LCUConfig.Files;

            var packSetup = await appMgr.GetModulePackSetup(entLookup, lcuLookup);

            State.Config.ActiveModules = packSetup.Model;

            var lcuSolutions = await appMgr.ListLCUSolutions(entLookup, lcuLookup);

            State.Config.ActiveSolutions = lcuSolutions.Model;
        }

        public virtual async Task LoadSecionActions(ApplicationManagerClient appMgr, string entLookup)
        {
            if (!State.SideBarEditActivity.IsNullOrEmpty() && !State.EditSection.IsNullOrEmpty())
            {
                var sidBarActions = await appMgr.LoadSectionActions(entLookup, State.SideBarEditActivity, State.EditSection);

                State.SectionActions = sidBarActions.Model;
            }
            else
                State.SectionActions = new List<SectionAction>();
        }

        public virtual async Task LoadSideBarSections(ApplicationManagerClient appMgr, string entLookup)
        {
            var sections = await appMgr.LoadIDESideBarSections(entLookup, State.SideBarEditActivity);

            State.SideBarSections = sections.Model;
        }

        public virtual async Task SaveActivity(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, Activity activity)
        {
            if (!activity.Title.IsNullOrEmpty() && !activity.Lookup.IsNullOrEmpty() && !activity.Icon.IsNullOrEmpty())
            {
                var actResp = await appDev.SaveActivity(activity, entLookup);

                activity = actResp.Model;

                await LoadActivities(appMgr, entLookup);

                await ToggleAddNew(AddNewTypes.None);

                State.EditActivity = activity.Lookup;
            }
        }

        public virtual async Task SaveLCU(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, string host, LCUConfig lcu)
        {
            if (!lcu.Lookup.IsNullOrEmpty() && lcu.Package != null)
            {
                var ensured = await appDev.EnsureLowCodeUnitView(lcu, entLookup, host);

                await LoadLCUs(appMgr, entLookup);

                await ToggleAddNew(AddNewTypes.None);
            }
        }

        public virtual async Task SaveLCUCapabilities(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, string host, string lcuLookup,
            LowCodeUnitConfiguration lcuConfig)
        {
            if (!lcuLookup.IsNullOrEmpty())
            {
                var status = await appDev.SaveLCUCapabilities(lcuConfig, entLookup, lcuLookup);

                await LoadLCUs(appMgr, entLookup);

                await LoadLCUConfig(appMgr, entLookup, lcuLookup);
            }
        }

        public virtual async Task SaveSectionAction(ApplicationDeveloperClient appDev, ApplicationManagerClient appMgr, string entLookup, SectionAction action)
        {
            if (!action.Action.IsNullOrEmpty() && !action.Title.IsNullOrEmpty())
            {
                action.Section = State.EditSection;

                var secAct = await appDev.SaveSectionAction(action, entLookup, State.SideBarEditActivity);

                await LoadSecionActions(appMgr, entLookup);

                await ToggleAddNew(AddNewTypes.None);
            }
        }

        public virtual async Task SetConfigLCU(ApplicationManagerClient appMgr, string entLookup, string host, string lcuLookup)
        {
            // log.LogInformation("Starting to set config LCU");

            await ClearConfig();

            State.Config.CurrentLCUConfig = lcuLookup;

            if (!State.Config.CurrentLCUConfig.IsNullOrEmpty())
                await LoadLCUConfig(appMgr, entLookup, State.Config.CurrentLCUConfig);
        }

        public virtual async Task SetEditActivity(string activityLookup)
        {
            await ToggleAddNew(AddNewTypes.None);

            State.EditActivity = State.Activities?.FirstOrDefault(a => a.Lookup == activityLookup)?.Lookup;
        }

        public virtual async Task SetEditLCU(string lcuLookup)
        {
            await ToggleAddNew(AddNewTypes.None);

            State.Arch.EditLCU = State.Arch.LCUs?.FirstOrDefault(a => a.Lookup == lcuLookup)?.Lookup;
        }

        public virtual async Task SetEditSection(ApplicationManagerClient appMgr, string entLookup, string section)
        {
            await ToggleAddNew(AddNewTypes.None);

            State.EditSection = State.SideBarSections?.FirstOrDefault(sec => sec == section);

            await LoadSecionActions(appMgr, entLookup);
        }

        public virtual async Task SetEditSectionAction(ApplicationManagerClient appMgr, string entLookup, string action)
        {
            State.EditSectionAction = State.SectionActions?.FirstOrDefault(sa => sa.Action == action)?.Action;

            await LoadSecionActions(appMgr, entLookup);
        }

        public virtual async Task SetSideBarEditActivity(ApplicationManagerClient appMgr, string entLookup, string host, string activityLookup)
        {
            State.SideBarEditActivity = activityLookup;

            await ConfigureSideBarEditActivity(appMgr, entLookup, host);
        }

        public virtual async Task ToggleAddNew(AddNewTypes type)
        {
            State.EditActivity = null;

            State.Arch.EditLCU = null;

            State.EditSectionAction = null;

            switch (type)
            {
                case AddNewTypes.Activity:
                    State.AddNew.Activity = !State.AddNew.Activity;

                    State.AddNew.LCU = false;

                    State.AddNew.SectionAction = false;

                    State.EditSection = null;
                    break;

                case AddNewTypes.LCU:
                    State.AddNew.Activity = false;

                    State.AddNew.LCU = !State.AddNew.LCU;

                    State.AddNew.SectionAction = false;

                    State.EditSection = null;
                    break;

                case AddNewTypes.SectionAction:
                    State.AddNew.Activity = false;

                    State.AddNew.LCU = false;

                    State.AddNew.SectionAction = !State.AddNew.SectionAction;
                    break;

                case AddNewTypes.None:
                    State.AddNew.Activity = false;

                    State.AddNew.LCU = false;

                    State.AddNew.SectionAction = false;

                    State.EditSection = null;
                    break;
            }
        }
        #endregion

        #region Helpers
        #endregion
    }

    public static class Temp
    {

        public static async Task<BaseResponse> SaveLCUCapabilities(this ApplicationDeveloperClient appDev, LowCodeUnitConfiguration lcuConfig, string entLookup,
            string lcuLookup)
        {
            var response = await appDev.Post<LowCodeUnitConfiguration, BaseResponse>($"hosting/{entLookup}/lcus/{lcuLookup}",
                lcuConfig);

            return response;
        }

    }
}