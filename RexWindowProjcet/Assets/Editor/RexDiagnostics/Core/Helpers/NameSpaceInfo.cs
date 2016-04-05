using System;
using System.Reflection;

namespace Rex.Utilities.Helpers
{
	[Serializable]
	public class NameSpaceInfo : IComparable<NameSpaceInfo>
	{
		public bool Folded;
		public int IndetLevel;
		public bool AtMaxIndent;
		public string Name;
		public bool Selected;

		public override bool Equals(object obj)
		{
			return Equals(Name, (obj as NameSpaceInfo).Name);
		}
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public int CompareTo(NameSpaceInfo other)
		{
			return string.Compare(Name, other.Name, StringComparison.Ordinal);
		}
	}
}
