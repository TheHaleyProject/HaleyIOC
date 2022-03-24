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
    public  sealed partial class MicroContainer : IBaseContainer
    {
        #region Helpers
        private TransientCreationLevel _convertToTransientLevel(MappingLevel mapping_level)
        {
            TransientCreationLevel _transient_level = TransientCreationLevel.None;
            switch (mapping_level)
            {
                case MappingLevel.Current:
                    _transient_level = TransientCreationLevel.Current;
                    break;
                case MappingLevel.CurrentWithDependencies:
                    _transient_level = TransientCreationLevel.CurrentWithDependencies;
                    break;
                case MappingLevel.CascadeAll:
                    _transient_level = TransientCreationLevel.CascadeAll;
                    break;
            }
            return _transient_level;
        }
        private bool ExistsInCurrentContainer(RegisterLoad register_load)
        {
            var _status = CheckIfRegistered(register_load.ContractType, register_load.PriorityKey);
            return _status.status; //Returns if registered.
        }
        private void validateConcreteType(Type concrete_type)
        {
            if (concrete_type == null || concrete_type.IsAbstract || concrete_type.IsEnum || concrete_type.IsInterface || concrete_type.IsArray || concrete_type.IsList() || concrete_type.IsEnumerable() || concrete_type.IsDictionary() || concrete_type.IsCollection())
            {
                throw new ArgumentException($@"Concrete type cannot be null, abstract, enum, interface, array, list, enumerable, dictionary, or collection. {concrete_type} is not a valid concrete type.");
            }
        }

        private RegisterMode convertMode(SingletonMode mode)
        {
            switch (mode)
            {
                case SingletonMode.ContainerSingleton:
                    return RegisterMode.ContainerSingleton;
                case SingletonMode.ContainerWeakSingleton:
                    return RegisterMode.ContainerWeakSingleton;
                case SingletonMode.UniversalSingleton:
                    return RegisterMode.UniversalSingleton;
            }
            return RegisterMode.ContainerSingleton;
        }

        private List<RegisterLoad> GetAllMappings(Type contract_type)
        {
            List<RegisterLoad> result = new List<RegisterLoad>();

            //For the given type, get all the mappings. (HIGH PRIO)
            var keys = Mappings.Keys.Where(_key => _key.contract_type == contract_type);

            foreach (var key in keys)
            {
                RegisterLoad _load;
                Mappings.TryGetValue(key, out _load);
                result.Add(_load);
            }

            List<RegisterLoad> parent_results = new List<RegisterLoad>();
            if (Parent != null && Parent is MicroContainer mContParent)
            {
                var parentLoads = mContParent.GetAllMappings(contract_type);
                parent_results.AddRange(parentLoads);
            }

            //The Parent results might contain, different concrete instance values for same priorityKey, contractype combo. For our case, if a register load has same priority key and same contract type, it can be considered duplicate. (irrespective of the different concrete instance or different concrete type). Also, we need to give high priority to the local mapping, so, parent will be overriden. 
            //That is why we dont' directly use the "Distinct(new RegisterLoadComparer()).ToList()" to filter out the duplicates.

            foreach (var load in parent_results)
            {
                if (result.Any(p => p.ContractType == load.ContractType && p.PriorityKey == load.PriorityKey)) continue;
                result.Add(load);
            }

            return result;
        }
        private (bool exists, bool isInParentContainer, RegisterLoad load) getMapping(string priority_key, Type contract_type)
        {
            //Should try to get the info from current and then go up to parent.
            var _key = new KeyBase(contract_type, priority_key);
            return getMapping(_key);
        }

        private (bool exists, bool isInParentContainer, RegisterLoad load) getMapping(IKeyBase key)
        {
            //Preference to prioritykey/contract_type combination.
            if (Mappings.ContainsKey(key))
            {
                Mappings.TryGetValue(key, out var _existing);
                return (true, false, _existing); //Even when we validate this from inside a parent container, we will always set "isInParentContainer" false. only in below block ,we reset this.
            }

            //If we are not able to find in current state, we go to the parent

            if (Parent != null && Parent is MicroContainer mCont)
            {
                var parentResult = mCont.getMapping(key);
                if (parentResult.exists)
                {
                    return (true,true,parentResult.load); //The load is coming from a parent. So, act accordingly.
                }
            }

            return (false,false, null);
        }
        private ConstructorInfo getConstructor(Type concrete_type)
        {
            ConstructorInfo constructor = null;
            var constructors = concrete_type.GetConstructors();

            if (constructors.Length == 0)
            {
                throw new ArgumentException($@"No constructors found. Unable to create an instance for {concrete_type.Name}");
            }

            if (constructors.Length > 1)
            {
                //If we have more items, get the first constructor that has [HaleyInject]
                foreach (var _constructor in constructors)
                {
                    var attr = _constructor.GetCustomAttribute(typeof(HaleyInjectAttribute));
                    if (attr != null)
                    {
                        constructor = _constructor;
                        break;
                    }
                }
            }

            //Taking the first constructor.
            if (constructor == null)
            {
                //Get the first constructor where ignore attribute is not present.
                constructor = constructors.FirstOrDefault(p => p.GetCustomAttribute(typeof(HaleyIgnoreAttribute)) == null);
                if (constructor == null) throw new ArgumentException($@"No valid constructors found. Unable to create an instance for {concrete_type.Name}");
            }
            return constructor;
        }
        #endregion
    }
}
