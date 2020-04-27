using System;
using System.IO;
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
using LCU.Graphs.Registry.Enterprises.IDE;
using LCU.Graphs.Registry.Enterprises.DataFlows;

namespace LCU.State.API.NapkinIDE.NapkinIDE.IdeManagement.State
{
    [Serializable]
    [DataContract]
    public class IDESettingsState
    {
		[DataMember]
		public virtual List<IDEActivity> Activities { get; set; }

		[DataMember]
		public virtual IdeSettingsAddNew AddNew { get; set; }

		[DataMember]
		public virtual IdeSettingsArchitechtureState Arch { get; set; }

		[DataMember]
		public virtual IdeSettingsConfigState Config { get; set; }

		[DataMember]
		public virtual string EditActivity { get; set; }

		[DataMember]
		public virtual string EditSection { get; set; }

		[DataMember]
		public virtual string EditSectionAction { get; set; }

		[DataMember]
		public virtual Dictionary<string, List<string>> LCUSolutionOptions { get; set; }

		[DataMember]
		public virtual bool Loading { get; set; }

		[DataMember]
		public virtual List<IDESideBarAction> SectionActions { get; set; }

		[DataMember]
		public virtual string SideBarEditActivity { get; set; }

		[DataMember]
		public virtual List<string> SideBarSections { get; set; }
    }
    
	[DataContract]
    public enum AddNewTypes
    {
		[EnumMember]
        None,

		[EnumMember]
        Activity,
        
		[EnumMember]
		LCU,
        
		[EnumMember]
		SectionAction
    }

    [Serializable]
	[DataContract]
	public class IdeSettingsAddNew
	{
		[DataMember]
		public virtual bool Activity { get; set; }

		[DataMember]
		public virtual bool LCU { get; set; }

		[DataMember]
		public virtual bool SectionAction { get; set; }
	}
    
    [Serializable]
	[DataContract]
	public class IdeSettingsArchitechtureState
	{
		[DataMember]
		public virtual string EditLCU { get; set; }

		[DataMember]
		public virtual List<LowCodeUnitSetupConfig> LCUs { get; set; }
	}

    [Serializable]
	[DataContract]
	public class IdeSettingsConfigState
	{
		[DataMember]
		public virtual List<string> ActiveFiles { get; set; }

		[DataMember]
		public virtual ModulePackSetup ActiveModules { get; set; }

		[DataMember]
		public virtual List<IdeSettingsConfigSolution> ActiveSolutions { get; set; }
		
		[DataMember]
		public virtual string CurrentLCUConfig { get; set; }

		[DataMember]
		public virtual LowCodeUnitConfiguration LCUConfig { get; set; }
	}
}
