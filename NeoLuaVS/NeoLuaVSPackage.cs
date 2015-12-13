using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudioTools;
using Neo.IronLua.Debugger;
using Neo.IronLua.Debugger.Remote;

namespace Neo.IronLua
{
	[
		PackageRegistration(UseManagedResourcesOnly = true),
		InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400),

		ProvideService(typeof(NeoLuaLanguageService), ServiceName = "NeoLua Language Service"),
		ProvideLanguageService(typeof(NeoLuaLanguageService), "NeoLua", 200,
			ShowSmartIndent = true,
			EnableLineNumbers = true,
			RequestStockColors = false,

			CodeSense = true,
			CodeSenseDelay = 1000,

			AutoOutlining = true,
			EnableCommenting = true,

			MatchBraces = true,
			MatchBracesAtCaret = true,
			ShowMatchingBrace = true
		),
		ProvideLanguageExtension(typeof(NeoLuaLanguageService), ".lua"),
		ProvideLanguageExtension(typeof(NeoLuaLanguageService), ".nlua"),
	
		//ProvideDebugEngine("NeoLua Debugging", typeof(AD7ProgramProvider), typeof(AD7Engine), AD7Engine.DebugEngineId),
    //ProvideDebugPortSupplier("NeoLua Remote (des)", typeof(RemoteDebugPortSupplier), RemoteDebugPortSupplier.PortSupplierId), // , typeof(RemoteDebugPortPicker)
																																																															//ProvideDebugPortPicker(typeof(RemoteDebugPortPicker)),

		Guid(GuidList.guidNeoLuaVSPkgString)
	]
	public sealed class NeoLuaVSPackage : Package, IOleComponent
	{
		private NeoLuaLanguageService languageService = null;
		private uint languageTimerComponent = 0;

		public NeoLuaVSPackage()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
		}

		protected override void Initialize()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
			base.Initialize();

			IServiceContainer sc = (IServiceContainer)this;

			// Register Language
			languageService = new NeoLuaLanguageService();
			languageService.SetSite(this);
			sc.AddService(typeof(NeoLuaLanguageService), languageService, true);

			// Register timer for the language
			IOleComponentManager mgr = this.GetService(typeof(SOleComponentManager)) as IOleComponentManager;
			if (mgr != null && languageTimerComponent == 0)
			{
				OLECRINFO[] crinfo = new OLECRINFO[1];
				crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));
				crinfo[0].grfcrf = (uint)(_OLECRF.olecrfNeedIdleTime | _OLECRF.olecrfNeedPeriodicIdleTime);
				crinfo[0].grfcadvf = (uint)(_OLECADVF.olecadvfModal | _OLECADVF.olecadvfRedrawOff | _OLECADVF.olecadvfWarningsOff);
				crinfo[0].uIdleTimeInterval = 1000;
				Marshal.ThrowExceptionForHR(mgr.FRegisterComponent(this, crinfo, out languageTimerComponent));
			}
		} // proc Initialize

		protected override void Dispose(bool disposing)
		{
			if (languageTimerComponent != 0)
			{
				IOleComponentManager mgr = this.GetService(typeof(SOleComponentManager)) as IOleComponentManager;
				if (mgr != null)
					mgr.FRevokeComponent(languageTimerComponent);
				languageTimerComponent = 0;
			}
			base.Dispose(disposing);
		} // proc Dispose

		#region -- IOleComponent ----------------------------------------------------------

		public int FDoIdle(uint grfidlef)
		{
			if (languageService != null)
				languageService.OnIdle((grfidlef & (uint)_OLEIDLEF.oleidlefPeriodic) != 0);
			return VSConstants.S_OK;
		} // func FDoIdle

		public int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked) { return VSConstants.S_FALSE; }
		public int FPreTranslateMessage(MSG[] pMsg) { return VSConstants.S_OK; }
		public int FQueryTerminate(int fPromptUser) { return VSConstants.S_FALSE; }
		public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam) { return VSConstants.S_FALSE; }
		public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved) { return IntPtr.Zero; }
		public void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved) { }
		public void OnAppActivate(int fActive, uint dwOtherThreadID) { }
		public void OnEnterState(uint uStateID, int fEnter) { }
		public void OnLoseActivation() { }
		public void Terminate() { }

		#endregion
	} // class NeoLuaVSPackage
}
