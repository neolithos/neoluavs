using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

// todo: named arguments

namespace Neo.IronLua
{
	#region -- class NeoLuaToken --------------------------------------------------------

	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public sealed class NeoLuaToken
	{
		private LuaToken typ;
		private TextSpan span;
		private string sValue;
		
		private Severity severity = Severity.Hint;
		private string sError;
		
		private NeoLuaScope parent = null;
		private NeoLuaToken next = null;
		private NeoLuaToken prev = null;

		public NeoLuaToken(NeoLuaScope parent, NeoLuaToken prev, Token tok)
		{
			this.parent = parent;
			this.prev = prev;
			this.typ = tok.Typ;
			this.sValue = tok.Value;
			this.span = new TextSpan { iStartLine = tok.Start.Line - 1, iStartIndex = tok.Start.Col - 1, iEndLine = tok.End.Line - 1, iEndIndex = tok.End.Col - 1};
		} // ctor

		public override string ToString()
		{
			return String.IsNullOrEmpty(sValue) ?
				String.Format("{0}", typ) :
				String.Format("{0}: {1}", typ, sValue);
		} // func ToString

		public void SetError(Severity severity, string sError)
		{
			this.severity = severity;
			this.sError = sError;
		} // proc SetError

		public void Combine(Token tok)
		{
			if (tok.Typ != typ)
				throw new InvalidOperationException();

			span = new TextSpan
			{
				iStartLine = span.iStartLine,
				iStartIndex = span.iStartIndex,
				iEndLine = tok.End.Line - 1,
				iEndIndex = tok.End.Col - 1
			};
		} // proc Combine

		public TextSpan Span { get { return span; } }

		public LuaToken Token { get { return typ; } }
		public string Error { get { return sError; } }
		public Severity ErrorSeverity { get { return severity; } }
		public string Value { get { return sValue; } }

		public int StartLine { get { return span.iStartLine; } }
		public int StartIndex { get { return span.iStartIndex; } }
		public int EndLine { get { return span.iEndLine; } }
		public int EndIndex { get { return span.iEndIndex; } }

		public NeoLuaToken Next { get { return next; } internal set { next = value; } }
		public NeoLuaToken Prev { get { return prev; } internal set { prev = value; } }
		public NeoLuaScope Parent { get { return parent; } }
	} // class NeoLuaToken

	#endregion

	#region -- class NeoLuaScope --------------------------------------------------------

	///////////////////////////////////////////////////////////////////////////////
	/// <summary>Descripe a scope</summary>
	public class NeoLuaScope
	{
		private NeoLuaScope parent = null;
		private NeoLuaScope firstScope = null; // first sub scope
		private NeoLuaScope lastScope = null; // of sub scope

		private NeoLuaScope nextScope = null; // next scope
		
		private NeoLuaToken firstToken = null;
		private NeoLuaToken lastToken = null;

		protected NeoLuaScope()
		{
		} // ctor

		/// <summary>Add the the token to the list.</summary>
		/// <param name="token"></param>
		/// <param name="scope"></param>
		public void AddToken(Token token)
		{
			// Append the item to the list
			var t = new NeoLuaToken(this, lastToken, token);
			if (firstToken == null)
				SetFirstToken(t);
			if (lastToken != null)
				lastToken.Next = t;
			SetLastToken(t);
		} // proc AddToken

		public T AddScope<T>()
			where T : NeoLuaScope
		{
			T scope = Activator.CreateInstance<T>();
			scope.parent = this;
			scope.lastToken = lastToken;
			scope.lastScope = scope;

			if (firstScope == null)
				firstScope = scope;
			if (lastScope != null)
				lastScope.nextScope = scope;
			SetLastScope(scope);
			return scope;
		} // proc AddScope

		public NeoLuaToken FindToken(int iLine, int iColumn)
		{
			NeoLuaToken current = firstToken;
			while (current != null)
			{
				if (iLine >= current.StartLine && iLine <= current.EndLine &&
					iColumn >= current.StartIndex && iColumn <= current.EndIndex)
					return current;

				current = current.Next;
			}
			return null;
		} // func FindToken

		private void SetLastScope(NeoLuaScope scope)
		{
			lastScope = scope;
			if (parent != null)
				parent.SetLastScope(scope);
		} // proc SetLastScope

		private void SetFirstToken(NeoLuaToken t)
		{
			if (firstToken == null)
				firstToken = t;
			if (parent != null)
				parent.SetFirstToken(t);
		} // proc SetLastToken

		private void SetLastToken(NeoLuaToken t)
		{
			lastToken = t;
			if (parent != null)
				parent.SetLastToken(t);
		} // proc SetLastToken
		
		public NeoLuaScope FirstScope { get { return firstScope; } }
		public NeoLuaScope LastScope { get { return lastScope; } }
		public NeoLuaScope NextScope { get { return nextScope; } }
		public NeoLuaScope ParentScope { get { return parent; } }

