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
        #region Creation Methods
        private bool register(RegisterLoad register_load, MappingLoad mapping_load)
        {
            //Universal singleton should only be registered from a Root Container.
            if (!IsRoot && register_load.Mode == RegisterMode.UniversalSingleton)
            {
                throw new ArgumentException("Universal singleton state can be registered only from a Root Container. Registering from a child container is probhibited to avoid Captive Dependency.");
            }

            //Validate if the contract type is alredy registered. If so, against correct concrete type.
            //if a priority key - contract type combo is already registered as an Universal Singleton, that also will be returned in below method.
            //We donot check if the parents have already some registered value because in such cases, we are OVERRIDDING THE VALUES at this container level. So, only while resolving we need to validate. Thus, in below method, we will use "CheckinParents = false".
            if (ExistsInCurrentContainer(register_load)) return false; 

            //Validate if the concrete type can be registered
            validateConcreteType(register_load.ConcreteType);

            //Generate instance only if the provided value is null and also singleton. Only if it is singleton, we create an instance and store. Else we store only the concrete type and save instance as it is (even if is null).
            if (register_load.ConcreteInstance == null && register_load.Mode != RegisterMode.Transient && !OnDemandResolution)
            {
                //If we have opted for on demand resolution, we should only register the interfaces and types and do not try to create an instance now.
                ResolveOnDemand(ref register_load, null, mapping_load);
            }

            //Get the key to register.
            var _key = new KeyBase(register_load.ContractType, register_load.PriorityKey);

            //We have already validate if overwrite is required or not. If we reach this point, then overwrite is required.
            return Mappings.TryAdd(_key, register_load);
        }
        private object createInstance(ResolveLoad resolve_load, MappingLoad mapping_load)
        {
            //If transient creation is current level only, then further dependencies should not generate new instance.
            if (resolve_load.TransientLevel == TransientCreationLevel.Current) resolve_load.TransientLevel = TransientCreationLevel.None;
            object concrete_instance = null;
            validateConcreteType(resolve_load.ConcreteType);

            //PRIO 1 - Try to see if this is a kind of universal dependency.
            if(resolveUniversalObject(ref concrete_instance, resolve_load) && concrete_instance != null) return concrete_instance;

            //PRIO 2 - Get the constructor of the concrete type and try to resolve it's constructor parameters and also the properties.
            ConstructorInfo constructor = getConstructor(resolve_load.ConcreteType);
            resolveConstructorParameters(ref constructor, resolve_load, mapping_load, ref concrete_instance);
            resolveProperties(resolve_load, mapping_load, ref concrete_instance);
            return concrete_instance;
        }

        private void resolveConstructorParameters(ref ConstructorInfo constructor, ResolveLoad resolve_load, MappingLoad mapping_load, ref object concrete_instance)
        {
            //If creation is current with properties, then constructor and props should generate new instance. Rest should be resolved.
            if (resolve_load.TransientLevel == TransientCreationLevel.CurrentWithDependencies)
            { resolve_load.TransientLevel = TransientCreationLevel.Current; }

            //TODO: CHECK IF BELOW REASSIGNMENT IS REQUIRED.
            if (mapping_load?.level == MappingLevel.CurrentWithDependencies)
            { mapping_load.level = MappingLevel.Current; }

            //Resolve the param arugments for the constructor.
            ParameterInfo[] constructor_params = constructor.GetParameters();

            //If parameter less construction, return a new creation.
            if (constructor_params.Length == 0)
            {
                concrete_instance = Activator.CreateInstance(resolve_load.ConcreteType);
            }
            else
            {
                List<object> parameters = new List<object>(constructor_params.Length);
                foreach (ParameterInfo pinfo in constructor_params)
                {
                    //New resolve and mapping load.
                    ResolveLoad _new_res_load = new ResolveLoad(resolve_load.Mode, resolve_load.PriorityKey, pinfo.Name, pinfo.ParameterType, resolve_load.ConcreteType, null, resolve_load.TransientLevel);

                    MappingLoad _new_map_load = new MappingLoad(mapping_load.provider, mapping_load.level, InjectionTarget.Constructor);

                    parameters.Add(MainResolve(_new_res_load, _new_map_load));
                }
                concrete_instance = constructor.Invoke(parameters.ToArray());
            }

        }
        private void resolveProperties(ResolveLoad resolve_load, MappingLoad mapping_load, ref object concrete_instance)
        {
            //If creation is current with properties, then constructor and props should generate new instance. Rest should be resolved.
            if (resolve_load.TransientLevel == TransientCreationLevel.CurrentWithDependencies) resolve_load.TransientLevel = TransientCreationLevel.Current;
            if (mapping_load?.level == MappingLevel.CurrentWithDependencies) mapping_load.level = MappingLevel.Current;

            //Resolve only properties that are of type Haley inject and also ignore if it has haleyignore
            var _props = resolve_load.ConcreteType.GetProperties().Where(
                p => Attribute.IsDefined(p, typeof(HaleyInjectAttribute)));

            if (_props.Count() > 0)
            {
                foreach (PropertyInfo pinfo in _props)
                {
                    try
                    {
                        //New resolve and mapping load.
                        ResolveLoad _new_res_load = new ResolveLoad(resolve_load.Mode, resolve_load.PriorityKey, pinfo.Name, pinfo.PropertyType, resolve_load.ConcreteType, null, resolve_load.TransientLevel);

                        MappingLoad _new_map_load = new MappingLoad(mapping_load.provider, mapping_load.level, InjectionTarget.Property);

                        var resolved_value = MainResolve(_new_res_load, _new_map_load);
                        if (resolved_value != null) pinfo.SetValue(concrete_instance, resolved_value);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
        }
        #endregion
    }
}
