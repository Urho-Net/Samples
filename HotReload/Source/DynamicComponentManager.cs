
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Urho;
using Urho.Resources;
using System.Runtime.Loader;
using System.Runtime.InteropServices;

namespace HotReload
{

    class DynamicComponentManager : Component
    {

        private static Assembly SystemRuntime = Assembly.Load(new AssemblyName("System.Runtime"));
        private static Assembly MSCoreLib = Assembly.Load(new AssemblyName("mscorlib"));
        private static Assembly UrhoDotNet = Assembly.Load(new AssemblyName("UrhoDotNet"));

        Scene scene = null;

        public class CollectibleAssemblyLoadContext : AssemblyLoadContext
        {
            public CollectibleAssemblyLoadContext() : base(isCollectible: true)
            { }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                return null;
            }
        }

        class DynamicComponentEntry
        {
            public string className = "";
            public CollectibleAssemblyLoadContext context;

            public Assembly assembly = null;
            public System.Type type = null;

            public void FreeResources()
            {
                try
                {
                    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    if(isWindows == false)
                    {
                      context?.Unload();
                    }
                    context = null;
                    assembly = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            public DynamicComponentEntry()
            {
                className = "";
                context = null;
                assembly = null;
                type = null;
            }

            public DynamicComponentEntry(string _className, CollectibleAssemblyLoadContext _context, Assembly _assembly, System.Type _type)
            {
                className = _className;
                context = _context;
                assembly = _assembly;
                type = _type;
            }
        }

        Dictionary<string, DynamicComponentEntry> componentsEntries = new Dictionary<string, DynamicComponentEntry>();

        public DynamicComponentManager(IntPtr handle) : base(handle) { }
        public DynamicComponentManager()
        {
            var cache = Application.ResourceCache;
            cache.FileChanged += OnFileChanged;
            cache.AutoReloadResources = true;
            scene = null;
        }

        public void SetScene(Scene _scene)
        {
            scene = _scene;
        }
        public void Stop()
        {

            foreach (var entry in componentsEntries)
            {
                entry.Value?.FreeResources();
            }

            componentsEntries.Clear();
        }

        public Component CreateComponent(string fileName, bool overwrite_existing = false)
        {

            Component instance = null;
            var cache = Application.ResourceCache;
            //  string fullPath = cache.GetResourceFileName(fileName);

            if (overwrite_existing == true)
            {
                CSharpCompilation compilation = Compile(fileName);
                instance = InstantiateComponent(compilation, fileName);
            }
            else
            {
                instance = InstantiateComponent(fileName);


                if (instance == null)
                {

                    CSharpCompilation compilation = Compile(fileName);

                    if (compilation != null)
                    {
                        instance = InstantiateComponent(compilation, fileName);
                    }
                }
            }

            return instance;
        }


        public bool RemoveComponent(string fileName)
        {
            bool res = false;
            res = componentsEntries.Remove(fileName);
            return res;
        }

        private Component InstantiateComponent(string fileName)
        {
            Component instance = null;

            DynamicComponentEntry entry = null;
            componentsEntries.TryGetValue(fileName, out entry);
            if (entry != null)
            {
                instance = Activator.CreateInstance(entry.type) as Component;
            }

            return instance;
        }

        private Component InstantiateComponent(Compilation compilation, string fileName)
        {
            var context = new CollectibleAssemblyLoadContext();
            Component instance = null;
            Assembly assembly = null;
            System.Type type = null;

            string componentName = fileName;
            int fileExtPos = fileName.LastIndexOf(".");
            if (fileExtPos >= 0)
                componentName = fileName.Substring(0, fileExtPos);

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1} {2}: {3}", fileName, diagnostic.Location.GetLineSpan(), diagnostic.Id, diagnostic.GetMessage());
                    }

                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);

                    assembly = context.LoadFromStream(ms);
                    Type[] types = assembly.GetTypes();
                    bool found_type = false;
                    foreach (Type t in types)
                    {
                        if (t.ToString().Contains(componentName))
                        {
                            type = t;
                            found_type = true;
                            break;
                        }

                    }

                    if (found_type == true)
                    {
                        instance = Activator.CreateInstance(type) as Component;
                        //dumpComponent(instance);
                        if (instance != null)
                        {
                            componentsEntries.Remove(fileName);
                            componentsEntries.Add(fileName, new DynamicComponentEntry(componentName, context, assembly, type));
                        }
                    }
                }
            }

            context.Unload();

            return instance;
        }

        private CSharpCompilation Compile(string fileName)
        {
            var cache = Application.ResourceCache;
            CSharpCompilation compilation = null;
            Urho.IO.File file = cache.GetFile(fileName);
            byte[] sourceBuffer = null;
            if (file != null)
            {
                uint size = file.Size;
                sourceBuffer = new byte[size];
                file.Read(sourceBuffer, size);
            }
            file.Dispose();

            string sourceCode = System.Text.Encoding.UTF8.GetString(sourceBuffer);

            string componentName = fileName;
            int fileExtPos = fileName.LastIndexOf(".");
            if (fileExtPos >= 0)
                componentName = fileName.Substring(0, fileExtPos);


            compilation = CSharpCompilation.Create(componentName, new[] { CSharpSyntaxTree.ParseText(sourceCode) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(SystemRuntime.Location),
                MetadataReference.CreateFromFile(MSCoreLib.Location),
                MetadataReference.CreateFromFile(UrhoDotNet.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }

        public void DumpComponents(Node node)
        {
            var components = node.Components;
            foreach (Component cmp in components)
            {
                Console.WriteLine("{0} {1} {2}", cmp.GetType(), cmp.Type, cmp.TypeName);
            }
        }

        public void DumpComponent(Component cmp)
        {
            Console.WriteLine("{0} {1} {2}", cmp.GetType(), cmp.Type, cmp.TypeName);
        }

        // TBD ELI ,  this is a temporary implemnation 
        private Node[] GetChildrenWithComponent(System.Type type)
        {
            List<Node> children = new List<Node>();

            if (scene != null)
            {
                var nodes = scene.Children;
                foreach (Node node in nodes)
                {
                    var components = node.Components;
                    foreach (Component component in components)
                    {
                        if (component.GetType().ToString() == type.ToString())
                        {
                            children.Add(node);
                        }
                    }
                }
            }

            return children.ToArray();

        }

        private bool RemoveComponent(Node node, System.Type type)
        {
            bool result = false;
            var components = node.Components;
            foreach (Component component in components)
            {
                if (component.GetType().ToString() == type.ToString())
                {
                    node.RemoveComponent(component);
                    result = true;
                    break;
                }
            }
            return result;
        }

        private void OnFileChanged(FileChangedEventArgs args)
        {
            string resourceName = args.ResourceName;
            Component instance = CreateComponent(resourceName, true);

            // compilation succeeded  so replace the components on the nodes
            if (instance != null)
            {
                System.Type type = instance.GetType();
                Node[] nodes = GetChildrenWithComponent(instance.GetType());
                foreach (Node node in nodes)
                {
                    RemoveComponent(node, instance.GetType());
                    Component comp = CreateComponent(resourceName);
                    if (comp != null)
                    {
                        node.AddComponent(comp);
                    }
                }
            }
        }
    }

}