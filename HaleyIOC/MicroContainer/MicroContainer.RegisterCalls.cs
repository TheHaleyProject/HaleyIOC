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

        #region DelegateRegister 
        public bool DelegateRegister<TConcrete>(Func<TConcrete> del, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            return DelegateRegister(null, del, mode);
        }
        public bool DelegateRegister<TContract, TConcrete>(Func<TConcrete> del, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            return DelegateRegister<TContract,TConcrete>(null, del, mode);
        }
        public bool DelegateRegister<TConcrete>(string priority_key, Func<TConcrete> del, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            RegisterMode _regMode = convertMode(mode);
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TConcrete), typeof(TConcrete), null);
            _reg_load.SetInstanceCreator(del);

            MappingLoad _map_load = new MappingLoad();
            return RegisterInternal(_reg_load, _map_load);
        }
        public bool DelegateRegister<TContract, TConcrete>(string priority_key, Func<TConcrete> del, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            RegisterMode _regMode = convertMode(mode);
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TContract), typeof(TConcrete), null);
            _reg_load.SetInstanceCreator(del);
            MappingLoad _map_load = new MappingLoad();
            return RegisterInternal(_reg_load, _map_load);
        }
        #endregion

        #region Register Methods
        public bool Register<TConcrete>(RegisterMode mode = RegisterMode.ContainerSingleton) where TConcrete : class
        {
            return RegisterWithKey<TConcrete>(null, mode);
        }
        public bool Register<TConcrete>(TConcrete instance, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            return RegisterWithKey(null, instance, mode);
        }
        public bool Register<TConcrete>(IMappingProvider dependencyProvider, MappingLevel mapping_level, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            return RegisterWithKey<TConcrete>(null, dependencyProvider, mapping_level, mode);
        }
        public bool Register<TContract, TConcrete>(TConcrete instance, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            return RegisterWithKey<TContract, TConcrete>(null, instance, mode);
        }
        public bool Register<TContract, TConcrete>(RegisterMode mode = RegisterMode.ContainerSingleton) where TConcrete : class, TContract
        {
            return RegisterWithKey<TContract, TConcrete>(null, mode);
        }
        public bool Register<TContract, TConcrete>(IMappingProvider dependencyProvider, MappingLevel mapping_level, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            return RegisterWithKey<TContract, TConcrete>(null, dependencyProvider, mapping_level, mode);
        }

        #endregion

        #region RegisterWithKey Methods
        public bool RegisterWithKey<TConcrete>(string priority_key, RegisterMode mode = RegisterMode.ContainerSingleton) where TConcrete : class
        {
            
            RegisterLoad _reg_load = new RegisterLoad(mode, priority_key, typeof(TConcrete), typeof(TConcrete),null);
            MappingLoad _map_load = new MappingLoad();
            //For this method, both contract and concrete type are same.
            return RegisterInternal(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TConcrete>(string priority_key, TConcrete instance, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            //For this method, both contract and concrete type are same.
            //If we have an instance, then obviously it is of singleton or forced singleton registration type.

            RegisterMode _regMode = convertMode(mode);
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TConcrete), typeof(TConcrete), instance);
            MappingLoad _map_load = new MappingLoad();
            return RegisterInternal(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TConcrete>(string priority_key, IMappingProvider dependencyProvider, MappingLevel mapping_level, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            RegisterMode _regMode = convertMode(mode);
            //For this method, both contract and concrete type are same.
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TConcrete), typeof(TConcrete), null);
            MappingLoad _map_load = new MappingLoad(dependencyProvider,mapping_level);
            return RegisterInternal(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TContract, TConcrete>(string priority_key, RegisterMode mode = RegisterMode.ContainerSingleton) where TConcrete : class, TContract
        {
            RegisterLoad _reg_load = new RegisterLoad(mode, priority_key, typeof(TContract), typeof(TConcrete), null);
            MappingLoad _map_load = new MappingLoad();
            return RegisterInternal(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TContract, TConcrete>(string priority_key, TConcrete instance, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            RegisterMode _regMode = convertMode(mode);
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TContract), typeof(TConcrete), instance);
            MappingLoad _map_load = new MappingLoad();
            return RegisterInternal(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TContract, TConcrete>(string priority_key, IMappingProvider dependencyProvider, MappingLevel mapping_level, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            RegisterMode _regMode = convertMode(mode);
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TContract), typeof(TConcrete), null);
            MappingLoad _map_load = new MappingLoad(dependencyProvider,mapping_level);
            return RegisterInternal(_reg_load,_map_load);
        }
        #endregion
    }
}
