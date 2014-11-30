using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Neo.IronLua
{
	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public class NeoLuaViewFilter : ViewFilter
	{
		public NeoLuaViewFilter(CodeWindowManager mgr, IVsTextView view)
			: base(mgr, view)
		{
		} // ctor

		#region -- Smart Indent -----------------------------------------------------------

		private string GetStandardIndent()
		{
			if (Source.LanguageService.Preferences.InsertTabs)
				return "\t";
			else
				return new string(' ', Source.LanguageService.Preferences.IndentSize);
		} // func GetStandardIndent

		public override bool HandleSmartIndent()
		{
			int iCaretLine;
			int iCaretColumn;
			if (!ErrorHandler.Succeeded(TextView.GetCaretPos(out iCaretLine, out iCaretColumn)))
				return false;

			if (iCaretLine == 0) // no ident on first line
				return false;


			int iWhiteSpaces = Source.ScanToNonWhitespaceChar(iCaretLine - 1);
			string sIndent = iWhiteSpaces > 0 ? Source.GetText(iCaretLine - 1, 0, iCaretLine - 1, iWhiteSpaces) : String.Empty;
			string sLineData = Source.GetText(iCaretLine - 1, iWhiteSpaces, iCaretLine - 1, Source.GetLineLength(iCaretLine - 1));

			if (sLineData.StartsWith("-- ")) // comment
			{
				int iEnd = 2;

				while (iEnd < sLineData.Length && Char.IsWhiteSpace(sLineData[iEnd]))
					iEnd++;

				Source.SetText(iCaretLine, 0, iCaretLine, iCaretColumn, sIndent + sLineData.Substring(0, iEnd));
				return true;
			}
			else 
			{
				// check for comment
				TokenInfo info = Source.GetTokenInfo(iCaretLine - 1, Source.GetLineLength(iCaretLine - 1));
				// check for smart
				if (info.Type != TokenType.Comment && info.Type != TokenType.LineComment &&  IsSmartIndent(sLineData, iCaretLine, iCaretColumn))
				{
					Source.SetText(iCaretLine, 0, iCaretLine, iCaretColumn, sIndent + GetStandardIndent());
					return true;
				}
				else if (!String.IsNullOrEmpty(sIndent))
				{
					Source.SetText(iCaretLine, 0, iCaretLine, iCaretColumn, sIndent);
					return true;
				}
			}
			return false;
		} // func HandleSmartIndent

		private bool IsSmartIndent(string sLineData, int iCaretLine, int iCaretColumn)
		{
			int iEnd = 0;
			if (sLineData.Length == 0)
				return false;

			while (iEnd < sLineData.Length && Char.IsLetterOrDigit(sLineData[iEnd]))
				iEnd++;

			string sFirstWord = sLineData.Substring(0, iEnd);
			if (sFirstWord == "do" ||
					sFirstWord == "for" ||
					sFirstWord == "foreach" ||
					sFirstWord == "while" ||
					sFirstWord == "repeat" ||
					sFirstWord == "if" ||
					sFirstWord == "elseif" ||
					sFirstWord == "else")
				return sLineData.IndexOf(" end") == -1;

			int iPos = sLineData.IndexOf("function");
			if (iPos == -1)
				return false;

			return sLineData.IndexOf(" end", iPos + 1) == -1;
		} // func IsSmartIndent 

		#endregion
	} // class NeoLuaViewFilter
}
