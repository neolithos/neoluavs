using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace Neo.IronLua.Debugger
{
	[
		ComVisible(true),
		Guid("64c64475-d20b-437d-967f-5a02853f5e9c")
	]
	public class AD7ProgramProvider : IDebugProgramProvider2
	{
		int IDebugProgramProvider2.GetProviderProcessData(enum_PROVIDER_FLAGS Flags, IDebugDefaultPort2 pPort, AD_PROCESS_ID ProcessId, CONST_GUID_ARRAY EngineFilter, PROVIDER_PROCESS_DATA[] pProcess)
		{
			return VSConstants.S_FALSE;
		}

		int IDebugProgramProvider2.GetProviderProgramNode(enum_PROVIDER_FLAGS Flags, IDebugDefaultPort2 pPort, AD_PROCESS_ID ProcessId, ref Guid guidEngine, ulong programId, out IDebugProgramNode2 ppProgramNode)
		{
			ppProgramNode = null;
			return VSConstants.E_NOTIMPL;
		}

		int IDebugProgramProvider2.SetLocale(ushort wLangID)
		{
			return VSConstants.S_OK;
		}

		int IDebugProgramProvider2.WatchForProviderEvents(enum_PROVIDER_FLAGS Flags, IDebugDefaultPort2 pPort, AD_PROCESS_ID ProcessId, CONST_GUID_ARRAY EngineFilter, ref Guid guidLaunchingEngine, IDebugPortNotify2 pEventCallback)
		{
			return VSConstants.S_OK;
		}
	}
}
