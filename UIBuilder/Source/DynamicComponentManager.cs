// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

#if !_MOBILE_
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
using System.Diagnostics;
using Urho.IO;
using Urho.Gui;

namespace UIBuilder
{

    class DynamicComponentManager : Component
    {

        private static Assembly SystemRuntime = Assembly.Load(new AssemblyName("System.Runtime"));
        private static Assembly SystemCollectionsGeneric = Assembly.Load(new AssemblyName("System.Collections"));
        private static Assembly SystemLinq = Assembly.Load(new AssemblyName("System.Linq"));

        private static Assembly MSCoreLib = Assembly.Load(new AssemblyName("mscorlib"));
        private static Assembly UrhoDotNet = Assembly.Load(new AssemblyName("UrhoDotNet"));

        Dictionary<SyntaxTree, string> syntaxTreesToFilePathDictionary = new Dictionary<SyntaxTree, string>();
        // private static Assembly Test = Assembly.Load(new AssemblyName("Test"));
        Scene scene = null;

        Timer graceCompilationTimer = null;

        bool isFilesChanged = false;

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
                    if (isWindows == false)
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
            XmlFile uiStyle = Application.Current.ResourceCache.GetXmlFile("UI/DefaultStyle.xml");
            Application.Current.UI.Root.SetDefaultStyle(uiStyle);

            Component comp = CreateComponent("UIDynamicBuilder.cs", true);
            if (comp != null)
            {
                comp.Temporary = true;
                var child = scene.CreateChild("UIDynamicBuilderNode");
                child.Temporary = true;
                child.AddComponent(comp);
            }

            graceCompilationTimer = new Timer();

            ReceiveSceneUpdates = true;
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
                        var syntaxTree = diagnostic.Location.SourceTree;
                        if (syntaxTreesToFilePathDictionary.TryGetValue(syntaxTree, out string syntaxTreeFilePath))
                        {
                            Console.Error.WriteLine("{0}: {1} {2}: {3}", syntaxTreeFilePath, diagnostic.Location.GetLineSpan(), diagnostic.Id, diagnostic.GetMessage());
                        }
                        else
                        {
                            Console.Error.WriteLine("{0}: {1} {2}: {3}", fileName, diagnostic.Location.GetLineSpan(), diagnostic.Id, diagnostic.GetMessage());
                        }
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

            List<SyntaxTree> syntaxTrees = new List<SyntaxTree> ();
            GetSyntaxTreesForFiles(Directory.GetCurrentDirectory() + "/Source/HotReload", ref syntaxTrees,fileName);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(sourceCode));

            compilation = CSharpCompilation.Create(componentName, syntaxTrees,
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(SystemRuntime.Location),
                MetadataReference.CreateFromFile(SystemLinq.Location),
                MetadataReference.CreateFromFile(SystemCollectionsGeneric.Location),
                MetadataReference.CreateFromFile(MSCoreLib.Location),
                MetadataReference.CreateFromFile(UrhoDotNet.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            //
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

        private bool RemoveDynamicBuilderComponent()
        {
            bool result = false;

            Node node = scene.GetChild("UIDynamicBuilderNode");
            if (node != null)
            {
                var components = node.Components;
                foreach (Component component in components)
                {
                    var componentName = component.GetType().ToString();
                    if (componentName == "UIBuilder.UIDynamicBuilder")
                    {
                        node.RemoveComponent(component);
                        result = true;
                        break;
                    }
                }

                scene.RemoveChild(node);
            }
            return result;
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);

            if (isFilesChanged && graceCompilationTimer.GetMSec(false) > 150)
            {
                isFilesChanged = false;

                Log.Info("compiling files");

                RemoveDynamicBuilderComponent();

                Component comp = CreateComponent("UIDynamicBuilder.cs", true);
                if (comp != null)
                {
                    comp.Temporary = true;
                    var child = scene.CreateChild("UIDynamicBuilderNode");
                    child.Temporary = true;
                    child.AddComponent(comp);
                }

                graceCompilationTimer.Reset();
            }
        }

        //csc /target:library *.cs  /reference:../../References/UrhoDotNet.dll /platform:x64   /out:DynamicLib.dll
        private void OnFileChanged(FileChangedEventArgs args)
        {
            Log.Info("OnFileChanged :" + args.FileName);

            isFilesChanged = true;
            graceCompilationTimer.Reset();
            syntaxTreesToFilePathDictionary.Clear();

        }

        public void Recompile()
        {
            isFilesChanged = true;
            graceCompilationTimer.Reset();
            syntaxTreesToFilePathDictionary.Clear();
        }

        void GetSyntaxTreesForFiles(string sourceFilesLocation , ref List<SyntaxTree> trees ,string ignored_file = "")
        {
            DirectoryInfo d = new DirectoryInfo(sourceFilesLocation);
            string[] sourceFiles = d.EnumerateFiles("*.cs", SearchOption.AllDirectories)
                .Select(a => a.FullName).ToArray();

            foreach (string file in sourceFiles)
            {
                //avoid duplicate of the same file
                if(ignored_file != "" && file.Contains(ignored_file))continue;

                string code = System.IO.File.ReadAllText(file);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                tree.WithFilePath(file);
                trees.Add(tree);
                syntaxTreesToFilePathDictionary.Add(tree,file);
            }

        }

        
    }

}

#endif