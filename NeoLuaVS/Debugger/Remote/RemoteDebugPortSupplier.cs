using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.OLE.Interop;

namespace Neo.IronLua.Debugger.Remote
{
	[
		ComVisible(true),
		Guid("5c26d50c-6b21-4ebb-8df9-7e915043570e")
	]
	public class RemoteDebugPortSupplier : IDebugPortSupplier2, IDebugPortSupplierDescription2
	{
		public const string PortSupplierId = "{499BA218-143A-4389-9678-CFA51D0E9316}";
		public static readonly Guid PortSupplierGuid = new Guid(PortSupplierId);

		int IDebugPortSupplier2.AddPort(IDebugPortRequest2 pRequest, out IDebugPort2 ppPort)
		{
			string portName;
			if (ErrorHandler.Succeeded(pRequest.GetPortName(out portName)))
      {
				ppPort = new RemoteDebugPort(this, pRequest, portName);
				return VSConstants.S_OK;
			}
			ppPort = null;
			return VSConstants.E_NOTIMPL;
		}

		int IDebugPortSupplier2.CanAddPort()
		{
			throw new NotImplementedException();
		}

		int IDebugPortSupplier2.EnumPorts(out IEnumDebugPorts2 ppEnum)
		{
			ppEnum = null;
			return VSConstants.E_NOTIMPL;
		}

		int IDebugPortSupplierDescription2.GetDescription(enum_PORT_SUPPLIER_DESCRIPTION_FLAGS[] pdwFlags, out string pbstrText)
		{
			pbstrText = "text....";
			return VSConstants.S_OK;
		}

		int IDebugPortSupplier2.GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
		{
			throw new NotImplementedException();
		}

		int IDebugPortSupplier2.GetPortSupplierId(out Guid pguidPortSupplier)
		{
			pguidPortSupplier = PortSupplierGuid;
			return VSConstants.S_OK;
		}

		int IDebugPortSupplier2.GetPortSupplierName(out string pbstrName)
		{
			pbstrName = "Data Exchange Server (NeoLua)";
			return VSConstants.S_OK;
		}

		int IDebugPortSupplier2.RemovePort(IDebugPort2 pPort)
		{
			throw new NotImplementedException();
		}
	} // class RemoteDebugPortSupplier

	internal sealed class RemoteDebugPrograms : IDebugProgram2
	{
		private readonly Guid guidId = Guid.NewGuid();
		private readonly IDebugProcess2 process;

		public RemoteDebugPrograms(IDebugProcess2 process)
		{
			this.process = process;
		}

		public int Attach(IDebugEventCallback2 pCallback)
		{
			throw new NotImplementedException();
		}

		public int CanDetach()
		{
			throw new NotImplementedException();
		}

		public int CauseBreak()
		{
			throw new NotImplementedException();
		}

		public int Continue(IDebugThread2 pThread)
		{
			throw new NotImplementedException();
		}

		public int Detach()
		{
			throw new NotImplementedException();
		}

		public int EnumCodeContexts(IDebugDocumentPosition2 pDocPos, out IEnumDebugCodeContexts2 ppEnum)
		{
			throw new NotImplementedException();
		}

		public int EnumCodePaths(string pszHint, IDebugCodeContext2 pStart, IDebugStackFrame2 pFrame, int fSource, out IEnumCodePaths2 ppEnum, out IDebugCodeContext2 ppSafety)
		{
			throw new NotImplementedException();
		}

		public int EnumModules(out IEnumDebugModules2 ppEnum)
		{
			throw new NotImplementedException();
		}

		public int EnumThreads(out IEnumDebugThreads2 ppEnum)
		{
			throw new NotImplementedException();
		}

		public int Execute()
		{
			throw new NotImplementedException();
		}

		public int GetDebugProperty(out IDebugProperty2 ppProperty)
		{
			throw new NotImplementedException();
		}

		public int GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 pCodeContext, out IDebugDisassemblyStream2 ppDisassemblyStream)
		{
			throw new NotImplementedException();
		}