		public NeoLuaToken FirstToken { get { return firstToken; } }
		public NeoLuaToken LastToken { get { return lastToken; } }

		public virtual string FirstLine { get { return null; } }
		public virtual bool CanHiddenRegion { get { return firstToken != null && lastToken != null &&  lastToken.EndLine - firstToken.StartLine >= 2; } }
	} // class NeoLuaBlock

	#endregion

	#region -- class NeoLuaBlockScope ---------------------------------------------------

	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public class NeoLuaBlockScope : NeoLuaScope
	{
		public override string FirstLine
		{
			get
			{
				if (FirstToken != null)
				{
					switch (FirstToken.Token)
					{
						case LuaToken.KwDo:
							return "do ...";
						case LuaToken.KwRepeat:
							return "repeat ...";
						case LuaToken.KwWhile:
							return "while ...";
						case LuaToken.KwFor:
							return "for ...";
						case LuaToken.KwForEach:
							return "foreach ...";
					}
				}
				return base.FirstLine;
			}
		}

		public static bool CheckInLoopScope(NeoLuaScope current, NeoLuaScope test)
		{
			while (current != null && !(current is NeoLuaBlockScope))
				current = current.ParentScope;

			return current == test;
		} // func CheckInLoopScope
} // class NeoLuaBlockScope

	#endregion

	#region -- class NeoLuaTableConstructorScope ----------------------------------------

	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public class NeoLuaTableConstructorScope : NeoLuaScope
	{
		public override string FirstLine { get { return "{ ... }"; } }
	} // class NeoLuaTableConstructorScope

	#endregion

	#region -- class NeoLuaIfScope ------------------------------------------------------

	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public class NeoLuaIfScope : NeoLuaScope
	{
		public override string FirstLine { get { return "if ..."; } }
	} // class NeoLuaIfScope

	#endregion

