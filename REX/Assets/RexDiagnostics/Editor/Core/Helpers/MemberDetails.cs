using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rex.Utilities.Helpers
{
	public enum MemberType
	{
		Field,
		Property,
		Method,
		ExtentionMethod,
	}
	public class MemberDetails : IEnumerable<Syntax>, IComparable<MemberDetails>
	{
		private readonly IEnumerable<Syntax> _details;
		private readonly object _value;

		public MemberType Type { get; set; }
		public object Value { get { return _value; } }
		public Syntax Name
		{
			get
			{
				return _details.LastOrDefault(i => i.Type == SyntaxType.Name) ??
				  _details.FirstOrDefault(i => i.Type == SyntaxType.Type) ??
				  _details.FirstOrDefault(i => i.Type == SyntaxType.Keyword && RexUtils.MapToPrimitive(i.String) != null);
			}
		}

		public Syntax Constant
		{
			get { return _details.FirstOrDefault(i => i.Type == SyntaxType.ConstVal); }
		}
		public MemberDetails(IEnumerable<Syntax> syntax)
		{
			_details = syntax;
		}

		public MemberDetails(object value, IEnumerable<Syntax> syntax)
		{
			_value = value;
			_details = syntax;
		}

		public IEnumerator<Syntax> GetEnumerator()
		{
			return _details.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _details.GetEnumerator();
		}

		public override string ToString()
		{
			return _details.Aggregate("", (a, j) => a + j).Trim();
		}

		public int CompareTo(MemberDetails other)
		{
			return ToString().CompareTo(other.ToString());
		}

		public bool IsEquivelent(MemberDetails other)
		{
			var mySyntax = _details.ToList();
			var otherSyntax = other._details.ToList();

			if (mySyntax.Count != otherSyntax.Count) return false;

			for (int i = 0; i < mySyntax.Count; i++)
			{
				if (!mySyntax[i].IsEquivelent(otherSyntax[i]))
					return false;
			}
			return true;
		}
	}

	public class Syntax : IComparable<Syntax>
	{
		public Syntax(string str, SyntaxType type)
		{
			_str = str;
			_type = type;
		}

		private readonly string _str;
		private readonly SyntaxType _type;

		public string String
		{
			get { return _str; }
		}

		public SyntaxType Type
		{
			get { return _type; }
		}

		public static Syntax Name(string name) { return new Syntax(name, SyntaxType.Name); }
		public static Syntax Keyword(string keyword) { return new Syntax(keyword, SyntaxType.Keyword); }
		public static Syntax NameSpaceForType(string nameSpace) { return new Syntax(nameSpace, SyntaxType.NameSpaceForType); }
		public static Syntax NewType(string typeStr) { return new Syntax(typeStr, SyntaxType.Type); }
		public static Syntax ParaName(string name) { return new Syntax(name, SyntaxType.ParaName); }
		public static Syntax ConstVal(string v) { return new Syntax(v, SyntaxType.ConstVal); }

		public static readonly Syntax Space = new Syntax(" ", SyntaxType.Space);
		public static readonly Syntax QuotationMark = new Syntax("\"", SyntaxType.QuotationMark);
		public static readonly Syntax SingleQuotationMark = new Syntax("'", SyntaxType.SingleQuotationMark);
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
		public static readonly Syntax BracketOpen = new Syntax("[", SyntaxType.BracketOpen);
		public static readonly Syntax BracketClose = new Syntax("]", SyntaxType.BracketClose);
		public static readonly Syntax Empty = new Syntax(string.Empty, SyntaxType._Default);
		public static readonly Syntax Dot = new Syntax(".", SyntaxType.Dot);
		public static readonly Syntax Comma = new Syntax(",", SyntaxType.Comma);
		public static readonly Syntax GenericParaOpen = new Syntax("<", SyntaxType.GenericParaOpen);
		public static readonly Syntax GenericParaClose = new Syntax(">", SyntaxType.GenericParaClose);
		public static readonly Syntax Semicolon = new Syntax(";", SyntaxType.Semicolon);
		public static readonly Syntax EqualsOp = new Syntax("=", SyntaxType.EqualsOp);


		internal bool IsEquivelent(Syntax syntax)
		{
			return _type == syntax.Type && String.Equals(syntax.String);
		}
		public override string ToString()
		{
			return String;
		}

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
		Space,
		QuotationMark,
		SingleQuotationMark,
		NameSpaceForType,
		BracketOpen,
		BracketClose,
	}
}
