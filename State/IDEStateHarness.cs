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

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State
{
    public class IDEStateHarness : LCUStateHarness<IDEState>
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public IDEStateHarness(IDEState state)
            : base(state ?? new IDEState())
        { }
        #endregion

        #region API Methods
        public virtual async Task Ensure(ApplicationManagerClient appMgr, IdentityManagerClient idMgr, string entApiKey, string username)
        {

            // check in to see if user has free trial/paid subscriber rights    
            var authResp = await idMgr.HasAccess(entApiKey, username, new List<string>() { "LCU.NapkinIDE.AllAccess" });

            State.IsActiveSubscriber = authResp.Status;

            var activitiesResp = await appMgr.LoadIDEActivities(entApiKey);

            if (State.IsActiveSubscriber)
            {
                var appsResp = await appMgr.ListApplications(entApiKey);

                State.InfrastructureConfigured = activitiesResp.Status && !activitiesResp.Model.IsNullOrEmpty() && appsResp.Status && !appsResp.Model.IsNullOrEmpty();
            }
            else
            {
                State.InfrastructureConfigured = true;
            }

            if (activitiesResp.Status)
                await SetupActivities(activitiesResp.Model, entApiKey);

            await LoadHeaderActions();

            await LoadSideBar(appMgr, entApiKey);

            if (State.IsActiveSubscriber)
            {
                var appsResp = await appMgr.ListApplications(entApiKey);

                State.InfrastructureConfigured = activitiesResp.Status && !activitiesResp.Model.IsNullOrEmpty() && appsResp.Status && !appsResp.Model.IsNullOrEmpty();
            }
            else
            {

            }
        }

        public virtual async Task SetupActivities(ApplicationManagerClient appMgr, string entApiKey)
        {
            var activitiesResp = await appMgr.LoadIDEActivities(entApiKey);

            if (activitiesResp.Status)
                await SetupActivities(activitiesResp.Model, entApiKey);
        }

        public virtual async Task SetupActivities(List<IDEActivity> activities, string entApiKey)
        {
            if (State.IsActiveSubscriber)
            {
                State.Activities = activities ?? new List<IDEActivity>();

                State.RootActivities = new List<IDEActivity>();

                State.RootActivities.Add(new IDEActivity()
                {
                    Icon = "settings",
                    Lookup = Environment.GetEnvironmentVariable("FORGE-SETTINGS-PATH") ?? "/forge-settings",
                    Title = "Settings"
                });
            }
            else
            {
                State.RootActivities = new List<IDEActivity>();

                State.Activities = activities?.Where(act => act.Lookup == "limited-trial" || act.Lookup == "fathym-forecast").ToList() 
                    ?? new List<IDEActivity>();
            }

            State.CurrentActivity = State.Activities.FirstOrDefault();
        }

        public virtual async Task LoadHeaderActions()
        {
            State.HeaderActions = new List<IDEAction>();

            if (State.IsActiveSubscriber)
            {
            }
            else
            {
                State.HeaderActions.Add(new IDEAction()
                {
                    Text = "Buy Now",
                    Type = IDEActionTypes.ExternalLink,
                    Icon = "shopping_cart",
                    Action = "/billing"
                });
            }

            State.HeaderActions.Add(new IDEAction()
            {
                Text = "Download",
                Type = IDEActionTypes.ExternalLink,
                Icon = "save_alt",
                Action = "https://github.com/lowcodeunit",
            });

            State.HeaderActions.Add(new IDEAction()
            {
                Text = "",
                Type = IDEActionTypes.ExternalLink,
                Icon = "assignment",
                Action = "https://support.fathym.com"
            });

            State.HeaderActions.Add(new IDEAction()
            {
                Text = "",
                Type = IDEActionTypes.Link,
                Icon = "help_outline",
                Action = "mailto:support@fathym.com?subject=Fathym IDE Support - ____&body=Please provide us as much detail as you can so that we may better support you."
            });
        }

        public virtual async Task LoadSideBar(ApplicationManagerClient appMgr, string entApiKey)
        {
            if (State.SideBar == null)
                State.SideBar = new IDESideBar();

            if (State.CurrentActivity != null)
            {
                var sectionsResp = await appMgr.LoadIDESideBarSections(entApiKey, State.CurrentActivity.Lookup);

                State.SideBar.Actions = sectionsResp.Model.SelectMany(section =>
                {
                    var actionsResp = appMgr.LoadIDESideBarActions(entApiKey, State.CurrentActivity.Lookup, section).Result;

                    return actionsResp.Model;
                }).ToList();
            }
            else
                State.SideBar = new IDESideBar();

            if (!State.HasLoaded)
            {
                var firstAction = State.SideBar?.Actions?.FirstOrDefault();

                if (firstAction != null)
                    await SelectSideBarAction(appMgr, entApiKey, firstAction.Group, firstAction.Action, firstAction.Section);

                State.HasLoaded = true;
            }
        }

        public virtual async Task RemoveEditor(string editorLookup)
        {
            State.Editors = State.Editors.Where(e => e.Lookup != editorLookup).ToList();

            State.CurrentEditor = State.Editors.FirstOrDefault();

            State.SideBar.CurrentAction = State.SideBar.Actions.FirstOrDefault(a => $"{a.Group}|{a.Action}" == State.CurrentEditor?.Lookup);
        }

        public virtual async Task SelectEditor(string editorLookup)
        {
            State.SideBar.CurrentAction = State.SideBar.Actions.FirstOrDefault(a => $"{a.Group}|{a.Action}" == editorLookup);

            State.CurrentEditor = State.Editors.FirstOrDefault(a => a.Lookup == editorLookup);
        }

        public virtual async Task SelectSideBarAction(ApplicationManagerClient appMgr, string entApiKey, string group, string action, string section)
        {
            State.SideBar.CurrentAction = State.SideBar.Actions.FirstOrDefault(a => a.Group == group && a.Action == action);

            if (State.Editors.IsNullOrEmpty())
                State.Editors = new List<IDEEditor>();

            var actionLookup = $"{group}|{action}";

            if (!State.Editors.Select(e => e.Lookup).Contains(actionLookup))
            {
                var ideEditorResp = await appMgr.LoadIDEEditor(entApiKey, group, action, section, State.CurrentActivity.Lookup);

                if (ideEditorResp.Status)
                    State.Editors.Add(ideEditorResp.Model);
            }

            State.CurrentEditor = State.Editors.FirstOrDefault(a => a.Lookup == actionLookup);
        }

        public virtual async Task SetActivity(ApplicationManagerClient appMgr, string entApiKey, string activityLookup)
        {
            State.CurrentActivity = State.Activities.FirstOrDefault(a => a.Lookup == activityLookup);

            await LoadSideBar(appMgr, entApiKey);

            State.SideBar.CurrentAction = State.SideBar.Actions.FirstOrDefault(a => $"{a.Group}|{a.Action}" == State.CurrentEditor?.Lookup);
        }

        public virtual void SetUsername(string username)
        {
            State.Username = username;
        }

        public virtual async Task ToggleShowPanels(string group, string action)
        {
            State.ShowPanels = !State.ShowPanels;
        }


        #endregion

        #region Helpers
        protected virtual async Task configureTrialActivities()
        {

        }
        #endregion
    }
}