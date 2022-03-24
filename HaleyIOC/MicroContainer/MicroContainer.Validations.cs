using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using Haley.Enums;
using System.Runtime.InteropServices;
using Haley.Models;
using Haley.Abstractions;
using Haley.Utils;

namespace Haley.IOC
{
    public sealed partial class MicroContainer
    {
        public (bool status, string message, IRegisterLoad load) CheckIfRegistered(IKeyBase key, bool checkInParents = false)
        {
            //Check if it is registered as an UniversalSingleton Object at the root level.
            if (Root != null && !IsRoot) //If current container is root, then we will end up in a overflow loop.
            {
                var _rootLoad = Root.CheckIfRegistered(key);
                if (_rootLoad.status && _rootLoad.load.Mode == RegisterMode.UniversalSingleton)
                {
                    //Only if it is a universal singleton we consider that inclusive in the local mapping.
                    var message = $@"The key : {key} is registered against the type {_rootLoad.load.ConcreteType} inside the Root container as an Universal Singleton.";
                    return (true, message, _rootLoad.load);
                }
            }

            if (Mappings.ContainsKey(key))
            {
                Mappings.TryGetValue(key, out var current_load);
                var message = $@"The key : {key} is registered against the type {current_load.ConcreteType} inside the container {this.Name} with id : {this.Id} .";
                return (true, message,current_load);
            }
            else if (checkInParents && this.Parent != null)
            {
                return this.Parent.CheckIfRegistered(key, checkInParents);
            }
            else
            {
                return (false, "Unable to find any registration", null);
            }
        }
        public (bool status, string message, IRegisterLoad load) CheckIfRegistered(Type contract_type, string priority_key, bool checkInParents = false)
        {
            return CheckIfRegistered(new KeyBase(contract_type, priority_key), checkInParents);
        }
        public (bool status, string message, IRegisterLoad load) CheckIfRegistered<Tcontract>(string priority_key, bool checkInParents = false)
        {
            return CheckIfRegistered(typeof(Tcontract), priority_key, checkInParents);
        }
    }
}
