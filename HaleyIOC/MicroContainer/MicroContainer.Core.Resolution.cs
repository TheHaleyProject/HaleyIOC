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
    public  sealed partial class MicroContainer 
    {
        #region Resolution Methods
        private object MainResolve(ResolveLoad resolve_load, MappingLoad mapping_load)
        {
            //If failed to resolve with in this container, we move ahead and go to the parent.
            object concrete_instance = null;

            //At most priority should be give to override
            if (OverrideCallBack != null)
            {
                try
                {
                    do {
                        concrete_instance = OverrideCallBack?.Invoke(resolve_load, this.Id); //Send in the container id for referencing by the override call back
                        if (concrete_instance == null) break;
                        //Now the concrete instance should either be same as contract type or should be assignable to contract type.
                        var createdInstanceType = concrete_instance.GetType();
                        if (createdInstanceType != resolve_load.ContractType && !resolve_load.ContractType.IsAssignableFrom(createdInstanceType)) {
                            concrete_instance = null;
                            break;
                        }
                        return concrete_instance;
                    } while (false); //Execute only once.
                }
                catch (Exception)
                {
                    //Later stage try to log it.
                }
            }

            //Before we try to do any kind of resolve, check for universal singelton instances. if found, return then. 
            //Universal singleton instances are only found in the root.
            if (ResolveUniversalObject(ref concrete_instance, resolve_load) && concrete_instance != null) return concrete_instance;

            switch (resolve_load.Mode)
            {
                case ResolveMode.AsRegistered: //This can be transient or singleton.
                    ResolveAsRegistered(resolve_load, mapping_load, out concrete_instance);
                    break;
                case ResolveMode.Transient: //This creates new instance.
                    ResolveAsTransient(resolve_load, mapping_load, out concrete_instance);
                    break;
            }

            return concrete_instance;
        }
        private void ResolveWithMappingProvider(ResolveLoad resolve_load, ref MappingLoad mapping_load, out object concrete_instance)
        {
            //Begin with null output.
            concrete_instance = null;

            //Mapping level defines until which stage or level , the mapping should be applied. If mapping provider is null or mapping level is none, don't proceed. 
            if (mapping_load.provider == null || mapping_load.level == MappingLevel.None) return;

            if (mapping_load.level == MappingLevel.Current)
            { mapping_load.level = MappingLevel.None; }

            if (resolve_load.ContractType == null) { throw new ArgumentNullException(nameof(resolve_load.ContractType)); }

            var _dip_values = mapping_load.provider.Resolve(resolve_load.ContractType, resolve_load.ContractName, resolve_load.ContractParent);

            //if external mapping resolves to a value, ensure that this injection is for suitable target or it should be for all
            if (_dip_values.concrete_instance != null && (mapping_load.injection == _dip_values.injection || mapping_load.injection == InjectionTarget.All))
            {
                concrete_instance = _dip_values.concrete_instance;
            }
        }
        private void ResolveAsRegistered(ResolveLoad resolve_load, MappingLoad mapping_load, out object concrete_instance)
        {
            concrete_instance = null;
            Type current_contract_type = resolve_load.ContractType;
            if (current_contract_type == null) throw new ArgumentNullException(nameof(current_contract_type));

            //Try to resolve multiple params if needed.
            _resolveArrayTypes(resolve_load, mapping_load, out concrete_instance);

            if (concrete_instance != null) return;
            var regData = getMapping(resolve_load.PriorityKey, current_contract_type);
            if (!regData.exists)
            {
                //Which means that the priority key could be absent. Try to get without priority key.
                regData = getMapping(null, current_contract_type);
            }
            
            //Try to resolve with mapping provider before anything.
            ResolveWithMappingProvider(resolve_load, ref mapping_load, out concrete_instance);

            if (concrete_instance != null) return;

            //If it is registered, then resolve it else re send request as transient.
            if (regData.exists)
            {
                //If already exists, then fetch the concrete type. Also, if a concrete type is registered, we can be confident that it has already passed the concrete type validation.
                resolve_load.ConcreteType = regData.load?.ConcreteType ?? resolve_load.ConcreteType ?? current_contract_type;

                if (regData.isInParentContainer && (regData.load?.Mode == RegisterMode.ContainerSingleton || regData.load?.Mode == RegisterMode.ContainerWeakSingleton) && !StopCheckingParents)
                {
                    //We found a registered data but not in current container but in some parent. So, we need to register this singleton object in this local container and return it.
                    var newSingletonInstance = CreateInstanceInternal(resolve_load, mapping_load);
                    if (newSingletonInstance != null)
                    {
                        //Use this singleton and register in this container.
                        RegisterLoad newLoad = new RegisterLoad();
                        regData.load.MapProperties(newLoad); //map into Newload.
                        newLoad.ConcreteInstance = newSingletonInstance; //Get this new singleton.
                        Mappings.TryAdd(new KeyBase(newLoad.ContractType, newLoad.PriorityKey), newLoad);
                        concrete_instance = newSingletonInstance;
                    }
                }

                if (concrete_instance != null) return; //We managed to recreate the instance and also add to mapping.

                switch (regData.load.Mode)
                {
                    case RegisterMode.UniversalSingleton:
                    case RegisterMode.ContainerSingleton:
                    case RegisterMode.ContainerWeakSingleton:
                        concrete_instance = regData.load.ConcreteInstance;
                        if (concrete_instance == null)
                        {
                            concrete_instance = ResolveOnDemand(ref regData.load, resolve_load, mapping_load);
                        }
                        break;
                    case RegisterMode.Transient:
                        concrete_instance = CreateInstanceInternal(resolve_load, mapping_load);
                        break;
                }
            }
            else // It is not registered (even in any parent). So, we reassign as transient resolution.
            {
                resolve_load.Mode = ResolveMode.Transient;
                if (resolve_load.TransientLevel == TransientCreationLevel.None) { resolve_load.TransientLevel = TransientCreationLevel.Current; }

                //todo: Should we reset the mapping level as well??
                concrete_instance = MainResolve(resolve_load, mapping_load);
            }
        }
        private void ResolveAsTransient(ResolveLoad resolve_load, MappingLoad mapping_load, out object concrete_instance)
        {
            concrete_instance = null;
            //Try to resolve multiple params if needed.
            _resolveArrayTypes(resolve_load, mapping_load, out concrete_instance);
            if (concrete_instance != null) return;

            //By default, create instance for the contract type.
            if (resolve_load.ConcreteType == null)
            { resolve_load.ConcreteType = resolve_load.ContractType; }

            //Try to resolve with mapping provider before anything. (if we have any kind of string parameter, we will resolve using mapping provider).
            ResolveWithMappingProvider(resolve_load, ref mapping_load, out concrete_instance);
            if (concrete_instance != null) return;


            //This will try to resolve in current container and go one level up to each parent.
            var registeredData = getMapping(resolve_load.PriorityKey, resolve_load.ContractType);

            if (!registeredData.exists)
            {
                //Which means that the priority key could be absent. Try to get without priority key.
                registeredData = getMapping(null, resolve_load.ContractType);
            }

            //If a mapping already exists, then create instance for the concrete type in mapping.
            if (registeredData.exists && !(registeredData.isInParentContainer && StopCheckingParents))
            {
                resolve_load.ConcreteType = registeredData.load?.ConcreteType; 
            }


            //Validate concrete type.
            if (resolve_load.ConcreteType == typeof(string) || resolve_load.ConcreteType.IsValueType)
            {
                throw new ArgumentException($@"Value type dependency error. The {resolve_load.ContractParent ?? resolve_load.ContractType} with contract name {resolve_load.ContractName ?? "#NotFound#"} contains a value dependency {resolve_load.ConcreteType}. Try adding a mapping provider for injecting value types.");
            }

            //If transient is not none, try to create new instance. If none, then go with as registered.
            if (resolve_load.TransientLevel != TransientCreationLevel.None)
            {
                //Before trying to create a transient instance, check the registered data.
                if (registeredData.exists && (
                    registeredData.load?.Mode == RegisterMode.UniversalSingleton ||( registeredData.load?.Mode == RegisterMode.ContainerSingleton && !registeredData.isInParentContainer)))
                {
                    //Only for weak singleton (inside same container) we should allow transient creation or else reuse the registered data. If the singleton is coming from parent container, then we should allow creation of transient.
                    concrete_instance = registeredData.load.ConcreteInstance;
                    if (concrete_instance == null)
                    {
                        concrete_instance = ResolveOnDemand(ref registeredData.load, resolve_load, mapping_load);
                    }
                }

                if (concrete_instance != null) return; //Meaning we managed to fill the value through forced singleton.
                concrete_instance = CreateInstanceInternal(resolve_load, mapping_load);
            }
            else
            {
                resolve_load.Mode = ResolveMode.AsRegistered;
                concrete_instance = MainResolve(resolve_load, mapping_load);
            }
        }
        private void _resolveArrayTypes(ResolveLoad resolve_load, MappingLoad mapping_load, out object concrete_instance)
        {
            //Whole idea behind the resolve array types is to get a list of all the possible values (even with different priority keys).
            // For a same priority key-contract type combo, child container might have an object and the parent might also have an object. We don't need both here. We only need unique sets (if same combo is present in parent and child, it is very sure that it is deliberately overridden.
            concrete_instance = null;
            Type array_contract_type = null;
            //If contracttype is of list or enumerable or array or collection, then return all the registered values for the generictypedefinition
            if (resolve_load.ContractType.IsList())
            {
                //We need to check the generic type.
                array_contract_type = resolve_load.ContractType.GetGenericArguments()[0];
            }
            else if (resolve_load.ContractType.IsArray)
            {
                array_contract_type = resolve_load.ContractType.GetElementType();
            }

            if (array_contract_type == null) return; //Then this is not a collection and unable to resolve array.

            List<RegisterLoad> _registrations = new List<RegisterLoad>();
            _registrations = GetAllMappings(array_contract_type) ?? new List<RegisterLoad>(); //Including parents.

            List<object> _instances_list = new List<object>();

            if (_registrations.Count > 0)
            {
                foreach (var _registration in _registrations)
                {
                    try
                    {
                        ResolveLoad _new_resolve_load = _registration.Convert(resolve_load.ContractName, resolve_load.ContractParent, resolve_load.Mode);
                        _new_resolve_load.TransientLevel = resolve_load.TransientLevel;
                        var _current_instance = MainResolve(_new_resolve_load, mapping_load);
                        _instances_list.Add(_current_instance);
                    }
                    catch (Exception)
                    {
                        // Don't throw, continue
                        continue; //Implement a logger to capture the details and return back to the user.
                    }
                }
                concrete_instance = _instances_list.ChangeType(resolve_load.ContractType); //Convert to the contract type.
            }
        }

        private object ResolveOnDemand(ref RegisterLoad load,ResolveLoad resolve_load = null,MappingLoad mapping_load = null)
        {
            //First priority: Concrete instance.
            if (load.ConcreteInstance != null) return load.ConcreteInstance;

            //Second priority: Check if any delegate is present or this is marked as a lazy register.
            if (load.InstanceCreator != null || load.IsLazyRegister)
            {
                try
                {
                    load.ConcreteInstance = load.InstanceCreator.Invoke();
                    if (load.ConcreteInstance != null) return load.ConcreteInstance;
                }
                catch (Exception)
                {
                    //log it.
                }
            }

            if (resolve_load == null)
            {
                //Convert register load to resolve load.
                resolve_load = load.Convert(null, null, ResolveMode.AsRegistered);
            }
            load.ConcreteInstance = CreateInstanceInternal(resolve_load, mapping_load); //Create instance resolving all dependencies
            return load.ConcreteInstance;

        }

        private bool ResolveUniversalObject(ref object concrete_instance, ResolveLoad load)
        {
            //Try to see if the root has any object.
            if (!IsRoot && Root != null)
            {
                var status = Root.CheckIfRegistered(load.ContractType, load.PriorityKey);
                if (status.status && status.load?.Mode == RegisterMode.UniversalSingleton && Root is MicroContainer mCont)
                {
                    mCont.Mappings.TryGetValue(new KeyBase(load.ContractType, load.PriorityKey), out var result);
                    concrete_instance = result.ConcreteInstance; //Return the universal object directly.
                    if (concrete_instance == null)
                    {
                        concrete_instance = ResolveOnDemand(ref result,load);
                    }

                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
