using System;
using System.Collections.Generic;
using System.Text;
using Haley.Abstractions;

namespace Haley.IOC
{
    public sealed class IOCStore
    {
        public IBaseContainer DI { get; }

        public IOCStore()
        {
            DI = new MicroContainer() { };
        }
        public static IOCStore Singleton = new IOCStore();
    }
}
