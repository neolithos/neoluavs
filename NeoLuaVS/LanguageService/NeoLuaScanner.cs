using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Neo.IronLua
{
	///////////////////////////////////////////////////////////////////////////////
	/// <summary>Simple Scanner, for identifing KeyWords, Operators, String, Numbers, Comments and Types.
	/// The Scanner deals:
	/// - with Invalid Strings and Comments.
	/// - type recordnation (identifier chains after "local var :", "const c typeof", "(p1 : , p2 :) :")
	/// - ()[]{} trigger BraceMatch
	/// - (, MethodInfo
	/// - .: MemberSelect</summary>
	public class NeoLuaScanner : IScanner
	{
		///////////////////////////////////////////////////////////////////////////////
		/// <summary></summary>
		private enum SimpleToken
		{
			Unknown,
			Eof,
			WhiteSpace,
			Comment,
			LineComment,
			LongCommentStart,
			Identifier,
			String,
			LongStringStart,
			Number,
			BraceOpen,
			BraceClose,
			BraceSquareOpen,
			BraceSquareClose,
			Comma,
			Dot,
			Colon,
			Operator,
			Braces,
			Type
		} // enum SimpleToken

		private const TokenColor OperatorColor = TokenColor.Number + 1;
		private const TokenColor TypeColor = TokenColor.Number + 2;

		private const int StateFlag = 0xFF;
		private const int DataShift = 8;
		private const int CommentFlag = 0x1;
		private const int StringFlag = 0x2;
		private const int ParserFlag = 0x3C;
		private const int TypeFlag = 0xC0;


		private int offset;      // current offset within the line
		private string line;     // current line

		/// <summary>Sets the current line</summary>
		/// <param name="source"></param>
		/// <param name="offset"></param>
		public void SetSource(string source, int offset)
		{
			this.offset = offset;
			this.line = source;
		} // proc SetSource

		#region -- Simple Lexer -----------------------------------------------------------

		#region -- ScanSimpleToken --------------------------------------------------------

		private void CollectString(char cStringEnd)
		{
			bool lSkipNext = false;
			offset++;
			while (offset < line.Length)
			{
				if (lSkipNext)
					lSkipNext = true;
				else if (line[offset] == '\\')
					lSkipNext = true;
				else if (line[offset] == cStringEnd)
				{
					offset++;
					break;
				}
				offset++;
			}
		} // proc CollectString

		private SimpleToken ScanSimpleToken()
		{
			int iState = 0;

			char cE = 'E';
			char ce = 'e';

			while (offset <= line.Length)
			{
				char c = offset < line.Length ? line[offset] : '\0';

				switch (iState)
				{
					#region -- 0 --
					case 0:
						if (c == '\0')
							return SimpleToken.Eof;
						else if (c == '.')
							iState = 20;
						else if (c == ':')
						{
							offset++;
							return SimpleToken.Colon;
						}
						else if (c == '0')
							iState = 31;
						else if (c >= '1' && c <= '9')
							iState = 30;
						else if (c == '\'' || c == '\"')
						{
							CollectString(c);
							return SimpleToken.String;
						}
						else if (c == '(')
						{
							offset++;
							return SimpleToken.BraceOpen;
						}
						else if (c == '[')
							iState = 12;
						else if (c == ')')
						{
							offset++;
							return SimpleToken.BraceClose;
						}
						else if (c == ']')
						{
							offset++;
							return SimpleToken.BraceSquareClose;
						}
						else if (c == '{' || c == '}')
						{
							offset++;
							return SimpleToken.Braces;
						}
						else if (c == ',')
						{
							offset++;
							return SimpleToken.Comma;
						}
						else if (c == '-')
							iState = 14;
						else if (IsOperatorChar(c))
							iState = 13;
						else if (Char.IsWhiteSpace(c))
							iState = 10;
						else if (Char.IsLetter(c))
							iState = 11;
						else
						{
							offset++;
							return SimpleToken.Unknown; // leave the area as default
						}
						break;
					#endregion
					#region -- 10 -- collect whitespace --
					case 10:
						if (c == '\0' || !Char.IsWhiteSpace(c))
							return SimpleToken.WhiteSpace;
						break;
					#endregion
					#region -- 11 -- collect identifier, keyword --
					case 11:
						if (c == '\0' || !Char.IsLetterOrDigit(c))
							return SimpleToken.Identifier;
						break;
					#endregion
					#region -- 12 -- collect long string --
					case 12:
						if (c == '=' || c == '[')
							return SimpleToken.LongStringStart;
						else
							return SimpleToken.BraceSquareOpen;
					#endregion
					#region -- 13 -- collect operator --
					case 13:
						if (!IsOperatorChar(c))
							return SimpleToken.Operator;
						break;
					#endregion
					#region -- 14,15,16 -- comment --
					case 14:
						if (c == '-') // comment
							iState = 15;
						else
							return SimpleToken.Operator;
						break;
					case 15:
						if (c == '[')
							iState = 16;
						else
						{
							offset = line.Length;
							return SimpleToken.LineComment;
						}
						break;
					case 16:
						if (c == '[' || c == '=')
							return SimpleToken.LongCommentStart;
						else
							goto case 15;
					#endregion
					#region -- 20 -- collect . --
					case 20:
						if (c == '.')
							iState = 21;
						else if (c >= '0' && c <= '9')
							iState = 30;
						else
							return SimpleToken.Dot;
						break;
					case 21:
						if (c != '.')
							return SimpleToken.Operator;
						break;
					#endregion
					#region -- 30 -- number --
					case 30:
						if (c == cE || c == ce)
							iState = 32;
						else if ((c >= '0' && c <= '9') ||
						 (ce == 'p' && ((c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))) ||
						 c == '.')
						{ }
						else
							return SimpleToken.Number;
						break;
					case 31:
						if (c == 'x' || c == 'X')
						{
							ce = 'p';
							cE = 'P';
							iState = 30;
						}
						else
						{
							iState = 30;
							goto case 30;
						}
						break;
					case 32:
						if (c == '+' || c == '-')
							iState = 30;
						else
						{
							iState = 30;
							goto case 30;
						}
						break;
					#endregion
					default:
						throw new InvalidOperationException();
				}

				offset++;
			}

			return SimpleToken.Eof;
		} // func ScanSimpleToken

		#endregion

		private string GetValue(int startAt, int offset)
			=> line.Substring(startAt, offset - startAt);

		private int ScanLevel()
		{
			var level = 0;
			while (offset < line.Length)
			{
				if (line[offset] == '=')
					level++;
				else
					break;
				offset++;
			}
			if (level > 0x7FFFFF)
				throw new OverflowException();
			offset++; // Jum over [
			return level;
		} // func ScanLevel

		private SimpleToken ScanSimpleTokenNonWhiteSpace(ref int startAt)
		{
			startAt = offset;
			var t = ScanSimpleToken();
			return t == SimpleToken.WhiteSpace ? ScanSimpleTokenNonWhiteSpace(ref startAt) : t;
		} // func ScanSimpleTokenNonWhiteSpace

		private static void SetLineStateExtented(ref int lineState, int newState)
		{
			lineState = (lineState & ~ParserFlag) | (newState << 2);
		} // proc SetLineStateExtented

		private static void SetLineStateType(ref int lineState, int newState)
		{
			if (newState == 0)
				SetLineStateData(ref lineState, 0);
			lineState = (lineState & ~TypeFlag) | (newState << 6);
		} // proc SetLineStateType

		private static void SetLineStateData(ref int lineState, int iData)
		{
			lineState = (lineState & StateFlag) | (iData << DataShift);
		} // func SetLineStateData

		private static int GetLineStateData(int lineState)
			=> lineState >> DataShift;
	
		public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int lineState)
		{
			RedoScan:
			var token = SimpleToken.Unknown;
			var startAt = offset;

			RedoLineState:
			if ((lineState & StateFlag) == 0)
			{
				if (token == SimpleToken.Unknown)
					token = ScanSimpleTokenNonWhiteSpace(ref startAt);

				if (token == SimpleToken.Identifier)
				{
					// local var : typedef
					// const var typeof typedef
					// const var : typedef
					// function name.a:a(a : typedef, a : typedef) : typedef
					// do (a : typedef,
					// for a : typedef,
					// foreach a : typedef
					var value = GetValue(startAt, offset);
					if (value == "local" || value == "foreach" || value == "for")
						SetLineStateExtented(ref lineState, 1);
					else if (value == "const")
						SetLineStateExtented(ref lineState, 3);
					else if (value == "function")
						SetLineStateExtented(ref lineState, 5);
					else if (value == "do")
						SetLineStateExtented(ref lineState, 8);
					else if (value == "cast")
						SetLineStateExtented(ref lineState, 13);
				}
				goto EmitToken;
			}
			else if ((lineState & (StringFlag | CommentFlag)) != 0) // Block (String, Comment)
			{
				#region -- block --
				if (offset >= line.Length)
				{
					if (startAt >= offset)
						token = SimpleToken.Eof;
					else
						token = (lineState & StringFlag) == StringFlag ? SimpleToken.String : SimpleToken.LineComment; // Emit part
				}
				else
				{
					var level = GetLineStateData(lineState);
					token = (lineState & StringFlag) == StringFlag ? SimpleToken.String : SimpleToken.LineComment; // Emit part
					while (offset < line.Length)
					{
						if (line[offset] == ']' && offset + level + 1 < line.Length && line[offset + level + 1] == ']')
						{
							// check for equals
							var isValid = true;
							for (int i = offset + 1; i <= offset + level; i++)
							{
								if (line[i] != '=')
								{
									isValid = false;
									break;
								}
							}
							if (isValid)
							{
								offset += level + 2;
								lineState = lineState & (ParserFlag | TypeFlag);
								break;
							}
						}
						offset++;
					}
				}
				goto EmitToken;
				#endregion
			}
			else if ((lineState & TypeFlag) != 0) // typedef parser idenfifier.idenfier[identifier,identifier]
			{
				#region -- typedef --
				var level = GetLineStateData(lineState);
				if (token == SimpleToken.Unknown)
					token = ScanSimpleTokenNonWhiteSpace(ref startAt);
				if (token != SimpleToken.Eof)
				{
					switch ((lineState & TypeFlag) >> 6)
					{
						case 1:
							if (token == SimpleToken.Identifier)
							{
								token = SimpleToken.Type;
								SetLineStateType(ref lineState, 2);
							}
							else
							{
								SetLineStateType(ref lineState, 0);
								goto RedoLineState;
							}
							break;
						case 2:
							if (token == SimpleToken.Dot)
								SetLineStateType(ref lineState, 1);
							else if (token == SimpleToken.Comma)
							{
								if (level == 0)
								{
									SetLineStateType(ref lineState, 0);
									goto RedoLineState;
								}
								else
									SetLineStateType(ref lineState, 1);
							}
							else if (token == SimpleToken.BraceSquareOpen)
							{
								level++;
								if (level > 0x7FFFFF)
									throw new OverflowException();

								SetLineStateData(ref lineState, level);
								SetLineStateType(ref lineState, 1);
							}
							else if (token == SimpleToken.BraceSquareClose)
							{
								level--;
								if (level < 0)
								{
									SetLineStateType(ref lineState, 0);
									goto RedoLineState;
								}
								else
									SetLineStateData(ref lineState, level);
							}
							else
							{
								SetLineStateType(ref lineState, 0);
								goto RedoLineState;
							}
							break;
					}
				}
				goto EmitToken;
				#endregion
			}
			else if ((lineState & ParserFlag) != 0) // extented Parser
			{
				if (token == SimpleToken.Unknown)
					token = ScanSimpleTokenNonWhiteSpace(ref startAt);
				if (token != SimpleToken.Eof)
				{
					switch ((lineState & ParserFlag) >> 2)
					{
						#region -- 1, 12 -- local var : typedef, var : typedef, for, foreach--
						case 1:
							if (token == SimpleToken.Identifier) // identifier
								SetLineStateExtented(ref lineState, 2);
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 2:
							if (token == SimpleToken.Colon)
							{
								SetLineStateExtented(ref lineState, 12);
								SetLineStateType(ref lineState, 1);
							}
							else if (token == SimpleToken.Comma)
								SetLineStateExtented(ref lineState, 1);
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 12:
							if (token == SimpleToken.Comma)
								SetLineStateExtented(ref lineState, 1);
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						#endregion
						#region -- 3 -- const c typeof typedef, const c : typedef --
						case 3:
							if (token == SimpleToken.Identifier)
								SetLineStateExtented(ref lineState, 4);
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 4:
							if ((token == SimpleToken.Identifier && GetValue(startAt, offset) == "typeof") ||
									 token == SimpleToken.Colon)
								SetLineStateType(ref lineState, 1);
							SetLineStateExtented(ref lineState, 0);
							break;
						#endregion
						#region -- 5,14 -- function m.m:m (a : typedef, b : typedef) : typedef --
						case 5:
							if (token == SimpleToken.Identifier)
								SetLineStateExtented(ref lineState, 6);
							else if (token == SimpleToken.BraceOpen)
							{
								token = SimpleToken.Braces;
								SetLineStateExtented(ref lineState, 9);
							}
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 6:
							if (token == SimpleToken.Dot)
								SetLineStateExtented(ref lineState, 5);
							else if (token == SimpleToken.Colon)
								SetLineStateExtented(ref lineState, 7);
							else if (token == SimpleToken.BraceOpen)
							{
								token = SimpleToken.Braces;
								SetLineStateExtented(ref lineState, 9);
							}
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 7:
							if (token == SimpleToken.Identifier)
								SetLineStateExtented(ref lineState, 8);
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 8:
							if (token == SimpleToken.BraceOpen)
							{
								token = SimpleToken.Braces;
								SetLineStateExtented(ref lineState, 9);
							}
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 9: // argument list: a : typedef,
							if (token == SimpleToken.Identifier)
								SetLineStateExtented(ref lineState, 10);
							else if (token == SimpleToken.BraceClose)
								SetLineStateExtented(ref lineState, 14);
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 10:
							if (token == SimpleToken.Colon)
							{
								SetLineStateType(ref lineState, 1);
								SetLineStateExtented(ref lineState, 11);
							}
							else if (token == SimpleToken.Comma)
								SetLineStateExtented(ref lineState, 9);
							else if (token == SimpleToken.BraceClose)
								SetLineStateExtented(ref lineState, 14);
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 11:
							if (token == SimpleToken.Comma)
								SetLineStateExtented(ref lineState, 9);
							else if (token == SimpleToken.BraceClose)
								SetLineStateExtented(ref lineState, 14);
							else
								SetLineStateExtented(ref lineState, 0);
							break;
						case 14:
							if (token == SimpleToken.Colon)
								SetLineStateType(ref lineState, 1);
							SetLineStateExtented(ref lineState, 0);
							break;
						#endregion
						#region -- 13 -- cast(typedef --
						case 13:
							if (token == SimpleToken.BraceOpen)
							{
								SetLineStateType(ref lineState, 1);
								SetLineStateExtented(ref lineState, 0);
							}
							break;
							#endregion
					}
				}
				goto EmitToken;
			}

			throw new InvalidOperationException();

			EmitToken:
			switch (token)
			{
				case SimpleToken.Unknown:
					goto RedoScan;

				case SimpleToken.Eof:
					return false;

				case SimpleToken.WhiteSpace:
					tokenInfo.Color = TokenColor.Text;
					tokenInfo.Type = TokenType.WhiteSpace;
					tokenInfo.Trigger = TokenTriggers.None;
					break;

				case SimpleToken.Comment:
					tokenInfo.Color = TokenColor.Comment;
					tokenInfo.Type = TokenType.Comment;
					tokenInfo.Trigger = TokenTriggers.None;
					break;

				case SimpleToken.LineComment:
					tokenInfo.Color = TokenColor.Comment;
					tokenInfo.Type = TokenType.LineComment;
					tokenInfo.Trigger = TokenTriggers.None;
					break;

				case SimpleToken.String:
					tokenInfo.Color = TokenColor.String;
					tokenInfo.Type = TokenType.String;
					tokenInfo.Trigger = TokenTriggers.None;
					break;

				case SimpleToken.Number:
					tokenInfo.Color = TokenColor.Number;
					tokenInfo.Type = TokenType.Literal;
					tokenInfo.Trigger = TokenTriggers.None;
					break;

				case SimpleToken.BraceOpen:
				case SimpleToken.BraceSquareOpen:
					tokenInfo.Color = OperatorColor;
					tokenInfo.Type = TokenType.Operator;
					tokenInfo.Trigger = TokenTriggers.MatchBraces | TokenTriggers.ParameterStart;
					break;

				case SimpleToken.Comma:
					tokenInfo.Color = OperatorColor;
					tokenInfo.Type = TokenType.Operator;
					tokenInfo.Trigger = TokenTriggers.MatchBraces | TokenTriggers.ParameterNext;
					break;

				case SimpleToken.Dot:
				case SimpleToken.Colon:
					tokenInfo.Color = OperatorColor;
					tokenInfo.Type = TokenType.Delimiter;
					tokenInfo.Trigger = TokenTriggers.MemberSelect;
					break;

				case SimpleToken.Operator:
					tokenInfo.Color = OperatorColor;
					tokenInfo.Type = TokenType.Operator;
					tokenInfo.Trigger = TokenTriggers.None;
					break;

				case SimpleToken.BraceClose:
				case SimpleToken.BraceSquareClose:
				case SimpleToken.Braces:
					tokenInfo.Color = OperatorColor;
					tokenInfo.Type = TokenType.WhiteSpace;
					tokenInfo.Trigger = TokenTriggers.MatchBraces;
					break;

				case SimpleToken.Identifier:
					if (IsKeyword(line, startAt, offset))
					{
						tokenInfo.Color = TokenColor.Keyword;
						tokenInfo.Type = TokenType.Keyword;
						tokenInfo.Trigger = TokenTriggers.None;
					}
					else
					{
						tokenInfo.Color = TokenColor.Identifier;
						tokenInfo.Type = TokenType.Text;
						tokenInfo.Trigger = TokenTriggers.None;
					}
					break;

				case SimpleToken.Type:
					tokenInfo.Color = TypeColor;
					tokenInfo.Type = TokenType.Text;
					tokenInfo.Trigger = TokenTriggers.None;
					break;

				case SimpleToken.LongStringStart:
					lineState = (ScanLevel() << DataShift) | (lineState & StateFlag) | StringFlag;
					goto RedoLineState;
				case SimpleToken.LongCommentStart:
					lineState = (ScanLevel() << DataShift) | (lineState & StateFlag) | CommentFlag;
					goto RedoLineState;
			}

			tokenInfo.StartIndex = startAt;
			tokenInfo.Color = tokenInfo.Color;
			tokenInfo.EndIndex = offset - 1;
			return true;
		} // func ScanTokenAndProvideInfoAboutIt

		#endregion

		// -- Static --------------------------------------------------------------

		/// <summary>List with keywords</summary>
		/// <remarks>must be sorted</remarks>
		private static readonly string[] keywords = new string[]
		{
			"and",
			"break",
			"cast",
			"const",
			"do",
			"else",
			"elseif",
			"end",
			"false",
			"for",
			"foreach",
			"function",
			"goto",
			"if",
			"in",
			"local",
			"nil",
			"not",
			"or",
			"repeat",
			"return",
			"then",
			"true",
			"typeof", // no real keyword, but mark it
			"until",
			"while"
		};

		private const string operators = "+-*/%^&|~#~<>=;";

		private static bool IsOperatorChar(char c)
		{
			return operators.IndexOf(c) >= 0;
		} // func IsOperatorChar

		private static bool IsKeyword(string sLine, int iStart, int iEnd)
		{
			return Array.BinarySearch(keywords, sLine.Substring(iStart, iEnd - iStart)) >= 0;
		} // func IsKeyword
	} // class NeoLuaScanner
}
