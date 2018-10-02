using System;

namespace Rex.Utilities
{
    public interface IOutputEntry
    {
        Exception Exception { get; set; }

        void LoadVoid();

        void LoadObject(object value);
    }
}
