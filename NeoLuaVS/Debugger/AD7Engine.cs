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
	Guid("1686cfe2-c177-43f8-be2f-a137f1332337")
	]
	public sealed class AD7Engine : IDebugEngine2
	{
		public AD7Engine()
		{
		} // ctor

		int IDebugEngine2.Attach(IDebugProgram2[] rgpPrograms, IDebugProgramNode2[] rgpProgramNodes, uint celtPrograms, IDebugEventCallback2 pCallback, enum_ATTACH_REASON dwReason)
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.CauseBreak()
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.ContinueFromSynchronousEvent(IDebugEvent2 pEvent)
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.CreatePendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, out IDebugPendingBreakpoint2 ppPendingBP)
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.DestroyProgram(IDebugProgram2 pProgram)
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.EnumPrograms(out IEnumDebugPrograms2 ppEnum)
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.GetEngineId(out Guid pguidEngine)
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.RemoveAllSetExceptions(ref Guid guidType)
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.RemoveSetException(EXCEPTION_INFO[] pException)
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.SetException(EXCEPTION_INFO[] pException)
		{
			throw new NotImplementedException();
		}

		int IDebugEngine2.SetLocale(ushort wLangID)
		{
			return VSConstants.S_OK;
		}

		int IDebugEngine2.SetMetric(string pszMetric, object varValue)
		{
			return VSConstants.S_OK;
		}

		int IDebugEngine2.SetRegistryRoot(string pszRegistryRoot)
		{
			return VSConstants.S_OK;
		}

		// -- static --------------------------------------------------------------

		public const string DebugEngineId = "{1CBDE4E3-BF22-452A-9396-596EF06E0762}";
		public static Guid DebugEngineGuid { get; } = new Guid(DebugEngineId);
	} // class AD7Engine
}
