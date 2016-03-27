using System;
using System.Reflection;

namespace Rex.Utilities.Helpers
{
    public class NameSpaceInfo : IComparable<NameSpaceInfo>
    {
        public bool Folded { get; set; }
        public int IndetLevel { get; internal set; }
        public bool AtMaxIndent { get; internal set; }
        public string Name { get; internal set; }
        public Assembly Assembly { get; internal set; }
        public bool Selected { get; set; }

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
