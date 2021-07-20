using System;
using System.Linq;

namespace Haley.Enums
{
    public enum TransientCreationLevel
    {
        None,
        Current,
        CurrentWithDependencies,
        CascadeAll
    }
}
