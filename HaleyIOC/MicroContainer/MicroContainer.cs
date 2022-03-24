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
    public sealed partial class MicroContainer : IBaseContainer
    {
        #region ATTRIBUTES
        //All mappings for this container
        readonly ConcurrentDictionary<IKeyBase, RegisterLoad> Mappings = new ConcurrentDictionary<IKeyBase, RegisterLoad>();
        //created children
        readonly ConcurrentDictionary<string, IBaseContainer> ChildContainers = new ConcurrentDictionary<string, IBaseContainer>();
        IBaseContainer Parent;
        IBaseContainer Root;
        internal bool IsRoot = false; //Whenever the container is created using the "CreateChildContainerMode" it should not be a root.
        #endregion

        #region Properties
        public string Id { get;}
        public string Name { get; }
        public ExceptionHandling ErrorHandling { get; set; }
        #endregion

        #region Child Handling
        public IBaseContainer CreateChildContainer(string name = null)
        {
            var newContainer = new MicroContainer(name) { Parent = this, IsRoot = false };

            newContainer.Root = IsRoot? this:Root;//If you are creating this from inside a root container, then "this" is the root for the child else this container's root is also the root for the child.
            //Also add this to the child container dictionary

            if (!ChildContainers.TryAdd(newContainer.Id, newContainer))
            {
                //unable to create a child container.
                System.Diagnostics.Debug.WriteLine("Unable to create a child container");
                return null;
            }
            return newContainer;
        }
        public void Dispose()
        {
            //Cascade dispose through all child.
            foreach (var kvp in ChildContainers)
            {
                kvp.Value.Dispose();
            }

            //Parent and root remains the same.
            ChildContainers.Clear(); //All child container registrations will also be cleared.
            Mappings.Clear();
        }

        #endregion

        public MicroContainer() 
        {
            Id = Guid.NewGuid().ToString();
            ErrorHandling = ExceptionHandling.Throw;
            IsRoot = true; //Whenever we create a new microcontainer, that becomes a root. However, if created using "CreateChildContainer" method, that becomes a child.
            //Within the scope of this container , whomever tries to resolve, will get only this container.
            this.Register<IBaseContainer, MicroContainer>(this, SingletonMode.ContainerSingleton); //IHaleyContainer should not be an Universal Singleton as each scope will have their own container. 
        }
        [HaleyIgnore]
        public MicroContainer(string name) : this()
        {
            this.Name = name;
        }
    }
}
