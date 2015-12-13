using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Neo.IronLua
{
	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	[
	Guid(GuidList.guidLanguageService)
	]
	public class NeoLuaLanguageService : LanguageService
	{
		#region -- Color items ------------------------------------------------------------

		private ColorableItem[] colorItems = new ColorableItem[]
    {
      new ColorableItem("Text", "Text", COLORINDEX.CI_USERTEXT_FG, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
      new ColorableItem("Keyword", "Keyword", COLORINDEX.CI_BLUE, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
      new ColorableItem("Comment", "Comment", COLORINDEX.CI_DARKGREEN, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
      new ColorableItem("Identifier", "Identifier", COLORINDEX.CI_USERTEXT_FG, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
      new ColorableItem("String", "String", COLORINDEX.CI_MAROON, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
      new ColorableItem("Number", "Number", COLORINDEX.CI_DARKBLUE, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
      new ColorableItem("Operator", "Operator", COLORINDEX.CI_DARKGRAY, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
      new ColorableItem("NeoLua-Type", "NeoLua-Type", COLORINDEX.CI_AQUAMARINE, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT)
    };

		#endregion

		private NeoLuaScanner scanner = new NeoLuaScanner();
		private LanguagePreferences languagePreferences = null;

		#region -- Ctor/Dtor --------------------------------------------------------------

		public NeoLuaLanguageService()
		{
		} // ctor

		public override string GetFormatFilterList()
		{
			return "Lua-File (*.lua,*.nlua)\n*.lua|*.nlua\n";
		} // func GetFormatFilterList

		public override LanguagePreferences GetLanguagePreferences()
		{
			if (languagePreferences == null)
			{
				languagePreferences = new LanguagePreferences(Site, typeof(NeoLuaLanguageService).GUID, Name);
				languagePreferences.Init();
			}
			return languagePreferences;
		} // func GetLanguagePreferences

		public override IScanner GetScanner(IVsTextLines buffer)
		{
			return scanner;
		} // func GetScanner

		public override Source CreateSource(IVsTextLines buffer)
		{
			return new NeoLuaSource(this, buffer, GetColorizer(buffer));
		} // func CreateSource

		public override ViewFilter CreateViewFilter(CodeWindowManager mgr, IVsTextView newView)
		{
			return new NeoLuaViewFilter(mgr, newView);
		} // func CreateViewFilter

		#endregion

		#region -- IVsProvideColorableItem ------------------------------------------------

		public override int GetItemCount(out int count)
		{
			count = colorItems.Length - 1;
			return VSConstants.S_OK;
		} // func GetItemCount

		public override int GetColorableItem(int index, out IVsColorableItem item)
		{
			if (index <= 0 || index > colorItems.Length)
			{
				item = null;
				return VSConstants.S_FALSE;
			}
			item = colorItems[index];
			return VSConstants.S_OK;
		} // func GetColorableItem

		#endregion

		#region -- ParseSource -------------------------------------------------------------

		public override AuthoringScope ParseSource(ParseRequest req)
		{
			// Get the source
			NeoLuaSource s = GetSource(req.View) as NeoLuaSource;
			if (s == null)
				return null;
			Debug.Print("ParseSource: {0} -> {1}", req.Reason, s.IsDirty);

			NeoLuaAuthoringScope scope = s.AuthoringScope;
			NeoLuaChunk c;
			switch (req.Reason)
			{
				#region -- check --
				case ParseReason.Check:
					{
						c = s.ParseChunk(req.Text);

						// Add errors, Regions
						var currentToken = c.FirstToken;
						var fileName = s.GetFilePath();
						while (currentToken != null)
						{
							if (!String.IsNullOrEmpty(currentToken.Error))
								req.Sink.AddError(fileName, currentToken.Error, currentToken.Span, currentToken.ErrorSeverity);
							if (currentToken.Token == LuaToken.Comment && currentToken.EndLine - currentToken.StartLine >= 2)
							{
								req.Sink.ProcessHiddenRegions = true;
								string sHiddenHint = s.GetText(currentToken.StartLine, currentToken.StartIndex, currentToken.StartLine, Math.Min(66, s.GetLineLength(currentToken.StartLine))) + " ...";
								req.Sink.AddHiddenRegion(currentToken.Span, sHiddenHint);
							}

							currentToken = currentToken.Next;
						}

						// Add hidden regions
						var currentScope = c.FirstScope;
						while (currentScope != null)
						{
							if (currentScope.CanHiddenRegion)
							{
								NeoLuaToken f = currentScope.FirstToken;
								NeoLuaToken l = currentScope.LastToken;

								req.Sink.ProcessHiddenRegions = true;
								req.Sink.AddHiddenRegion(new TextSpan() { iStartLine = f.StartLine, iStartIndex = f.StartIndex, iEndLine = l.EndLine, iEndIndex = l.EndIndex }, currentScope.FirstLine);
							}
							currentScope = currentScope.NextScope;
						}
					}
					break;
				#endregion
				#region -- braces --
				case ParseReason.MatchBraces:
				case ParseReason.HighlightBraces:
					{
						c = s.ParseChunk(req.Text);
						NeoLuaToken tStart = c.FindToken(req.Line, req.Col);
						if (tStart != null)
						{
							NeoLuaToken tEnd = null;
							switch (tStart.Token)
							{
								case LuaToken.BracketOpen:
									tEnd = FindMatchToken(tStart, LuaToken.BracketOpen, LuaToken.BracketClose, true);
									break;
								case LuaToken.BracketCurlyOpen:
									tEnd = FindMatchToken(tStart, LuaToken.BracketCurlyOpen, LuaToken.BracketCurlyClose, true);
									break;
								case LuaToken.BracketSquareOpen:
									tEnd = FindMatchToken(tStart, LuaToken.BracketSquareOpen, LuaToken.BracketSquareClose, true);
									break;
								case LuaToken.BracketClose:
									tEnd = FindMatchToken(tStart, LuaToken.BracketClose, LuaToken.BracketOpen, false);
									break;
								case LuaToken.BracketCurlyClose:
									tEnd = FindMatchToken(tStart, LuaToken.BracketCurlyClose, LuaToken.BracketCurlyOpen, false);
									break;
								case LuaToken.BracketSquareClose:
									tEnd = FindMatchToken(tStart, LuaToken.BracketSquareClose, LuaToken.BracketSquareOpen, false);
									break;
								case LuaToken.KwEnd:
								case LuaToken.KwBreak:
									TextSpan[] matches = FindEndMatchToken(tStart);
									if (matches != null)
										req.Sink.MatchMultiple(matches, 1);
									break;
							}

							if (tEnd != null)
								req.Sink.MatchPair(tStart.Span, tEnd.Span, 1);
						}
					}
					break;
				#endregion
				case ParseReason.DisplayMemberList:
				case ParseReason.MemberSelect:
				case ParseReason.CompleteWord:
					{
						c = s.ParseChunk(req.Text);
						var t = c.FindToken(req.Line, req.Col);
						var typeScope = t?.Parent as NeoLuaTypeScope ?? (t.Next != null ? t.Next.Parent as NeoLuaTypeScope : null);
						if (typeScope != null)
						{
							scope.CurrentToken = t;
							scope.Declarations = s.FindDeclarations(true, typeScope.TypeName);
						}
						//else if (t.Prev.Token == LuaToken.Identifier && t.Prev.Value == "clr")
						//{



						//}
						else
							scope.Declarations = null;
					}
					break;

				case ParseReason.QuickInfo:
					break;
				case ParseReason.MethodTip:
					break;

				case ParseReason.Goto:
					break;

				case ParseReason.CodeSpan:
					break;
			}

			return scope;
		} // func ParseSource

		#region -- FindMatchToken ---------------------------------------------------------

		private NeoLuaToken FindMatchToken(NeoLuaToken current, LuaToken incToken, LuaToken decToken, bool lNext)
		{
			int iNested = 0;
			while (current != null)
			{
				if (current.Token == incToken)
					iNested++;
				else if (current.Token == decToken)
				{
					iNested--;
					if (iNested == 0)
						return current;
				}

				current = lNext ? current.Next : current.Prev;
			}
			return null;
		} // func FindMatchToken

		private TextSpan[] FindEndMatchToken(NeoLuaToken current)
		{
			List<TextSpan> matches = new List<TextSpan>();

			if (current != null && current.Token == LuaToken.KwBreak)
				current = current.Parent.LastToken;
			if (current == null || current.Token != LuaToken.KwEnd)
				return null;

			NeoLuaToken first = current.Parent.FirstToken;
			NeoLuaScope currentScope = current.Parent;
			if (first.Token == LuaToken.BracketOpen) // lambda
			{
				while (first != null && first.Token != LuaToken.KwFunction)
					first = first.Prev;
				if (first == null)
					return null;
			}

			// add first token
			matches.Add(first.Span);

			// search for "do"
			if (first.Token == LuaToken.KwFor ||
				first.Token == LuaToken.KwForEach ||
				first.Token == LuaToken.KwWhile ||
				first.Token == LuaToken.KwDo)
			{
				NeoLuaToken c = first.Next;
				while (c != null && c != current)
				{
					bool lInScope = NeoLuaBlockScope.CheckInLoopScope(c.Parent, currentScope);

					if (lInScope &&
						(
							c.Token == LuaToken.KwDo ||
							c.Token == LuaToken.KwIn ||
							c.Token == LuaToken.KwBreak)
						)
					{
						matches.Add(c.Span);
					}
					c = c.Next;
				}
			}
			else if (first.Token == LuaToken.KwIf)
			{
				NeoLuaToken c = first;
				while (c != null && c != current)
				{
					if (c.Parent == currentScope &&
						(
							c.Token == LuaToken.KwElse ||
							c.Token == LuaToken.KwElseif ||
							c.Token == LuaToken.KwThen
						)
					)
					{
						matches.Add(c.Span);
					}

					c = c.Next;
				}
			}

			matches.Add(current.Span);

			return matches.Count > 1 ? matches.ToArray() : null;
		} // func FindMatchToken

		#endregion

		#endregion

		public override void OnIdle(bool periodic)
		{
			// from IronPythonLanguage sample
			// this appears to be necessary to get a parse request with ParseReason = Check?
			var src = (Source)GetSource(this.LastActiveTextView);
			if (src != null && src.LastParseTime >= Int32.MaxValue >> 12)
			{
				src.LastParseTime = 0;
			}
			//Debug.Print("Document check: isDirty={0}, {1}ms old, isParsing={2} ", src.IsDirty, Environment.TickCount - src.LastParseTime, IsParsing);
			base.OnIdle(periodic);
		} // proc OnIdle

		public override string Name { get { return "NeoLua"; } }
	} // class NeoLuaLanguageService
}