		public int GetENCUpdate(out object ppUpdate)
		{
			throw new NotImplementedException();
		}

		public int GetEngineInfo(out string pbstrEngine, out Guid pguidEngine)
		{
			pbstrEngine = "Lua";
			pguidEngine = AD7Engine.DebugEngineGuid;
			return VSConstants.S_OK;
		}

		public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
		{
			throw new NotImplementedException();
		}

		public int GetName(out string pbstrName)
		{
			throw new NotImplementedException();
		}

		public int GetProcess(out IDebugProcess2 ppProcess)
		{
			throw new NotImplementedException();
		}

		public int GetProgramId(out Guid pguidProgramId)
		{
			pguidProgramId = guidId;
			return VSConstants.S_OK;
		}

		public int Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
		{
			throw new NotImplementedException();
		}

		public int Terminate()
		{
			throw new NotImplementedException();
		}

		public int WriteDump(enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
		{
			throw new NotImplementedException();
		}
	}

	internal sealed class RemoteEnumDebugPrograms : IEnumDebugPrograms2
	{
		private readonly IDebugProcess2 process;

		public RemoteEnumDebugPrograms(IDebugProcess2 process)
		{
			this.process = process;
		}


		int IEnumDebugPrograms2.Clone(out IEnumDebugPrograms2 ppEnum)
		{
			ppEnum = new RemoteEnumDebugPrograms(process);
			return VSConstants.S_OK;
		}

		int IEnumDebugPrograms2.GetCount(out uint pcelt)
		{
			pcelt = 1;
			return VSConstants.S_OK;
		}

		int IEnumDebugPrograms2.Next(uint celt, IDebugProgram2[] rgelt, ref uint pceltFetched)
		{
			rgelt[0] = new RemoteDebugPrograms(process);
			pceltFetched = 1;
			return VSConstants.S_OK;
		}

		int IEnumDebugPrograms2.Reset()
		{
			return VSConstants.S_OK;
		}

		int IEnumDebugPrograms2.Skip(uint celt)
		{
			return VSConstants.S_OK;
		}
	}

	internal sealed class RemoteDebugProcess : IDebugProcess2
	{
		private readonly Guid guidId;
		private readonly RemoteDebugPort port;
		
		public RemoteDebugProcess(RemoteDebugPort port)
		{
			this.guidId = Guid.NewGuid();
			this.port = port;
		} // ctor


		int IDebugProcess2.Attach(IDebugEventCallback2 pCallback, Guid[] rgguidSpecificEngines, uint celtSpecificEngines, int[] rghrEngineAttach)
		{
			throw new NotImplementedException();
		}

		int IDebugProcess2.CanDetach()
		{
			throw new NotImplementedException();
		}

		int IDebugProcess2.CauseBreak()
		{
			throw new NotImplementedException();
		}

		int IDebugProcess2.Detach()
		{
			throw new NotImplementedException();
		}

		int IDebugProcess2.EnumPrograms(out IEnumDebugPrograms2 ppEnum)
		{
			ppEnum = new RemoteEnumDebugPrograms(this);
			return VSConstants.S_OK;
		}

		int IDebugProcess2.EnumThreads(out IEnumDebugThreads2 ppEnum)
		{
			throw new NotImplementedException();
		}

		int IDebugProcess2.GetAttachedSessionName(out string pbstrSessionName)
		{
			throw new NotImplementedException();
		}

		int IDebugProcess2.GetInfo(enum_PROCESS_INFO_FIELDS Fields, PROCESS_INFO[] pProcessInfo)
		{
			pProcessInfo[0] = new PROCESS_INFO();
			pProcessInfo[0].Fields = Fields;
			pProcessInfo[0].bstrFileName = "test.lua";
			pProcessInfo[0].bstrBaseName = "base";
			pProcessInfo[0].bstrTitle = "title";
			pProcessInfo[0].dwSessionId = 1;
			pProcessInfo[0].Flags = enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_RUNNING;

			return VSConstants.S_OK;
		}

