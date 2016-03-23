using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rex.Utilities.Helpers
{
	/// <summary>
	/// Class used for syntax highlighting on the 
	/// </summary>
	public class MemberDetails : IEnumerable<Syntax>, IComparable<MemberDetails>
	{
		private readonly IEnumerable<Syntax> details;

		public object Value { get; }
		public Syntax Name =>
			details.LastOrDefault(i => i.Type == SyntaxType.Name) ??
			details.FirstOrDefault(i => i.Type == SyntaxType.Type) ??
			details.FirstOrDefault(i => i.Type == SyntaxType.Keyword && Utils.MapToKeyWords.Values.Contains(i.String));

		public Syntax Constant => details.FirstOrDefault(i => i.Type == SyntaxType.ConstVal);

		public MemberDetails(object value, params Syntax[] syntax)
		{
			Value = value;
			details = syntax;
		}
		public MemberDetails(object value, IEnumerable<Syntax> syntax)
		{
			Value = value;
			details = syntax;
		}

		public IEnumerator<Syntax> GetEnumerator() => details.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => details.GetEnumerator();

		public override string ToString() => details.Aggregate("", (a, j) => a + " " + j).Trim();

		public int CompareTo(MemberDetails other)
		{
			return ToString().CompareTo(other.ToString());
		}

		public bool IsEquivelent(MemberDetails other)
		{
			var mySyntax = details.ToList();
			var otherSyntax = other.details.ToList();

			if (mySyntax.Count != otherSyntax.Count) return false;

			for (int i = 0; i < mySyntax.Count; i++)
			{
				if (!mySyntax[i].IsEquivelent(otherSyntax[i]))
					return false;
			}
			return true;
		}

		public MemberDetails Merge(MemberDetails details)
		{
			return new MemberDetails(Value, this.Concat(details));
		}
	}

	public class Syntax : IComparable<Syntax>
	{
		public Syntax(string str, SyntaxType type)
		{
			String = str;
			Type = type;
		}

		public string String { get; }

		public SyntaxType Type { get; }

		public static Syntax Name(string name) => new Syntax(name, SyntaxType.Name);
		public static Syntax Keyword(string keyword) => new Syntax(keyword, SyntaxType.Keyword);
		public static Syntax NewType(string typeStr) => new Syntax(typeStr, SyntaxType.Type);
		public static Syntax ParaName(string name) => new Syntax(name, SyntaxType.ParaName);
		public static Syntax ConstVal(string v) => new Syntax(v, SyntaxType.ConstVal);

		public static readonly Syntax ReadonlyKeyword = new Syntax("readonly", SyntaxType.Keyword);
		public static readonly Syntax ConstKeyword = new Syntax("const", SyntaxType.Keyword);
		public static readonly Syntax StaticKeyword = new Syntax("static", SyntaxType.Keyword);
		public static readonly Syntax OutKeyword = new Syntax("out", SyntaxType.Keyword);
		public static readonly Syntax RefKeyword = new Syntax("ref", SyntaxType.Keyword);
		public static readonly Syntax GetKeyword = new Syntax("get", SyntaxType.Keyword);
		public static readonly Syntax SetKeyword = new Syntax("set", SyntaxType.Keyword);
		public static readonly Syntax ParaOpen = new Syntax("(", SyntaxType.ParanOpen);
		public static readonly Syntax ParaClose = new Syntax(")", SyntaxType.ParanClose);
		public static readonly Syntax CurlyOpen = new Syntax("{", SyntaxType.CurlyOpen);
		public static readonly Syntax CurlyClose = new Syntax("}", SyntaxType.CurlyClose);
		public static readonly Syntax Empty = new Syntax(string.Empty, SyntaxType._Default);
		public static readonly Syntax Dot = new Syntax(".", SyntaxType.Dot);
		public static readonly Syntax Comma = new Syntax(",", SyntaxType.Comma);
		public static readonly Syntax GenericParaOpen = new Syntax("<", SyntaxType.GenericParaOpen);
		public static readonly Syntax GenericParaClose = new Syntax(">", SyntaxType.GenericParaClose);
		public static readonly Syntax Semicolon = new Syntax(";", SyntaxType.Semicolon);
		public static readonly Syntax EqualsOp = new Syntax("=", SyntaxType.EqualsOp);

		internal bool IsEquivelent(Syntax syntax) => Type == syntax.Type && String.Equals(syntax.String);
		public override string ToString() => String;

		public int CompareTo(Syntax other)
		{
			if (other == null)
				return 1;

			return string.Compare(String, other.String);
		}
	}


	public enum SyntaxType
	{
		_Default,
		Keyword,
		Name,
		ParaName,
		Type,
		ParanOpen,
		ParanClose,
		CurlyOpen,
		CurlyClose,
		ConstVal,
		Dot,
		Comma,
		GenericParaOpen,
		GenericParaClose,
		Semicolon,
		EqualsOp,
	}
}
