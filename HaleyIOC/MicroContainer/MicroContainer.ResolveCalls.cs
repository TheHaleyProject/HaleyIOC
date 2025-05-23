﻿using System;
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
        #region Resolve Methods
        public T Resolve<T>(IOCResolveMode mode = IOCResolveMode.AsRegistered)
        {
            var _obj = Resolve(typeof(T), mode);
            return (T)_obj.ChangeType<T>();
        }

        public T Resolve<T>(string priority_key, IOCResolveMode mode = IOCResolveMode.AsRegistered)
        {
            var _obj = Resolve(priority_key, typeof(T), mode);
            return (T)_obj.ChangeType<T>();
        }

        public object Resolve(Type contract_type, IOCResolveMode mode = IOCResolveMode.AsRegistered)
        {
            return Resolve(null,contract_type, mode);
        }
        public object Resolve(string priority_key, Type contract_type, IOCResolveMode mode = IOCResolveMode.AsRegistered)
        {
            TransientCreationLevel _tlevel = TransientCreationLevel.None;
            if (mode == IOCResolveMode.Transient) _tlevel = TransientCreationLevel.Current;
            ResolveLoad _request = new ResolveLoad(mode, priority_key, null, contract_type, null, null, transient_level: _tlevel);
            return MainResolve(_request, new MappingLoad());
        }
        public T Resolve<T>(IMappingProvider mapping_provider, IOCResolveMode mode = IOCResolveMode.AsRegistered,bool currentOnlyAsTransient = false)
        {
            var _obj = Resolve(typeof(T),mapping_provider, mode, currentOnlyAsTransient);
            return (T)_obj.ChangeType<T>();
        }

        public object Resolve(Type contract_type, IMappingProvider mapping_provider, IOCResolveMode mode = IOCResolveMode.AsRegistered, bool currentOnlyAsTransient = false)
        {
            return Resolve(null, contract_type, mapping_provider, mode, currentOnlyAsTransient);
        }

        public object Resolve(string priority_key, Type contract_type, IMappingProvider mapping_provider, IOCResolveMode mode = IOCResolveMode.AsRegistered, bool currentOnlyAsTransient = false)
        {
            TransientCreationLevel _tlevel = TransientCreationLevel.Current;
            ResolveLoad _request = new ResolveLoad(mode, priority_key, null, contract_type, null, null, transient_level: _tlevel);
            MappingLoad _map_load = new MappingLoad(mapping_provider, MappingLevel.CurrentWithDependencies);

            if (mode == IOCResolveMode.AsRegistered && currentOnlyAsTransient)
            {
                return CreateInstanceInternal(_request, _map_load); //This ensures that the first level is created as transient, irrespective of the resolve mode.
            }
            return MainResolve(_request, _map_load);
        }
        #endregion

        #region TryResolve Methods
        public bool TryResolve(Type contract_type, out object concrete_instance, IOCResolveMode mode = IOCResolveMode.AsRegistered)
        {
            return TryResolve(null, contract_type, out concrete_instance, mode);
        }
        public bool TryResolve(string priority_key, Type contract_type, out object concrete_instance, IOCResolveMode mode = IOCResolveMode.AsRegistered)
        {
            try
            {
                concrete_instance = Resolve(priority_key, contract_type, mode);
                return true;
            }
            catch (Exception)
            {
                concrete_instance = null;
                return false;
            }
        }

        public bool TryResolve(Type contract_type, IMappingProvider mapping_provider, out object concrete_instance, IOCResolveMode mode = IOCResolveMode.AsRegistered, bool currentOnlyAsTransient = false)
        {
           return TryResolve(null,contract_type,mapping_provider, out concrete_instance,mode,currentOnlyAsTransient);
        }

        public bool TryResolve(string priority_key, Type contract_type, IMappingProvider mapping_provider, out object concrete_instance, IOCResolveMode mode = IOCResolveMode.AsRegistered, bool currentOnlyAsTransient = false)
        {
            try
            {
                concrete_instance = Resolve(priority_key, contract_type,mapping_provider, mode,currentOnlyAsTransient);
                return true;
            }
            catch (Exception)
            {
                concrete_instance = null;
                return false;
            }
        }
        #endregion

        #region ResolveTransient Methods

        public T ResolveTransient<T>(TransientCreationLevel transient_level)
        {
            var _obj = ResolveTransient(typeof(T), transient_level);
            return (T)_obj.ChangeType<T>();
        }
        public T ResolveTransient<T>(string priority_key,TransientCreationLevel transient_level)
        {
            var _obj = ResolveTransient(priority_key, typeof(T), transient_level);
            return (T)_obj.ChangeType<T>();
        }
        public object ResolveTransient(Type contract_type, TransientCreationLevel transient_level)
        {
            return ResolveTransient(null,contract_type,transient_level);
        }
        public object ResolveTransient(string priority_key, Type contract_type, TransientCreationLevel transient_level)
        {
            ResolveLoad _res_load = new ResolveLoad(IOCResolveMode.Transient, priority_key, null, contract_type, null, null, transient_level);
            return MainResolve(_res_load,new MappingLoad());
        }

        public T ResolveTransient<T>(IMappingProvider mapping_provider, MappingLevel mapping_level = MappingLevel.CurrentWithDependencies)
        {
            var _obj = ResolveTransient(typeof(T), mapping_provider, mapping_level);
            return (T)_obj.ChangeType<T>();
        }
        public object ResolveTransient(Type contract_type, IMappingProvider mapping_provider, MappingLevel mapping_level = MappingLevel.CurrentWithDependencies)
        {
            return ResolveTransient(null,contract_type,mapping_provider,mapping_level);
        }
        public object ResolveTransient(string priority_key, Type contract_type, IMappingProvider mapping_provider, MappingLevel mapping_level = MappingLevel.CurrentWithDependencies)
        {
            ResolveLoad _res_load = new ResolveLoad(IOCResolveMode.Transient, priority_key, null, contract_type, null, null, _convertToTransientLevel(mapping_level));
            MappingLoad _map_load = new MappingLoad(mapping_provider, mapping_level);
            //Change below method.
            return MainResolve(_res_load,_map_load);
        }
        #endregion

        public object GetService(Type serviceType)
        {
            return Resolve(serviceType);
        }
    }
}