		int IDebugProcess2.GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
		{
			pbstrName = "test.lua";
			return VSConstants.S_OK;
		}

		int IDebugProcess2.GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
		{
			pProcessId[0].dwProcessId = 10;
      return VSConstants.S_OK;
		}

		int IDebugProcess2.GetPort(out IDebugPort2 ppPort)
		{
			ppPort = port;
			return VSConstants.S_OK;
		}

		int IDebugProcess2.GetProcessId(out Guid pguidProcessId)
		{
			pguidProcessId = guidId;
			return VSConstants.S_OK;
		}

		int IDebugProcess2.GetServer(out IDebugCoreServer2 ppServer)
		{
			throw new NotImplementedException();
		}

		int IDebugProcess2.Terminate()
		{
			throw new NotImplementedException();
		}
	}

	internal sealed class RemoteEnumDebugProcesses : IEnumDebugProcesses2
	{
		private readonly RemoteDebugPort port;

		public RemoteEnumDebugProcesses(RemoteDebugPort port)
		{
			this.port = port;
		} // ctor

		int IEnumDebugProcesses2.Clone(out IEnumDebugProcesses2 ppEnum)
		{
			ppEnum = new RemoteEnumDebugProcesses(port);
			return VSConstants.S_OK;
		}

		int IEnumDebugProcesses2.GetCount(out uint pcelt)
		{
			pcelt = 1;
			return VSConstants.S_OK;
		}

		int IEnumDebugProcesses2.Next(uint celt, IDebugProcess2[] rgelt, ref uint pceltFetched)
		{
			rgelt[0] = new RemoteDebugProcess(port);
				pceltFetched = 1;
			return VSConstants.S_OK;
		}

		int IEnumDebugProcesses2.Reset()
		{
			return VSConstants.S_OK;
		}

		int IEnumDebugProcesses2.Skip(uint celt)
		{
			return VSConstants.S_OK;
		}
	} // class RemoteEnumDebugProcesses

	internal sealed class RemoteDebugPort : IDebugPort2
	{
		private readonly Guid guidId;
		private readonly RemoteDebugPortSupplier supplier;
		private readonly IDebugPortRequest2 request;
		private readonly string portName;

		public RemoteDebugPort(RemoteDebugPortSupplier supplier, IDebugPortRequest2 request, string portName)
		{
			this.guidId = Guid.NewGuid();
			this.supplier = supplier;
			this.request = request;
			this.portName = portName;
		} // ctor

		int IDebugPort2.EnumProcesses(out IEnumDebugProcesses2 ppEnum)
		{
			ppEnum = new RemoteEnumDebugProcesses(this);
			return VSConstants.S_OK;
		}

		int IDebugPort2.GetPortId(out Guid pguidPort)
		{
			pguidPort = guidId;
			return VSConstants.S_OK;
		}

		int IDebugPort2.GetPortName(out string pbstrName)
		{
			pbstrName = "portname";
			return VSConstants.S_OK;
		}

		int IDebugPort2.GetPortRequest(out IDebugPortRequest2 ppRequest)
		{
			ppRequest = request;
			return VSConstants.S_OK;
		}

		int IDebugPort2.GetPortSupplier(out IDebugPortSupplier2 ppSupplier)
		{
			ppSupplier = supplier;
			return VSConstants.S_OK;
		}

		int IDebugPort2.GetProcess(AD_PROCESS_ID ProcessId, out IDebugProcess2 ppProcess)
		{
			throw new NotImplementedException();
		}
	}

	[
		ComVisible(true),
		Guid("cd4ae8bb-1fe5-456b-b92b-6ad8556b035c")
	]
	public class RemoteDebugPortPicker : IDebugPortPicker
	{
		int IDebugPortPicker.DisplayPortPicker(IntPtr hwndParentDialog, out string pbstrPortId)
		{
			throw new NotImplementedException();
		}

		int IDebugPortPicker.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider pSP)
		{
			return VSConstants.S_OK;
		}
	}
}
