using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.core
{
    public class T1ModuleInfo
    {
        /// <summary>
        /// The assembly which contains the module definition.
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// Type of the module.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Instance of the module.
        /// </summary>
        public T1Module Instance { get; }

        /// <summary>
        /// Is this module loaded as a plugin.
        /// </summary>
        public bool IsLoadedAsPlugIn { get; }

        /// <summary>
        /// All dependent modules of this module.
        /// </summary>
        public List<T1ModuleInfo> Dependencies { get; }

        public T1ModuleInfo([NotNull] Type type, [NotNull] T1Module instance, bool isLoadedAsPlugIn)
        {
            Type = type;
            Instance = instance;
            IsLoadedAsPlugIn = isLoadedAsPlugIn;
            Assembly = Type.GetTypeInfo().Assembly;

            Dependencies = new List<T1ModuleInfo>();
        }

    }
}
