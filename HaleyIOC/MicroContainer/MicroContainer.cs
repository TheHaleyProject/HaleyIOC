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
using System.Xml.Linq;

namespace Haley.IOC
{
    public sealed partial class MicroContainer : IMicroContainer
    {
        #region ATTRIBUTES
        //All mappings for this container
        readonly ConcurrentDictionary<IIOCKeyBase, RegisterLoad> Mappings = new ConcurrentDictionary<IIOCKeyBase, RegisterLoad>();
        //created children
        Func<ResolveLoad,string, object> OverrideCallBack = null;

        #endregion
        public event EventHandler<string> ContainerDisposed;
        #region Properties
        public bool ResolveOnlyOnDemand { get; private set; }
        public bool IsDisposed { get; private set; }
        public string Id { get;}
        public string Name { get; }
        public ExceptionHandling ErrorHandling { get; set; }
        #endregion

        public void Dispose() {
            try {
                if (IsDisposed) return;//Its already disposed.

                //Cascade dispose through all child.
                foreach (var kvp in ChildContainers) {
                    kvp.Value.Dispose();
                }

                //Parent and root remains the same.
                ChildContainers.Clear(); //All child container registrations will also be cleared.
                Mappings.Clear();
                

            } finally {
                if (!IsDisposed) {
                    IsDisposed = true;
                    ContainerDisposed?.Invoke(this, Id); //subscribed items (like factory) will then dispose themselves.
                }
            }
        }

        public MicroContainer() 
        {
            ResolveOnlyOnDemand = false;

            if (string.IsNullOrWhiteSpace(Id)) {
                Id = Guid.NewGuid().ToString();
            }

            ErrorHandling = ExceptionHandling.Throw;
            IsRoot = true; //Whenever we create a new microcontainer, that becomes a root. However, if created using "CreateChildContainer" method, that becomes a child.
            //Within the scope of this container , whomever tries to resolve, will get only this container.
            this.Register<IMicroContainer, MicroContainer>(this, SingletonMode.ContainerSingleton); //IHaleyContainer should not be an Universal Singleton as each scope will have their own container. 
        }
        [HaleyIgnore]
        public MicroContainer(Guid id, string name) : this()
        {
            this.Name = name;
            if (id == default(Guid)) {
                id = Guid.NewGuid();
            }
            this.Id = Id.ToString();
        }

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
        private bool ExistsInCurrentContainer(IIOCRegisterLoad register_load)
        {
            var _status = CheckIfRegistered(register_load.ContractType, register_load.PriorityKey);
            return _status.status; //Returns if registered.
        }
        private bool ValidateConcreteType(Type concrete_type)
        {
            if (concrete_type == null || concrete_type.IsAbstract || concrete_type.IsEnum || concrete_type.IsInterface || concrete_type.IsArray || concrete_type.IsList() || concrete_type.IsEnumerable() || concrete_type.IsDictionary() || concrete_type.IsCollection())
            {
                if (ErrorHandling == ExceptionHandling.Throw)
                {
                    throw new ArgumentException($@"Concrete type cannot be null, abstract, enum, interface, array, list, enumerable, dictionary, or collection. {concrete_type} is not a valid concrete type.");
                }
                return false;
            }
            return true;
        }

        private IOCRegisterMode convertMode(SingletonMode mode)
        {
            switch (mode)
            {
                case SingletonMode.ContainerSingleton:
                    return IOCRegisterMode.ContainerSingleton;
                case SingletonMode.ContainerWeakSingleton:
                    return IOCRegisterMode.ContainerWeakSingleton;
                case SingletonMode.UniversalSingleton:
                    return IOCRegisterMode.UniversalSingleton;
            }
            return IOCRegisterMode.ContainerSingleton;
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

        private (bool exists, bool isInParentContainer, RegisterLoad load) getMapping(IIOCKeyBase key)
        {
            //Preference to prioritykey/contract_type combination.
            if (Mappings.ContainsKey(key))
            {
                Mappings.TryGetValue(key, out var _existing);
                return (true, false, _existing); //Even when we validate this from inside a parent container, we will always set "isInParentContainer" false. only in below block ,we reset this.
            }

            //If we are not able to find in current state, we go to the parent

            if (Parent != null && Parent is MicroContainer mCont && !IgnoreParentContainer)
            {
                var parentResult = mCont.getMapping(key);
                if (parentResult.exists)
                {
                    return (true, true, parentResult.load); //The load is coming from a parent. So, act accordingly.
                }
            }

            return (false, false, null);
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

        #region Validataions
        public bool TrySetResolutionOverride(Func<ResolveLoad, string, object> overrideCallback)
        {
            if (OverrideCallBack != null) return false;
            OverrideCallBack = overrideCallback;
            return true;
        }
        public void SetResolveOnlyOnDemandMode(bool flag)
        {
            //This will only affect during the registration time.
            //If we have already have some registrations done (but without resolution), those will still be resolved on demand.
            ResolveOnlyOnDemand = flag;
        }

        public (bool status, string message, IIOCRegisterLoad load) CheckIfRegistered(IIOCKeyBase key, bool checkInParents = false)
        {
            //Check if it is registered as an UniversalSingleton Object at the root level.
            if (Root != null && !IsRoot) //If current container is root, then we will end up in a overflow loop.
            {
                var _rootLoad = Root.CheckIfRegistered(key);
                if (_rootLoad.status && _rootLoad.load.Mode == IOCRegisterMode.UniversalSingleton)
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
                return (true, message, current_load);
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
        public (bool status, string message, IIOCRegisterLoad load) CheckIfRegistered(Type contract_type, string priority_key, bool checkInParents = false)
        {
            return CheckIfRegistered(new KeyBase(contract_type, priority_key), checkInParents);
        }
        public (bool status, string message, IIOCRegisterLoad load) CheckIfRegistered<Tcontract>(string priority_key, bool checkInParents = false)
        {
            return CheckIfRegistered(typeof(Tcontract), priority_key, checkInParents);
        }
        #endregion
    }
}