	#region -- class NeoLuaTypeScope ----------------------------------------------------

	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public class NeoLuaTypeScope : NeoLuaScope
	{
		public string TypeName
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				NeoLuaToken c = FirstToken;
				while (c != null && c.Parent == this)
				{
					switch (c.Token)
					{
						case LuaToken.Identifier:
							sb.Append(c.Value);
							break;
						case LuaToken.Dot:
							sb.Append('.');
							break;
						case LuaToken.BracketSquareOpen:
							sb.Append('[');
							break;
						case LuaToken.BracketSquareClose:
							sb.Append(']');
							break;
					}
					c = c.Next;
				}
				return sb.ToString();
			}
		} // prop TypeName

		public override string FirstLine { get { return "..."; } }
		public override bool CanHiddenRegion { get { return false; } }
	} // class NeoLuaTypeScope

	#endregion

	#region -- class NeoLuaPrefixScope --------------------------------------------------

	public class NeoLuaPrefixScope : NeoLuaScope
	{
	} // class NeoLuaPrefixScope

	#endregion

	#region -- class NeoLuaFunctionScope ------------------------------------------------

	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public class NeoLuaFunctionScope : NeoLuaBlockScope
	{
		public override string FirstLine { get { return "..."; } }
	} // class NeoLuaFunctionScope

	#endregion

	#region -- class NeoLuaChunk --------------------------------------------------------

	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public sealed class NeoLuaChunk : NeoLuaBlockScope
	{
		// -- Static --------------------------------------------------------------

		#region -- Parse Chunk, Block -----------------------------------------------------

		public static NeoLuaChunk Parse(LuaLexer code)
		{
			var chunk = new NeoLuaChunk();

			// Get the first token
			code.SkipComments = false;
			if (code.Current == null)
				code.Next();
			if (code.Current.Typ == LuaToken.Comment)
				EatToken(chunk, code);

			// Parse the statements
			while (code.Current.Typ != LuaToken.Eof)
			{
				if (ParseBlock(chunk, code))
				{
					if (code.Current.Typ == LuaToken.Eof)
						return chunk;
					else
						ParseError(chunk, code, "Eof excpected");
				}
			}
			return chunk;
		} // func ParseChunk

		private static bool ParseBlock(NeoLuaScope scope, LuaLexer code)
		{
			while (true)
			{
				switch (code.Current.Typ)
				{
					case LuaToken.Eof: // End of file
						return true;

					case LuaToken.KwReturn: //  The return-statement is only allowed on the end of a scope
						EatToken(scope, code); // eat return
						return ParseReturn(scope, code);

					case LuaToken.KwBreak: // The break-statement is only allowed on the end of a scope
						EatToken(scope, code);
						FetchTokenOptional(scope, LuaToken.Semicolon, code);
						return true;

					case LuaToken.Semicolon: // End of statement => ignore
						EatToken(scope, code);
						break;

					default:
						if (!ParseStatement(scope, code)) // Parse normal statements
							return true; // block finish
						break;
				}
			}
		} // func ParseBlock

		private static bool ParseReturn(NeoLuaScope scope, LuaLexer code)
		{
			if (IsExpressionStart(code) && !ParseExpressionList(scope, code))
				return false;

			FetchTokenOptional(scope, LuaToken.Semicolon, code);
			return true;
		} // func ParseReturn

		private static bool IsExpressionStart(LuaLexer code)
		{
			return code.Current.Typ == LuaToken.BracketOpen ||
				code.Current.Typ == LuaToken.Identifier ||
				code.Current.Typ == LuaToken.DotDotDot ||
				code.Current.Typ == LuaToken.String ||
				code.Current.Typ == LuaToken.Number ||
				code.Current.Typ == LuaToken.KwTrue ||
				code.Current.Typ == LuaToken.KwFalse ||
				code.Current.Typ == LuaToken.KwNil ||
				code.Current.Typ == LuaToken.BracketCurlyOpen ||
				code.Current.Typ == LuaToken.Minus ||
				code.Current.Typ == LuaToken.Dilde ||
				code.Current.Typ == LuaToken.Cross ||
				code.Current.Typ == LuaToken.KwNot ||
				code.Current.Typ == LuaToken.KwFunction ||
				code.Current.Typ == LuaToken.KwCast;
		} // func IsExpressionStart

		#endregion

		#region -- Parse Statement --------------------------------------------------------

		private static bool ParseStatement(NeoLuaScope scope, LuaLexer code)
		{
			switch (code.Current.Typ)
			{
				case LuaToken.Identifier: // Expression
				case LuaToken.DotDotDot:
				case LuaToken.BracketOpen:
				case LuaToken.String:
				case LuaToken.Number:
				case LuaToken.KwFalse:
				case LuaToken.KwTrue:
				case LuaToken.KwNil:
				case LuaToken.BracketCurlyOpen:
				case LuaToken.Minus:
				case LuaToken.KwCast:
					return ParseExpressionStatement(scope, code, false);

				case LuaToken.ColonColon: // Start of a label
					return ParseLabel(scope, code);

				case LuaToken.KwGoto:
					return ParseGoto(scope, code);

				case LuaToken.KwDo:
					return ParseDoLoop(scope, code);

				case LuaToken.KwWhile:
					return ParseWhileLoop(scope, code);

				case LuaToken.KwRepeat:
					return ParseRepeatLoop(scope, code);

				case LuaToken.KwIf:
					return ParseIfStatement(scope, code);

				case LuaToken.KwFor:
					return ParseForLoop(scope, code);

				case LuaToken.KwForEach:
					return ParseForEachLoop(scope, code);

				case LuaToken.KwFunction:
					return ParseFunction(scope, code, false);

				case LuaToken.KwLocal:
						EatToken(scope, code);
						return code.Current.Typ == LuaToken.KwFunction ?
							ParseFunction(scope, code, true) :
							ParseExpressionStatement(scope, code, true);
					
				case LuaToken.KwConst:
					return ParseConst(scope, code);

				case LuaToken.InvalidString:
					return ParseError(scope, code, "NewLine in string constant.");
				case LuaToken.InvalidComment:
					return ParseError(scope, code, "Comment not closed.");
				case LuaToken.InvalidChar:
					return ParseError(scope, code, "Unexpected char.");

				default:
					return false;
			}
		}  // func ParseStatement

		private static bool ParseExpressionStatement(NeoLuaScope scope, LuaLexer code, bool lLocal)
		{
			// parse the assgiee list (var0, var1, var2, ...)
			while (true)
			{
				if (lLocal) // parse local variables
				{
					if (!ParseIdentifierAndType(scope, code))
						return false;
					// if (exprVar.Type != typeVar)
					//	 throw ParseError(tVar, Properties.Resources.rsParseTypeRedef);
				}
				else // parse a assignee
				{
					// parse as a prefix
					if (!ParsePrefix(scope, code))
						return false;
				}

				// is there another prefix
				if (code.Current.Typ == LuaToken.Comma)
					EatToken(scope, code);
				else
					break;
			}

			// Optional assign
			if (code.Current.Typ == LuaToken.Assign)
			{
				EatToken(scope, code);
				return ParseExpressionList(scope, code);
			}
			else
				return true;
		} // proc ParseExpressionStatement

		private static bool ParseIfStatement(NeoLuaScope scope, LuaLexer code)
		{
			scope = scope.AddScope<NeoLuaIfScope>();
			// if expr then block { elseif expr then block } [ else block ] end
			if (!FetchTokenEat(scope, LuaToken.KwIf, code))
				return false;
			if (!ParseExpression(scope, code))
				return false;
			if (!FetchTokenEat(scope, LuaToken.KwThen, code))
				return false;

			if (!ParseIfElseBlock(scope, code))
				return false;

			return ParseElseStatement(scope, code);
		} // proc ParseIfStatement

		private static bool ParseElseStatement(NeoLuaScope scope, LuaLexer code)
		{
			switch (code.Current.Typ)
			{
				case LuaToken.KwElseif:
					EatToken(scope, code);
					if (!ParseExpression(scope, code))
						return false;

					if (!FetchTokenEat(scope, LuaToken.KwThen, code))
						return false;

					if (!ParseIfElseBlock(scope, code))
						return false;

					return ParseElseStatement(scope, code);

				case LuaToken.KwElse:

					EatToken(scope, code);
					if (!ParseIfElseBlock(scope, code))
						return false;
					return FetchTokenEat(scope, LuaToken.KwEnd, code);

				case LuaToken.KwEnd:
					EatToken(scope, code);
					return true;

				default:
					return ParseError(scope, code, "else or end is missing.");
			}
		} // func ParseElseStatement

		private static bool ParseIfElseBlock(NeoLuaScope parent, LuaLexer code)
			=> ParseBlock(parent, code);

		private static bool ParseConst(NeoLuaScope scope, LuaLexer code)
		{
			// const ::= variable '=' ( expr | clr '.' Type )
			EatToken(scope, code);

			if (!ParseIdentifierAndType(scope, code))
				return false;

			if (code.Current.Typ == LuaToken.Identifier || code.Current.Value == "typeof")
			{
				EatToken(scope, code);
				return ParseType(scope, code);
			}
			else
			{
				if (!FetchTokenEat(scope, LuaToken.Assign, code))
					return false;

				return ParseExpression(scope, code);
			}
		} // func ParseConst

		#endregion

		#region -- Parse Prefix, Suffix ---------------------------------------------------

		private static bool ParsePrefix(NeoLuaScope scope, LuaLexer code)
		{
			scope = scope.AddScope<NeoLuaPrefixScope>();

			// prefix ::= Identifier suffix_opt |  '(' exp ')' suffix | literal | tablector
			switch (code.Current.Typ)
			{
				case LuaToken.BracketOpen: // Parse eine Expression
					EatToken(scope, code);
					if (!ParseExpression(scope, code))
						return false;
					if (!FetchTokenEat(scope, LuaToken.BracketClose, code))
						return false;
					break;

				case LuaToken.DotDotDot:
				case LuaToken.Identifier:
				case LuaToken.KwForEach:
				case LuaToken.String: // Literal String
				case LuaToken.Number: // Literal Zahl
				case LuaToken.KwTrue: // Literal TRUE
				case LuaToken.KwFalse: // Literal FALSE
				case LuaToken.KwNil: // Literal NIL
					EatToken(scope, code);
					break;

				case LuaToken.KwCast:
					if (!ParsePrefixCast(scope, code))
						return false;
					break;

				case LuaToken.BracketCurlyOpen: // tablector
					if (!ParseTableConstructor(scope, code))
						return false;
					break;

				case LuaToken.KwFunction: // Function definition
					EatToken(scope, code);
					if (!ParseLamdaDefinition(scope, code, false))
						return false;
					break;

				default:
					return ParseError(scope, code, "prefix token expected.");
			}

			return ParseSuffix(scope, code);
		} // func ParsePrefix

		private static bool ParseSuffix(NeoLuaScope scope, LuaLexer code)
		{
			// suffix_opt ::= [ suffix ]
			// suffix ::= { '[' exp ']'  | '.' Identifier | args | ':' Identifier args }
			// args ::= tablector | string | '(' explist ')'

			while (true)
			{
				switch (code.Current.Typ)
				{
					case LuaToken.BracketSquareOpen: // Index
						EatToken(scope, code);
						if (code.Current.Typ != LuaToken.BracketSquareClose)
						{
							if (!ParseExpressionList(scope, code))
								return false;
						}
						if (!FetchTokenEat(scope, LuaToken.BracketSquareClose, code))
							return false;
						break;

					case LuaToken.Dot: // Property of an class
						EatToken(scope, code);
						if (!FetchTokenEat(scope, LuaToken.Identifier, code))
							return false;
						break;

					case LuaToken.BracketOpen: // List of arguments
						if (!ParseArgumentList(scope, code))
							return false;
						break;

					case LuaToken.BracketCurlyOpen: // LuaTable as an argument
						if (!ParseTableConstructor(scope, code))
							return false;
						break;

					case LuaToken.String: // String as an argument
						EatToken(scope, code);
						break;

					case LuaToken.Colon: // Methodenaufruf
						EatToken(scope, code);

						// Lese den Namen um den Member zu belegen
						if (!FetchTokenEat(scope, LuaToken.Identifier, code))
							return false;

						// Parse die Parameter
						switch (code.Current.Typ)
						{
							case LuaToken.BracketOpen: // Argumentenliste
								if (!ParseArgumentList(scope, code))
									return false;
								break;

							case LuaToken.BracketCurlyOpen: // LuaTable als Argument
								if (!ParseTableConstructor(scope, code))
									return false;
								break;

							case LuaToken.String: // String als Argument
								if (!FetchTokenEat(scope, LuaToken.String, code))
									return false;
								break;
						}
						break;

					default:
						return true;
				}
			}
		} // func ParsePrefix

		private static bool ParseArgumentList(NeoLuaScope scope, LuaLexer code)
		{
			if (!FetchTokenEat(scope, LuaToken.BracketOpen, code))
				return false;

			// exprArgumentList := '(' [ exprArg { , exprArg } ] ')'
			while (code.Current.Typ != LuaToken.BracketClose)
			{
				if (code.LookAhead.Typ == LuaToken.Assign) // named argument
				{
					if (!FetchTokenEat(scope, LuaToken.Identifier, code))
						return false;

					EatToken(scope, code);
				}

				// parse the expression
				if (!ParseExpression(scope, code))
					return ParseError(scope, code, "Expression expected.");

				// optinal comma
				if (code.Current.Typ != LuaToken.BracketClose)
				{
					if (!FetchTokenEat(scope, LuaToken.Comma, code))
						return false;
				}
			}

			EatToken(scope, code);
			return true;
		} // func ParseArgumentList

		#endregion

		#region -- Parse Expressions ------------------------------------------------------

		private static bool ParseExpressionList(NeoLuaScope scope, LuaLexer code)
		{
			while (true)
			{
				if (!ParseExpression(scope, code))
					return false;

				// Noch eine Expression
				if (code.Current.Typ == LuaToken.Comma)
					EatToken(scope, code);
				else
					break;
			}
			return true;
		} // func ParseExpressionList

		private static bool IsOperator(LuaToken typ)
		{
			switch (typ)
			{
				case LuaToken.KwOr:
				case LuaToken.BitOr:
				case LuaToken.Dilde:

				case LuaToken.KwAnd:
				case LuaToken.BitAnd:

				case LuaToken.Lower:
				case LuaToken.Greater:
				case LuaToken.LowerEqual:
				case LuaToken.GreaterEqual:
				case LuaToken.NotEqual:
				case LuaToken.Equal:

				case LuaToken.DotDot:
				case LuaToken.ShiftLeft:
				case LuaToken.ShiftRight:

				case LuaToken.Plus:
				case LuaToken.Minus:
				case LuaToken.Star:
				case LuaToken.Slash:
				case LuaToken.SlashShlash:
				case LuaToken.Percent:

					return true;
				default:
					return false;
			}
		} // func IsOperator

		private static bool ParseExpression(NeoLuaScope scope, LuaLexer code)
		{
			if (!ParseExpressionUnary(scope, code))
				return false;

			while (IsOperator(code.Current.Typ))
			{
				EatToken(scope, code);
				if (!ParseExpressionUnary(scope, code))
					return false;
			}

			return true;
		} // func ParseExpression

		private static bool ParseExpressionUnary(NeoLuaScope scope, LuaLexer code)
		{
			// expUn ::= { 'not' | - | # | ~ } expPow
			var typ = code.Current.Typ;
			if (typ == LuaToken.KwNot ||
					typ == LuaToken.Minus ||
					typ == LuaToken.Dilde ||
					typ == LuaToken.Cross)
			{
				EatToken(scope, code);
				return ParseExpressionUnary(scope, code);
			}
			else
				return ParseExpressionPower(scope, code);
		} // func ParseExpressionUnary

		private static bool ParseExpressionPower(NeoLuaScope scope, LuaLexer code)
		{
			if (!ParseExpressionCast(scope, code))
				return false;

			if (code.Current.Typ == LuaToken.Caret)
			{
				EatToken(scope, code);
				return ParseExpressionPower(scope, code);
			}
			else
				return true;
		} // func ParseExpressionPower

		private static bool ParseExpressionCast(NeoLuaScope scope, LuaLexer code)
		{
			if (code.Current.Typ == LuaToken.KwCast)
			{
				if (!ParsePrefixCast(scope, code))
					return false;
				return ParseSuffix(scope, code);
			}
			else
				return ParsePrefix(scope, code);
		} // func ParseExpressionCast

		private static bool ParsePrefixCast(NeoLuaScope scope, LuaLexer code)
		{
			EatToken(scope, code); ;
			if (!FetchTokenEat(scope, LuaToken.BracketOpen, code))
				return false;

			// Read the type
			if (!ParseType(scope, code))
				return false;

			if (!FetchTokenEat(scope, LuaToken.Comma, code))
				return false;

			if (!ParseExpression(scope, code))
				return false;

			return FetchTokenEat(scope, LuaToken.BracketClose, code);
		} // func ParsePrefixCast

		private static bool ParseIdentifierAndType(NeoLuaScope scope, LuaLexer code)
		{
			// var ::= name ':' type
			if (!FetchTokenEat(scope, LuaToken.Identifier, code))
				return false;

			if (code.Current.Typ == LuaToken.Colon)
			{
				EatToken(scope, code);
				if (!ParseType(scope, code))
					return false;
			}
			return true;
		} // func ParseIdentifierAndType

		private static bool ParseType(NeoLuaScope scope, LuaLexer code)
		{
			if (!(scope is NeoLuaTypeScope))
				scope = scope.AddScope<NeoLuaTypeScope>();
			// is the first token an alias
			FetchTokenEat(scope, LuaToken.Identifier, code);

			while (code.Current.Typ == LuaToken.Dot ||
						code.Current.Typ == LuaToken.Plus ||
						code.Current.Typ == LuaToken.BracketSquareOpen)
			{
				if (code.Current.Typ == LuaToken.BracketSquareOpen)
				{
					EatToken(scope, code);
					if (code.Current.Typ != LuaToken.BracketSquareClose)
					{
						if (!ParseType(scope, code))
							return false;

						while (code.Current.Typ == LuaToken.Comma)
						{
							EatToken(scope, code);
							if (!ParseType(scope, code))
								return false;
						}
					}
					if (!FetchTokenEat(scope, LuaToken.BracketSquareClose, code))
						return false;
				}
				else
				{
					EatToken(scope, code);
					if (!FetchTokenEat(scope, LuaToken.Identifier, code))
						return false;
				}
			}

			return true;
		} // func ParseType

		#endregion

		#region -- Parse Goto, Label ------------------------------------------------------

		private static bool ParseGoto(NeoLuaScope scope, LuaLexer code)
		{
			// goto Identifier
			return
				FetchTokenEat(scope, LuaToken.KwGoto, code) &&
				FetchTokenEat(scope, LuaToken.Identifier, code);
		} // proc ParseGoto

		private static bool ParseLabel(NeoLuaScope scope, LuaLexer code)
		{
			// ::identifier::
			return
				FetchTokenEat(scope, LuaToken.ColonColon, code) &&
				FetchTokenEat(scope, LuaToken.Identifier, code) &&
				FetchTokenEat(scope, LuaToken.ColonColon, code);
		} // proc ParseLabel

		#endregion

		#region -- Parse Loops ------------------------------------------------------------

		private static bool ParseDoLoop(NeoLuaScope parent, LuaLexer code)
		{
			// doloop ::= do '(' name { ',' name } = expr { ',' expr }  ')' block end

			var scope = parent.AddScope<NeoLuaBlockScope>();

			// fetch do
			if (!FetchTokenEat(scope, LuaToken.KwDo, code))
				return false;

			if (code.Current.Typ == LuaToken.BracketOpen) // look for disposable variables
			{
				EatToken(scope, code);
				if (!ParseExpressionStatement(scope, code, true))
					return false;

				if (!FetchTokenEat(scope, LuaToken.BracketClose, code))
					return false;
			}

			// parse the block
			if (!ParseBlock(scope, code))
				return false;
			if (!FetchTokenEat(scope, LuaToken.KwEnd, code))
				return false;

			if (FetchTokenOptional(scope, LuaToken.BracketOpen, code) == null)
				return true;

			while (FetchTokenOptional(scope, LuaToken.BracketClose, code) == null)
			{
				if (!FetchTokenEat(scope, LuaToken.KwFunction, code))
					return false;

				if (FetchTokenOptional(scope, LuaToken.BracketOpen, code) != null)
				{
					if (!FetchTokenEat(scope, LuaToken.Identifier, code))
						return false;

					if (FetchTokenOptional(scope, LuaToken.Colon, code) != null)
					{
						if (!ParseType(scope, code))
							return false;
					}

					if (!FetchTokenEat(scope, LuaToken.BracketClose, code))
						return false;
				}

				if (!ParseBlock(scope, code))
					return false;

				if (!FetchTokenEat(scope, LuaToken.KwEnd, code))
					return false;

				FetchTokenOptional(scope, LuaToken.Comma, code);
			}

			return true;
		} // ParseDoLoop

		private static bool ParseWhileLoop(NeoLuaScope parent, LuaLexer code)
		{
			var scope = parent.AddScope<NeoLuaBlockScope>();
			return
				// get the expression
				FetchTokenEat(scope, LuaToken.KwWhile, code) &&
				ParseExpression(scope, code) &&

				// append the block
				FetchTokenEat(scope, LuaToken.KwDo, code) &&
				ParseBlock(scope, code) &&
				FetchTokenEat(scope, LuaToken.KwEnd, code);
		} // func ParseWhileLoop

		private static bool ParseRepeatLoop(NeoLuaScope parent, LuaLexer code)
		{
			var scope = parent.AddScope<NeoLuaBlockScope>();
			return
				// loop content
				FetchTokenEat(scope, LuaToken.KwRepeat, code) &&
				ParseBlock(scope, code) &&

				// get the loop expression
				FetchTokenEat(scope, LuaToken.KwUntil, code) &&
				ParseExpression(scope, code);
		} // func ParseRepeatLoop

		private static bool ParseForLoop(NeoLuaScope parent, LuaLexer code)
		{
			NeoLuaScope scope = parent.AddScope<NeoLuaBlockScope>();
			
			// for name
			if (!FetchTokenEat(scope, LuaToken.KwFor, code))
				return false;
			if (!ParseIdentifierAndType(scope, code))
				return false;

			if (code.Current.Typ == LuaToken.Assign)
			{
				// = exp, exp [, exp] do block end
				if (!FetchTokenEat(scope, LuaToken.Assign, code))
					return false;
				if (!ParseExpression(scope, code))
					return false;
				if (!FetchTokenEat(scope, LuaToken.Comma, code))
					return false;
				if (!ParseExpression(scope, code))
					return false;
				if (code.Current.Typ == LuaToken.Comma)
				{
					EatToken(scope, code);
					if (!ParseExpression(scope, code))
						return false;
				}

				return
					FetchTokenEat(scope, LuaToken.KwDo, code) &&
					ParseBlock(scope, code) &&
					FetchTokenEat(scope, LuaToken.KwEnd, code);
			}
			else
			{
				// {, name} in explist do block end

				// fetch all loop variables
				while (code.Current.Typ == LuaToken.Comma)
				{
					EatToken(scope, code);
					if (!ParseIdentifierAndType(scope, code))
						return false;
				}

				// get the loop expressions
				return
					FetchTokenEat(scope, LuaToken.KwIn, code) &&
					ParseExpressionList(scope, code) &&

					// parse the loop body
					FetchTokenEat(scope, LuaToken.KwDo, code) &&
					ParseBlock(scope, code) &&
					FetchTokenEat(scope, LuaToken.KwEnd, code);
			}
		} // func ParseForLoop

		private static bool ParseForEachLoop(NeoLuaScope parent, LuaLexer code)
		{
			NeoLuaScope scope = parent.AddScope<NeoLuaBlockScope>();
			
			// foreach name in exp do block end;
			EatToken(scope, code); // foreach

			return
				ParseIdentifierAndType(scope, code) &&
				FetchTokenEat(scope, LuaToken.KwIn, code) &&
				ParseExpression(scope, code) &&
				FetchTokenEat(scope, LuaToken.KwDo, code) &&
				ParseBlock(scope, code) &&
				FetchTokenEat(scope, LuaToken.KwEnd, code);
		} // proc ParseForEachLoop

		#endregion

		#region -- Parse Function, Lambda -------------------------------------------------

		private static bool ParseFunction(NeoLuaScope scope, LuaLexer code, bool lLocal)
		{
			if (!FetchTokenEat(scope, LuaToken.KwFunction, code))
				return false;

			if (lLocal) // Local function, only one identifier is allowed
			{
				return FetchTokenEat(scope, LuaToken.Identifier, code) &&
					ParseLamdaDefinition(scope, code, false);
			}
			else // Function that is assigned to a table. A chain of identifiers is allowed.
			{
				if (!FetchTokenEat(scope, LuaToken.Identifier, code))
					return false;

				// Collect the chain of members
				while (code.Current.Typ == LuaToken.Dot)
				{
					EatToken(scope, code);
					if (!FetchTokenEat(scope, LuaToken.Identifier, code))
						return false;
				}
				// add a method to the table. methods get a hidden parameter and will bo marked
				bool lMethodMember;
				if (code.Current.Typ == LuaToken.Colon)
				{
					EatToken(scope, code);
					if (!FetchTokenEat(scope, LuaToken.Identifier, code))
						return false;
					lMethodMember = true;
				}
				else
				{
					lMethodMember = false;
				}

				// generate lambda
				return ParseLamdaDefinition(scope, code, lMethodMember);
			}
		} // proc ParseLamdaDefinition

		private static bool ParseLamdaDefinition(NeoLuaScope parent, LuaLexer code, bool lSelfParameter)
		{
			NeoLuaScope scope = parent.AddScope<NeoLuaFunctionScope>();

			// Lese die Parameterliste ein
			if (!FetchTokenEat(scope, LuaToken.BracketOpen, code))
				return false;

			//if (lSelfParameter)
			//	parameters.Add(scope.RegisterParameter(typeof(object), "self"));

			if (code.Current.Typ == LuaToken.Identifier || code.Current.Typ == LuaToken.DotDotDot)
			{
				if (code.Current.Typ == LuaToken.DotDotDot)
					EatToken(scope, code);
				else
				{
					if (!ParseIdentifierAndType(scope, code))
						return false;

					while (code.Current.Typ == LuaToken.Comma)
					{
						EatToken(scope, code);
						if (code.Current.Typ == LuaToken.DotDotDot)
						{
							EatToken(scope, code);
							break;
						}
						else
						{
							if (!ParseIdentifierAndType(scope, code))
								return false;
						}
					}
				}
			}
			if (!FetchTokenEat(scope, LuaToken.BracketClose, code))
				return false;

			// Is there a specific result 
			if (code.Current.Typ == LuaToken.Colon)
			{
				EatToken(scope, code);
				if (!ParseType(scope, code))
					return false;
			}

			// Lese den Code-Block
			return
				ParseBlock(scope, code) &&
				FetchTokenEat(scope, LuaToken.KwEnd, code);
		} // proc ParseLamdaDefinition

		#endregion

		#region -- Parse TableConstructor -------------------------------------------------

		private static bool ParseTableConstructor(NeoLuaScope scope, LuaLexer code)
		{
			// table ::= '{' [field] { fieldsep field } [fieldsep] '}'
			// fieldsep ::= ',' | ';'
			scope = scope.AddScope<NeoLuaTableConstructorScope>();
			
			if (!FetchTokenEat(scope, LuaToken.BracketCurlyOpen, code))
				return false;

			if (code.Current.Typ == LuaToken.BracketCurlyClose)
			{
				EatToken(scope, code);
				return true;
			}
			else
			{
				NeoLuaScope scopeTable = scope;

				// fiest field
				if (!ParseTableField(scopeTable, code))
					return false;

				// collect more table fields
				while (code.Current.Typ == LuaToken.Comma || code.Current.Typ == LuaToken.Semicolon)
				{
					EatToken(scope, code);

					// Optional last separator
					if (code.Current.Typ == LuaToken.BracketCurlyClose)
						break;

					// Parse the field
					if (!ParseTableField(scopeTable, code))
						return false;
				}

				// Closing bracket
				return FetchTokenEat(scope, LuaToken.BracketCurlyClose, code);
			}
		} // func ParseTableConstructor

		private static bool ParseTableField(NeoLuaScope scope, LuaLexer code)
		{
			// field ::= '[' exp ']' '=' exp | Name '=' exp | exp
			if (code.Current.Typ == LuaToken.BracketSquareOpen)
			{
				// Parse the index
				EatToken(scope, code);

				return
					ParseExpression(scope, code) &&
					FetchTokenEat(scope, LuaToken.BracketSquareClose, code) &&
					FetchTokenEat(scope, LuaToken.Assign, code) &&
					ParseExpression(scope, code);
			}
			else if (code.Current.Typ == LuaToken.Identifier && code.LookAhead.Typ == LuaToken.Assign)
			{
				EatToken(scope, code);
				EatToken(scope, code);

				// Expression
				return ParseExpression(scope, code);
			}
			else
			{
				return ParseExpression(scope, code);
			}
		} // proc ParseTableField

		#endregion

		#region -- EatToken, FetchToken, ParseError ---------------------------------------

		private static NeoLuaToken EatToken(NeoLuaScope scope, LuaLexer code)
		{
			scope.AddToken(code.Current);
			code.Next();
			while (code.Current.Typ == LuaToken.Comment)
			{
				if (scope.LastToken.Token == LuaToken.Comment && // is a comment before
					 (scope.LastToken.Prev == null || (scope.LastToken.StartLine != scope.LastToken.Prev.EndLine)) && // is a single token on this line
					 (scope.LastToken.EndLine == code.Current.Start.Line - 2)
					)
					scope.LastToken.Combine(code.Current);
				else
					scope.AddToken(code.Current);
				code.Next();
			}
			return scope.LastToken;
		} // proc EatToken

		private static NeoLuaToken FetchTokenOptional(NeoLuaScope scope, LuaToken typ, LuaLexer code)
		{
			if (code.Current.Typ == typ)
				return EatToken(scope, code);
			else
				return null;
		} // func FetchTokenOptional

		private static bool FetchTokenEat(NeoLuaScope scope, LuaToken typ, LuaLexer code)
		{
			if (code.Current.Typ == typ)
			{
				EatToken(scope, code);
				return true;
			}
			else
				return FetchTokenError(scope, code, typ);
		} // func FetchTokenEat

		private static bool FetchTokenEat(NeoLuaScope scope, LuaToken typ, LuaLexer code, out NeoLuaToken token)
		{
			if (code.Current.Typ == typ)
			{
				token = EatToken(scope, code);
				return true;
			}
			else
			{
				token = null;
				return FetchTokenError(scope, code, typ);
			}
		} // func FetchTokenEat

		private static bool FetchTokenError(NeoLuaScope scope, LuaLexer code, LuaToken typ)
		{
			return ParseError(scope, code, String.Format("Unexpected token '{0}'. '{1}' expected.", LuaLexer.GetTokenName(code.Current.Typ), LuaLexer.GetTokenName(typ)));
		} // proc FetchToken

		private static bool ParseError(NeoLuaScope scope, LuaLexer code, string sMessage)
		{
			// eat the token with error info
			int iLine = code.Current.End.Line;
			EatToken(scope, code);

			scope.LastToken.SetError(Severity.Error, sMessage);

			// skip tokens
			while (code.Current.End.Line == iLine && code.Current.Typ != LuaToken.Eof)
				EatToken(scope, code);

			return false;
		} // func ParseError

		#endregion
	} // class NeoLuaChunk

	#endregion
}
