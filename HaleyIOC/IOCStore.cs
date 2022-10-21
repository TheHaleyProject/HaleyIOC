using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Haley.Abstractions;

namespace Haley.IOC
{
    public sealed class IOCStore
    {
        #region Static Properties
        private static IOCStore _instance = new IOCStore();
        public static IMicroContainer DI => _instance._di;
        #endregion
        IMicroContainer _di;
        public IOCStore()
        {
            _di = new MicroContainer() { };
        }
    }
}
