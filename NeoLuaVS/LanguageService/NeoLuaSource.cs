using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
	public class NeoLuaSource : Source
	{
		private bool lIsChunkDirty = true;

		private NeoLuaChunk currentChunk = null;
		private NeoLuaAuthoringScope authoringScope;

		private IVsSimpleLibrary2 library = null;
		private IVsSimpleBrowseComponentSet libraryScope = null;
		private int iLastTypeUpdated = 0;
		private TypeListItem typeRoot = null;

		#region -- Ctor/Dtor --------------------------------------------------------------

		public NeoLuaSource(NeoLuaLanguageService languageService, IVsTextLines buffer, Colorizer colorizer)
			: base(languageService, buffer, colorizer)
		{
			authoringScope = new NeoLuaAuthoringScope(this);
		} // ctor

		public override void Dispose()
		{
			try
			{
				if (libraryScope != null)
				{
					Marshal.ReleaseComObject(libraryScope);
					libraryScope = null;
				}
				if (library != null)
				{
					Marshal.ReleaseComObject(library);
					library = null;
				}
			}
			finally
			{
				base.Dispose();
			}
		} // prop Dispose

		#endregion

		#region -- Parser -----------------------------------------------------------------

		public NeoLuaChunk ParseChunk(string sText)
		{
			if (lIsChunkDirty)
			{
				using (LuaLexer l = new LuaLexer(GetFilePath(), new StringReader(sText ?? GetText())))
					currentChunk = NeoLuaChunk.Parse(l);
				lIsChunkDirty = false;
			}
			return currentChunk;
		} // func ParseChunk

		#endregion

		#region -- OnCommand --------------------------------------------------------------

		private bool IsValidSpaceIndent(string sLineData, int iOfs, out int iSpaces)
		{
			int iIndentSize = LanguageService.Preferences.IndentSize;
			iSpaces = 0;
			while (iOfs >= 0 && sLineData[iOfs] == ' ' && iIndentSize >= 0)
			{
				iSpaces++;
				iOfs--;
				iIndentSize--;
			}
			return iSpaces > 0;
		} // func IsValidSpaceIndent

		private int DeindentLine(int iLine, int iColumn, int iWordLength)
		{
			int iOfs = ScanToNonWhitespaceChar(iLine) - 1;
			if (iOfs >= 0 && iOfs + iWordLength - 1 == iColumn)
			{
				bool lChanged = false;
				string sLineData = GetText(iLine, 0, iLine, iOfs + 1);
				int iSpaces;
				if (sLineData[iOfs] == '\t')
				{
					sLineData = sLineData.Remove(iOfs, 1);
					iColumn--;
					lChanged = true;
				}
				else if (IsValidSpaceIndent(sLineData, iOfs, out iSpaces))
				{
					sLineData = sLineData.Remove(iOfs - iSpaces + 1, iSpaces);
					iColumn -= iSpaces;
					lChanged = true;
				}
				if (lChanged)
					SetText(iLine, 0, iLine, iOfs + 1, sLineData);
			}
			return iColumn;
		} // func DeindentLine

		public override void OnCommand(IVsTextView textView, VSConstants.VSStd2KCmdID command, char ch)
		{
			if (command == VSConstants.VSStd2KCmdID.TYPECHAR && (ch == 'd' || ch == 'k' || ch == 'l' || ch == 'f' || ch == 'e'))
			{
				bool lHighlightBraces = LanguageService.Preferences.EnableMatchBraces && LanguageService.Preferences.EnableMatchBracesAtCaret;
				int iLine;
				int iColumn;
				if (!ErrorHandler.Succeeded(textView.GetCaretPos(out iLine, out iColumn)))
					return;

				if (ch == 'd' && iColumn >= 3 && GetText(iLine, iColumn - 3, iLine, iColumn) == "end")
				{
					// Deindent on single end
					iColumn = DeindentLine(iLine, iColumn, 3);

					// highlight
					if (lHighlightBraces)
					{
						TokenInfo info = GetTokenInfo(iLine, iColumn);
						this.MatchBraces(textView, iLine, iColumn, info);
					}
				}
				else if (ch == 'f' && iColumn >= 6 && GetText(iLine, iColumn - 6, iLine, iColumn) == "elseif")
					iColumn = DeindentLine(iLine, iColumn, 6);
				else if (ch == 'e' && iColumn >= 4 && GetText(iLine, iColumn - 4, iLine, iColumn) == "else")
					iColumn = DeindentLine(iLine, iColumn, 4);
				else if (lHighlightBraces && (
					ch == 'k' && iColumn >= 5 && GetText(iLine, iColumn - 3, iLine, iColumn) == "break" ||
					ch == 'l' && iColumn >= 5 && GetText(iLine, iColumn - 3, iLine, iColumn) == "until"
					))
				{
					TokenInfo info = GetTokenInfo(iLine, iColumn);
					this.MatchBraces(textView, iLine, iColumn, info);
				}
			}
			else
				base.OnCommand(textView, command, ch);
		} // proc OnCommand

		#endregion

		#region -- Commenting -------------------------------------------------------------

		public override CommentInfo GetCommentFormat()
		{
			CommentInfo c = new CommentInfo();
			c.LineStart = "--";
			c.BlockStart = null;
			c.BlockEnd = null;
			c.UseLineComments = true;
			return c;
		} // func GetCommentFormat

		public override TextSpan CommentLines(TextSpan span, string lineComment)
		{
			// Search for min block
			int iMin = int.MaxValue;
			for (int i = span.iStartLine; i <= span.iEndLine; i++)
			{
				int t = ScanToNonWhitespaceChar(i);
				if (t == GetLineLength(i))
					continue;
				if (iMin > t)
					iMin = t;
			}

			if (iMin == int.MaxValue)
				return span;

			// comment lines out
			bool lExpandLength = false;
			for (int i = span.iStartLine; i <= span.iEndLine; i++)
			{
				if (GetLineLength(i) > iMin)
				{
					SetText(i, iMin, i, iMin, lineComment);
					lExpandLength = true;
				}
				else
					lExpandLength = false;
			}

			if (lExpandLength)
				span.iEndIndex += lineComment.Length;
			return span;
		} // proc CommentLines

		public override TextSpan UncommentLines(TextSpan span, string lineComment)
		{
			for (int i = span.iStartLine; i <= span.iEndLine; i++)
			{
				int iStart = ScanToNonWhitespaceChar(i);
				if (iStart + 2 < GetLineLength(i) && GetText(i, iStart, i, iStart + 2) == lineComment)
				{
					SetText(i, iStart, i, iStart + 2, String.Empty);
					if (i == span.iStartLine && span.iStartIndex != 0)
						span.iStartIndex = iStart;
				}
			}

			return span;
		} // func UncommentLines

		#endregion

		#region -- Library ----------------------------------------------------------------

		#region -- class CreateToolTipText ------------------------------------------------

		///////////////////////////////////////////////////////////////////////////////
		/// <summary></summary>
		private class CreateToolTipText : IVsObjectBrowserDescription3
		{
			private StringBuilder sb = new StringBuilder();

			public int AddDescriptionText3(string pText, VSOBDESCRIPTIONSECTION obdSect, IVsNavInfo pHyperJump)
			{
				sb.Append(pText);
				return VSConstants.S_OK;
			} // func AddDescriptionText3

			public int ClearDescriptionText()
			{
				sb.Clear();
				return VSConstants.S_OK;
			} // func ClearDescriptionText

			public override string ToString()
			{
				return sb.ToString();
			} // func ToString
		} // class CreateToolTipText

		#endregion

		#region -- class TypeListItem -----------------------------------------------------

		///////////////////////////////////////////////////////////////////////////////
		/// <summary></summary>
		private class TypeListItem : Declarations, IComparable<TypeListItem>
		{
			public const uint NamespaceIndex = 0xFFFFFFFF;
			private uint dwIndex;
			private string sName;
			private IVsSimpleObjectList2 list;
			private List<TypeListItem> items = new List<TypeListItem>();

			public TypeListItem(IVsSimpleObjectList2 list, uint dwIndex, string sName)
			{
				this.list = list;
				this.dwIndex = dwIndex;
				this.sName = sName;
			} // ctor

			public int CompareTo(TypeListItem other)
			{
				return String.Compare(sName, other.sName);
			} // func CompareTo

			public TypeListItem Add(uint dwIndex, string sName)
			{
				TypeListItem item = new TypeListItem(list, dwIndex, sName);
				int iPos = items.BinarySearch(item);
				if(iPos < 0)
					items.Insert(~iPos, item);
				else
					item = items[iPos];
				return item;
			} // func Add

			public TypeListItem FindNameSpace(int iStart, string sNamespace, bool lAdd)
			{
				if (iStart >= sNamespace.Length)
					return this;
				else
				{
					int iPos = sNamespace.IndexOf('.', iStart);
					string sPart = iPos == -1 ? sNamespace.Substring(iStart) : sNamespace.Substring(iStart, iPos - iStart);

					TypeListItem f = new TypeListItem(list, NamespaceIndex, sPart);
					int iFindIndex = items.BinarySearch(f);
					if (iFindIndex >= 0)
						f = items[iFindIndex];
					else if (lAdd)
						items.Insert(~iFindIndex, f);

					if (f == null)
						return null;
					else if (iPos == -1)
						return f;
					else
						return f.FindNameSpace(iPos + 1, sNamespace, lAdd);
				}
			} // func FindNameSpace

			public override int GetCount()
			{
				return items.Count;
			} // func GetCount

			private string GetName()
			{
				if (dwIndex == NamespaceIndex )
					return sName;
				else
				{
					string sTmp;
					ErrorHandler.ThrowOnFailure(list.GetTextWithOwnership(dwIndex, VSTREETEXTOPTIONS.TTO_DEFAULT, out sTmp));
					return sTmp;
				}
			} // func GetName

			private string GetDisplayText()
			{
				if (dwIndex == NamespaceIndex)
					return sName;
				else
				{
					string sDisplayText;
					ErrorHandler.ThrowOnFailure(list.GetTextWithOwnership(dwIndex, VSTREETEXTOPTIONS.TTO_DISPLAYTEXT, out sDisplayText));
					return sDisplayText;
				}
			} // func GetDisplayText

			private string GetDescription()
			{
				if (dwIndex == NamespaceIndex)
					return "namespace " + sName;
				else
				{
					//uint dw;
					//if (ErrorHandler.Succeeded(list.GetCategoryField2(dwIndex, (int)LIB_CATEGORY.LC_CLASSTYPE, out dw)))
					//	return ((_LIBCAT_CLASSTYPE)dw).ToString();

					CreateToolTipText tt = new CreateToolTipText();
					if (ErrorHandler.Succeeded(list.FillDescription2(dwIndex, (uint)_VSOBJDESCOPTIONS.ODO_NONE, tt)))
						return tt.ToString();
					else
						return "class " + sName;
				}
			} // func GetDescription

			private int GetGlyph()
			{
				if (dwIndex == NamespaceIndex)
					return 90;
				else
				{
					VSTREEDISPLAYDATA[] data = new VSTREEDISPLAYDATA[1];
					ErrorHandler.ThrowOnFailure(list.GetDisplayData(dwIndex, data));
					return data[0].Image;
				}
			} // func GetGlyph

			public override string GetName(int index)
			{
				if (index < 0)
					return String.Empty;
				return items[index].GetName();
			} // func GetName

			public override string GetDisplayText(int index)
			{
				return items[index].GetDisplayText();
			} // func GetDisplayText

			public override string GetDescription(int index)
			{
				return items[index].GetDescription();
			} // func GetDescription

			public override int GetGlyph(int index)
			{
				return items[index].GetGlyph();
			} // func GetGlyph
		} // class TypeListItem

		#endregion

		private static readonly Guid guidCOMplusplus = new Guid("1ec72fd7-c820-4273-9a21-777a5c522e03");

		private void InitComponent()
		{
			if (library != null && libraryScope != null)
				return;

			// Get the object manager
			IVsObjectManager2 mgr = LanguageService.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
			if (mgr == null)
				throw new ArgumentException("object mangager not found.");

			if (library == null)
			{
				// Find the com+ library
				IVsLibrary2 libraryCInterface;
				Guid guid = guidCOMplusplus;
				ErrorHandler.ThrowOnFailure(mgr.FindLibrary(ref guid, out libraryCInterface));
				library = libraryCInterface as IVsSimpleLibrary2;
				if (library == null)
					throw new ArgumentException("COM+ library not found.");
			}

			if (libraryScope == null)
			{
				// create the command set for the source file
				ErrorHandler.ThrowOnFailure(mgr.CreateSimpleBrowseComponentSet((uint)_BROWSE_COMPONENT_SET_TYPE.BCST_EXCLUDE_LIBRARIES, null, 0, out libraryScope));

				// Add compnents
				string sBasePath = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\";
				Guid guid;
				uint dwStructSize = (uint)Marshal.SizeOf(typeof(VSCOMPONENTSELECTORDATA));
				IVsNavInfo nav;
				ErrorHandler.ThrowOnFailure(library.GetGuid(out guid));
				VSCOMPONENTSELECTORDATA[] a = new VSCOMPONENTSELECTORDATA[1];
				ErrorHandler.ThrowOnFailure(libraryScope.AddComponent(ref guid,
					new[]
					{
						new VSCOMPONENTSELECTORDATA()
						{
							dwSize = dwStructSize,
							bstrFile = sBasePath + "mscorlib.dll",
							bstrTitle = "mscorlib",
							type = VSCOMPONENTTYPE.VSCOMPONENTTYPE_ComPlus
						}
					}, out nav, a));
			}
		} // proc InitComponent

		private void UpdateTypeList()
		{
			IVsSimpleObjectList2 simpleList;
			ErrorHandler.ThrowOnFailure(library.GetList2((uint)_LIB_LISTTYPE.LLT_CLASSES, (uint)_LIB_FLAGS.LF_GLOBAL,
				new[]
					{
						new VSOBSEARCHCRITERIA2()
						{
							eSrchType = VSOBSEARCHTYPE.SO_ENTIREWORD,
							szName = String.Empty
						}
					}, out simpleList));

			uint dwCount;
			ErrorHandler.ThrowOnFailure(simpleList.GetItemCount(out dwCount));

			typeRoot = new TypeListItem(simpleList, TypeListItem.NamespaceIndex, String.Empty);

			for (uint i = 0; i < dwCount; i++)
			{
				// Ermittle den Namen
				string sNamespace;
				string sTypeName;
				ErrorHandler.ThrowOnFailure(simpleList.GetTextWithOwnership(i, VSTREETEXTOPTIONS.TTO_PREFIX2, out sNamespace));
				ErrorHandler.ThrowOnFailure(simpleList.GetTextWithOwnership(i, VSTREETEXTOPTIONS.TTO_DEFAULT, out sTypeName));

				// Hole die Attribute ab
				uint dwClassType;
				uint dwClassAccess;
				ErrorHandler.ThrowOnFailure(simpleList.GetCategoryField2(i, (int)LIB_CATEGORY.LC_CLASSTYPE, out dwClassType));
				ErrorHandler.ThrowOnFailure(simpleList.GetCategoryField2(i, (int)LIB_CATEGORY.LC_CLASSACCESS, out dwClassAccess));

				// Keine eigentlichen Typen und Namespaces
				if (((_LIBCAT_CLASSTYPE)dwClassType) == _LIBCAT_CLASSTYPE.LCCT_INTRINSIC ||
					((_LIBCAT_CLASSTYPE)dwClassType) == _LIBCAT_CLASSTYPE.LCCT_NSPC)
					continue;

				// Nur öffentliche Methoden
				if ((dwClassAccess & (uint)_LIBCAT_CLASSACCESS.LCCA_PUBLIC) == 0)
					continue;

				// Suche den Knoten
				typeRoot.FindNameSpace(0, sNamespace, true).Add(i, sTypeName);
			}

			iLastTypeUpdated = Environment.TickCount;
		} // proc UpdateTypeList

		public Declarations FindDeclarations(bool lTypes, string sStarts)
		{
			InitComponent();

			if (lTypes)
			{
				// Aktualisiere die Typ-Liste
				if (typeRoot == null || Math.Abs(Environment.TickCount - iLastTypeUpdated) > 60000)
				{
					typeRoot = null;
					UpdateTypeList();
				}

				// Suche den Passenden Knoten
				return typeRoot.FindNameSpace(0, sStarts, false);
			}
			else
				return null;

			//IVsNavInfo navInfo;
			//Guid guid;
			//library.GetGuid(out guid);
			//libraryScope.get_RootNavInfo(out navInfo);
			//ErrorHandler.ThrowOnFailure(libraryScope.CreateNavInfo(ref guid,
			//	new SYMBOL_DESCRIPTION_NODE[] { new SYMBOL_DESCRIPTION_NODE() { dwType = (uint)_LIB_LISTTYPE.LLT_NAMESPACES, pszName = "System" } }, 1, out navInfo));

			//IVsObjectList2 listC;
			//ErrorHandler.ThrowOnFailure(libraryScope.put_ChildListOptions((uint)(_BROWSE_COMPONENT_SET_OPTIONS.BCSO_NO_DRAG_DROP | _BROWSE_COMPONENT_SET_OPTIONS.BCSO_NO_REMOVE | _BROWSE_COMPONENT_SET_OPTIONS.BCSO_NO_RENAME)));
			//ErrorHandler.ThrowOnFailure(libraryScope.GetList2((uint)_LIB_LISTTYPE.LLT_NAMESPACES, (uint)_LIB_FLAGS.LF_GLOBAL,
			//	new[]
			//	{
			//		new VSOBSEARCHCRITERIA2()
			//		{
			//			eSrchType = VSOBSEARCHTYPE.SO_ENTIREWORD,
			//			szName = sStarts
			//		}
			//	}, null, out listC));

			//IVsSimpleObjectList2 simpleList = listC as IVsSimpleObjectList2;
			//ErrorHandler.ThrowOnFailure(library.GetList2((uint)(_LIB_LISTTYPE.LLT_CLASSES), (uint)_LIB_FLAGS.LF_GLOBAL,
			//	new[]
			//	{
			//		new VSOBSEARCHCRITERIA2()
			//		{
			//			eSrchType = VSOBSEARCHTYPE.SO_ENTIREWORD,
			//			szName = sStarts
			//		}
			//	}, out simpleList));
			
			//string sTmp;
			//simpleList.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_BASETEXT, out sTmp);
			//Debug.Print(sTmp);
			//simpleList.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_CUSTOM, out sTmp);
			//Debug.Print(sTmp);
			//simpleList.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_DEFAULT, out sTmp);
			//Debug.Print(sTmp);
			//simpleList.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_DISPLAYTEXT, out sTmp);
			//Debug.Print(sTmp);
			//simpleList.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_EXTENDED, out sTmp);
			//Debug.Print(sTmp);
			//simpleList.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_PREFIX, out sTmp);
			//Debug.Print(sTmp);
			//simpleList.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_PREFIX2, out sTmp);
			//Debug.Print(sTmp);
			//simpleList.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_SEARCHTEXT, out sTmp);
			//Debug.Print(sTmp);
			//simpleList.GetTextWithOwnership(0, VSTREETEXTOPTIONS.TTO_SORTTEXT, out sTmp);
			//Debug.Print(sTmp);


			//Guid riid = typeof(IVsSimpleObjectList2).GUID;
			//IntPtr p;
			//Marshal.QueryInterface(Marshal.GetIUnknownForObject(listC), ref riid, out p);

			//simpleList = listC as IVsSimpleObjectList2;
			
			//simp

			//return new ObjectListDeclarations(simpleList);
		} // func FidDeclarations

		#endregion

		public override bool IsDirty
		{
			get
			{
				return base.IsDirty;
			}
			set
			{
				if (value)
					lIsChunkDirty = true;
				base.IsDirty = value;
			}
		} // prop IsDirty

		public bool IsChunkDirty { get { return lIsChunkDirty; } }
		public NeoLuaChunk ParseInfo { get { return currentChunk; } }
		public NeoLuaAuthoringScope AuthoringScope { get { return authoringScope; } }
	} // class NeoLuaSource
}
 