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
using System.Diagnostics;

namespace Haley.IOC
{
    public sealed partial class MicroContainer
    {
        #region ATTRIBUTES
        readonly ConcurrentDictionary<string, IMicroContainer> ChildContainers = new ConcurrentDictionary<string, IMicroContainer>();
        internal bool IsRoot = false; //Whenever the container is created using the "CreateChildContainerMode" it should not be a root.

        #endregion

        public event EventHandler<IMicroContainer> ChildContainerCreated;

        #region Properties
        public IMicroContainer Parent { get; private set; }
        public IMicroContainer Root { get; private set; }
        public bool IgnoreParentContainer { get; private set; }
        #endregion

        /// <summary>
        /// Gets the top level child container
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IMicroContainer GetChild(string id, bool search_all_children = false) {
            if (string.IsNullOrWhiteSpace(id) || ChildContainers == null || ChildContainers?.Count == 0) return null;
            IMicroContainer container = null;
            if (!ChildContainers.TryGetValue(id, out container) && search_all_children) {
                //We were unable to find a value with the provided id in the childcontainers of this level. 
                //We also have the search_all_children set to true.
                foreach (var child in ChildContainers.Values) {
                    container = child.GetChild(id,search_all_children);
                    if (container != null) return container;
                }
            }
            return container;
        }

        public IMicroContainer this[string id] {
            get {
                return GetChild(id);
            }
        }
        public IMicroContainer CreateChild(Guid id = default, bool ignore_parentcontainer = false) {
            return CreateChild(id, null, ignore_parentcontainer);
        }

        public IMicroContainer CreateChild(string name, bool ignore_parentcontainer = false) {
            return CreateChild(default(Guid), name, ignore_parentcontainer);
        }

        public IMicroContainer CreateChild() {
            return CreateChild(default(Guid), null, false);
        }

        public IMicroContainer CreateChild(Guid id, string name, bool ignore_parentcontainer) {
            MicroContainer result = null;
            try {
                //Default GUID will be empty.
                if (id == default(Guid)) {
                    id = Guid.NewGuid(); //Assign a new GUID
                }

                result = new MicroContainer(id, name) { Parent = this, IsRoot = false, IgnoreParentContainer = ignore_parentcontainer };

                result.Root = IsRoot ? this : Root;//If you are creating this from inside a root container, then "this" is the root for the child else this container's root is also the root for the child.

                if (!ChildContainers.TryAdd(result.Id, result)) {
                    //unable to create a child container.
                    System.Diagnostics.Debug.WriteLine("Unable to create a child container");
                    return null;
                }
                ChildContainerCreated?.Invoke(this, result); //So, if there are any other actions to be done by others, can happen based on this event.
                return result;
            } catch {
                return result;
            }
        }
    }
}
