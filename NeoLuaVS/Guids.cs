using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1017:MarkAssembliesWithComVisible")]


namespace Neo.IronLua
{
	static class GuidList
	{
		public const string guidNeoLuaVSPkgString = "ba4fdff8-63ba-45c3-bc2e-0bac596d855e";
		public const string guidNeoLuaVSCmdSetString = "710ebfc2-dd1a-45a9-8c1e-70cd03d40824";

		public const string guidLanguageService = "fab3d5c9-20b8-488d-ab4f-62ef131b498f";

		public static readonly Guid guidNeoLuaVSCmdSet = new Guid(guidNeoLuaVSCmdSetString);
	};
}