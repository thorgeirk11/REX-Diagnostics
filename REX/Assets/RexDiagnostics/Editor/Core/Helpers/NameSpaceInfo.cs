using System;
using System.Reflection;

namespace Rex.Utilities.Helpers
{
	[Serializable]
	public class NameSpaceInfo : IComparable<NameSpaceInfo>
	{
		/// <summary>
		/// Is this namespace collapsed or expanded in scope selection in UI?
		/// </summary>
		public bool DisplaySubNamespaces;
		/// <summary>
		/// At which depth is this namespace. e.g System.Collections is at depth 2 since it's a sub namespace of System
		/// </summary>
		public int Depth;
		/// <summary>
		/// Dose this NameSpace have sub namespaces. e.g. "System" is an example of a namespace with sub namespaces.
		/// </summary>
		public bool HasSubNamespaces;
		/// <summary>
		/// Name of the NameSpace.
		/// </summary>
		public string Name;
		/// <summary>
		/// Is the namespace included in the using statements of REX?
		/// </summary>
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
