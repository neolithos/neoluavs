using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Neo.IronLua
{
	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public class NeoLuaAuthoringScope : AuthoringScope
	{
		private NeoLuaSource source;

		private NeoLuaToken currentToken = null;
		private Declarations declarations = null;

		public class DumpDesc : IVsObjectBrowserDescription3
		{
			public int AddDescriptionText3(string pText, VSOBDESCRIPTIONSECTION obdSect, IVsNavInfo pHyperJump)
			{
				Debug.Print("{0}: {1}", obdSect, pText);
				return VSConstants.S_OK;
			}

			public int ClearDescriptionText()
			{
				return VSConstants.S_OK;
			}
		}

		public NeoLuaAuthoringScope(NeoLuaSource source)
		{
			this.source = source;
		} // ctor
		
		public override string GetDataTipText(int line, int col, out TextSpan span)
		{
			span = new TextSpan();
			//if(source.ParseInfo == null)
			//	return null;

			//try
			//{
			//	NeoLuaToken tok = source.ParseInfo.FindToken(line, col);
			//	if (tok != null && tok.Parent is NeoLuaTypeScope && tok.Token == LuaToken.Identifier)
			//	{
			//		IVsLibrary2 _library;
			//		IVsSimpleLibrary2 simpleLibrary;
			//		IVsObjectManager2 mgr = source.LanguageService.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
					
			//		//Guid guid = new Guid("18e32c04-58ba-4a1e-80de-1c291634166a"); COM
			//		Guid guid = new Guid("1ec72fd7-c820-4273-9a21-777a5c522e03"); // {1EC72FD7-C820-4273-9A21-777A5C522E03} // COM+
			//		uint o = 0;
			//		ErrorHandler.ThrowOnFailure(mgr.FindLibrary(ref guid, out _library));
			//		simpleLibrary = _library as IVsSimpleLibrary2;
			//		IVsSimpleBrowseComponentSet compset;
			//		ErrorHandler.ThrowOnFailure(mgr.CreateSimpleBrowseComponentSet(0, null, 0, out compset));

			//		IVsNavInfo v;
			//		compset.AddComponent(ref guid, new[]{
			//			new VSCOMPONENTSELECTORDATA(){
			//				 bstrFile=@"C:\Projects\NeoLua\NeoLua\bin\Release\Neo.Lua.dll",
			//				 bstrTitle="Neo.Lua",
			//				 dwSize =(uint) Marshal.SizeOf(typeof(VSCOMPONENTSELECTORDATA)),
			//				 type= VSCOMPONENTTYPE.VSCOMPONENTTYPE_ComPlus
			//			}
			//		}, out v, null);
					
			//		IVsSimpleObjectList2 list2;
			//		IVsObjectList2 list1;
			//		ErrorHandler.ThrowOnFailure(simpleLibrary.GetList2((uint)_LIB_LISTTYPE.LLT_CLASSES, (uint)_LIB_LISTFLAGS.LLF_NONE,
			//			new[]
			//		{
			//			new VSOBSEARCHCRITERIA2(){
			//				pIVsNavInfo = v,
			//				eSrchType= VSOBSEARCHTYPE.SO_ENTIREWORD,
			//				szName = "LuaGlobal",
			//				grfOptions = 0
			//			}

			//		}, out list2));
			//		compset.GetList2((uint)_LIB_LISTTYPE.LLT_CLASSES, (uint)_LIB_LISTFLAGS.LLF_NONE, new[]
			//		{
			//			new VSOBSEARCHCRITERIA2(){
			//				pIVsNavInfo = null,
			//				eSrchType=  VSOBSEARCHTYPE.SO_ENTIREWORD,
			//				szName = "LuaGlobal",
			//				grfOptions = 0
			//			}

			//		}, null, out list1);
			//		list1.GetItemCount(out o);
			//		Debug.Print("Items: {0}", o);

			//		list2.GetItemCount(out o);
			//		Debug.Print("Items: {0}", o);
			//		for (uint k = 0; k < o; k++)
			//		{
			//			string sText;
			//			list2.GetTextWithOwnership(k, VSTREETEXTOPTIONS.TTO_DISPLAYTEXT, out sText);
			//			Debug.Print("- " + sText);
			//		}

			//		//simpleLibrary.LoadState

			//		//string sOutComponent;
			//		// Füge die Lib ein
			//		//ErrorHandler.ThrowOnFailure(simpleLibrary.AddBrowseContainer(new[]{
			//		//	new VSCOMPONENTSELECTORDATA(){
			//		//		 bstrFile=@"C:\Projects\NeoLua\NeoLua\bin\Release\Neo.Lua.dll",
			//		//		 bstrTitle="Neo.Lua",
			//		//		 dwSize =(uint) Marshal.SizeOf(typeof(VSCOMPONENTSELECTORDATA)),
			//		//		 type= VSCOMPONENTTYPE.VSCOMPONENTTYPE_ComPlus
			//		//	}
			//		//}, ref o, out sOutComponent));
			//		//ErrorHandler.ThrowOnFailure(lib.RemoveBrowseContainer(0, @"C:\Projects\NeoLua\NeoLua\bin\Release\Neo.Lua.dll"));

			//		//// Suche etwas
			//		//IVsSimpleObjectList2 list3;
			//		//ErrorHandler.ThrowOnFailure(simpleLibrary.GetList2((uint)_LIB_LISTTYPE.LLT_CLASSES, (uint)_LIB_LISTFLAGS.LLF_USESEARCHFILTER, // 
			//		//	 new[]
			//		//		{
			//		//			new VSOBSEARCHCRITERIA2
			//		//			{
			//		//				eSrchType = VSOBSEARCHTYPE.SO_ENTIREWORD,
			//		//				szName = "StringBuilder"
			//		//			}
			//		//		}, out list3));

			//		//if (list3 != null)
			//		//{
			//		//	uint dwCount;
			//		//	list3.GetItemCount(out dwCount);
			//		//	if (dwCount > 0)
			//		//	{
			//		//		string sText;
			//		//		list3.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_DISPLAYTEXT, out sText);
			//		//		IVsSimpleObjectList2 list4;
			//		//		ErrorHandler.ThrowOnFailure(list3.GetList2(0, (uint)_LIB_LISTTYPE.LLT_MEMBERS, (uint)_LIB_LISTFLAGS.LLF_NONE, null, out list4));

			//		//		list3.FillDescription2(0, (uint)(_VSOBJDESCOPTIONS.ODO_TOOLTIPDESC | _VSOBJDESCOPTIONS.ODO_USEFULLNAME), new DumpDesc());

			//		//		list4.GetItemCount(out dwCount);
			//		//		Debug.Print("{0} -> {1}", sText, dwCount);
			//		//		for (uint i = 0; i < dwCount; i++)
			//		//		{
			//		//			if (ErrorHandler.Succeeded(list4.GetTextWithOwnership(i, VSTREETEXTOPTIONS.TTO_DISPLAYTEXT, out sText)))
			//		//				Debug.Print("- {1}, {0}", sText, i);
			//		//		}
			//		//		//list4.GetCategoryField2(
			//		//		list4.FillDescription2(8, (uint)(_VSOBJDESCOPTIONS.ODO_TOOLTIPDESC | _VSOBJDESCOPTIONS.ODO_USEFULLNAME), new DumpDesc()); // 
			//		//	}
			//		//}
			//	}
			//}
			//catch (Exception e)
			//{
			//	Debug.Print(e.Message);
			//}

			return null;
		} // func GetDataTipText

		public override Declarations GetDeclarations(IVsTextView view, int line, int col, TokenInfo info, ParseReason reason)
		{
			// todo: check corrent token
			return declarations;
		} // func GetDeclarations

		public override Methods GetMethods(int line, int col, string name)
		{
			return null;
		}

		public override string Goto(VSConstants.VSStd97CmdID cmd, IVsTextView textView, int line, int col, out TextSpan span)
		{
			span = new TextSpan();
			return null;
		}

		public NeoLuaToken CurrentToken { get { return currentToken; } set { currentToken = value; } }
		public Declarations Declarations { get { return declarations; } set { declarations = value; } }
	} // class NeoLuaAuthoringScope
}
