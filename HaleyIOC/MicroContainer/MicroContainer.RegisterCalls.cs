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
        #region Register Methods
        public bool Register<TConcrete>(RegisterMode mode = RegisterMode.ContainerSingleton) where TConcrete : class
        {
            try
            {
                return RegisterWithKey<TConcrete>(null, mode);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool Register<TConcrete>(TConcrete instance, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            try
            {
                return RegisterWithKey(null, instance,mode);
            }
            catch (Exception ex)
            {
                if (ErrorHandling == ExceptionHandling.Throw) throw ex;
                return false;
            }
        }
        public bool Register<TConcrete>(IMappingProvider dependencyProvider, MappingLevel mapping_level, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            try
            {
                return RegisterWithKey<TConcrete>(null, dependencyProvider, mapping_level,mode);
            }
            catch (Exception ex)
            {
                if (ErrorHandling == ExceptionHandling.Throw) throw ex;
                return false;
            }
        }
        public bool Register<TContract, TConcrete>(TConcrete instance, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            try
            {
                return RegisterWithKey<TContract, TConcrete>(null, instance,mode);
            }
            catch (Exception ex)
            {
                if (ErrorHandling == ExceptionHandling.Throw) throw ex;
                return false;
            }
        }
        public bool Register<TContract, TConcrete>(RegisterMode mode = RegisterMode.ContainerSingleton) where TConcrete : class, TContract
        {
            try
            {
                return RegisterWithKey<TContract, TConcrete>(null, mode);
            }
            catch (Exception ex)
            {
                if (ErrorHandling == ExceptionHandling.Throw) throw ex;
                return false;
            }
        }
        public bool Register<TContract, TConcrete>(IMappingProvider dependencyProvider, MappingLevel mapping_level, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            try
            {
                return RegisterWithKey<TContract, TConcrete>(null, dependencyProvider, mapping_level,mode);
            }
            catch (Exception ex)
            {
                if (ErrorHandling == ExceptionHandling.Throw) throw ex;
                return false;
            }
        }

        #endregion

        #region RegisterWithKey Methods
        public bool RegisterWithKey<TConcrete>(string priority_key, RegisterMode mode = RegisterMode.ContainerSingleton) where TConcrete : class
        {
            RegisterLoad _reg_load = new RegisterLoad(mode, priority_key, typeof(TConcrete), typeof(TConcrete),null);
            MappingLoad _map_load = new MappingLoad();
            //For this method, both contract and concrete type are same.
            return register(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TConcrete>(string priority_key, TConcrete instance, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            //For this method, both contract and concrete type are same.
            //If we have an instance, then obviously it is of singleton or forced singleton registration type.

            RegisterMode _regMode = convertMode(mode);
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TConcrete), typeof(TConcrete), instance);
            MappingLoad _map_load = new MappingLoad();
            return register(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TConcrete>(string priority_key, IMappingProvider dependencyProvider, MappingLevel mapping_level, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class
        {
            RegisterMode _regMode = convertMode(mode);
            //For this method, both contract and concrete type are same.
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TConcrete), typeof(TConcrete), null);
            MappingLoad _map_load = new MappingLoad(dependencyProvider,mapping_level);
            return register(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TContract, TConcrete>(string priority_key, RegisterMode mode = RegisterMode.ContainerSingleton) where TConcrete : class, TContract
        {
            RegisterLoad _reg_load = new RegisterLoad(mode, priority_key, typeof(TContract), typeof(TConcrete), null);
            MappingLoad _map_load = new MappingLoad();
            return register(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TContract, TConcrete>(string priority_key, TConcrete instance, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            RegisterMode _regMode = convertMode(mode);
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TContract), typeof(TConcrete), instance);
            MappingLoad _map_load = new MappingLoad();
            return register(_reg_load,_map_load);
        }
        public bool RegisterWithKey<TContract, TConcrete>(string priority_key, IMappingProvider dependencyProvider, MappingLevel mapping_level, SingletonMode mode = SingletonMode.ContainerSingleton) where TConcrete : class, TContract
        {
            RegisterMode _regMode = convertMode(mode);
            RegisterLoad _reg_load = new RegisterLoad(_regMode, priority_key, typeof(TContract), typeof(TConcrete), null);
            MappingLoad _map_load = new MappingLoad(dependencyProvider,mapping_level);
            return register(_reg_load,_map_load);
        }
        #endregion
    }
}
