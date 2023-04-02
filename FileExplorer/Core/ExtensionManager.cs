using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FileExplorer.Extension;
using FileExplorer.Persistence;

namespace FileExplorer.Core
{
    public sealed class ExtensionManager
    {
        #region Singleton

        public static ExtensionManager Instance {get; private set;}

        static ExtensionManager()
        {
            Instance = new ExtensionManager();
        }

        private ExtensionManager() 
        {
            ExtensionAssemblies = Directory.GetFiles(ExtensionDirectory, "*.dll", SearchOption.AllDirectories);

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string fileName = new AssemblyName(e.Name).Name + ".dll";

                string file = ExtensionAssemblies.FirstOrDefault(x => x.OrdinalEquals(fileName));
                if (file != null)
                    return Assembly.LoadFile(file);

                return null;
            };

            LoadExtensions();
        }

        private static string[] ExtensionAssemblies;

        #endregion

        [ImportMany]
        public IEnumerable<ExportFactory<IPreviewExtension, IPreviewExtensionMetadata>> Extensions { get; private set; }

        public static readonly string ExtensionDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PreviewExtensions");

        private void LoadExtensions()
        {
            if (Directory.Exists(ExtensionDirectory))
            {
                AggregateCatalog aggregateCatalog = new AggregateCatalog();
                DummyExtensionLoader dummyExtensionLoader = new DummyExtensionLoader();

                foreach (string assemblyFile in Directory.EnumerateFiles(ExtensionDirectory, "FileExplorer.Extension.*.dll", SearchOption.AllDirectories))
                {
                    string assemblyName = Path.GetFileNameWithoutExtension(assemblyFile);
                    ExtensionMetadata extensionMetadata = App.Repository.Extensions.FirstOrDefault(x => x.AssemblyName == assemblyName);

                    try
                    {
                        AssemblyCatalog assemblyCatalog = new AssemblyCatalog(assemblyFile);
                        CompositionContainer compositionContainer = new CompositionContainer(assemblyCatalog);
                        compositionContainer.ComposeParts(dummyExtensionLoader);

                        if (extensionMetadata == null)
                        {
                            extensionMetadata = new ExtensionMetadata(dummyExtensionLoader.Extension.Metadata);
                            extensionMetadata.AssemblyName = assemblyName;

                            App.Repository.Extensions.Add(extensionMetadata);
                        }
                        else
                        {
                            extensionMetadata.Version = dummyExtensionLoader.Extension.Metadata.Version;
                            extensionMetadata.SupportedFileTypes = dummyExtensionLoader.Extension.Metadata.SupportedFileTypes;
                            extensionMetadata.Error = String.Empty;

                            App.Repository.Extensions.Update(extensionMetadata);
                        }

                        aggregateCatalog.Catalogs.Add(assemblyCatalog);
                    }
                    catch (ReflectionTypeLoadException exception)
                    {
                        Journal.WriteLog(exception);
                        StringBuilder error = new StringBuilder(exception.Message); 

                        if (exception.LoaderExceptions != null)
                        {
                            foreach (Exception loaderException in exception.LoaderExceptions)
                            {
                                Journal.WriteLog(loaderException);
                                error.AppendLine(loaderException.Message);
                            }
                        }

                        if (extensionMetadata == null)
                        {
                            extensionMetadata = new ExtensionMetadata { AssemblyName = assemblyName, DisplayName = assemblyName, Disabled = true, Error = error.ToString() };
                            App.Repository.Extensions.Add(extensionMetadata);
                        }
                        else
                        {
                            extensionMetadata.Disabled = true;
                            extensionMetadata.Error = error.ToString();
                            App.Repository.Extensions.Update(extensionMetadata);
                        }

                    }
                    catch (Exception exception)
                    {
                        Journal.WriteLog(exception);

                        if (extensionMetadata == null)
                        {
                            extensionMetadata = new ExtensionMetadata { AssemblyName = assemblyName, DisplayName = assemblyName, Disabled = true, Error = exception.Message };
                            App.Repository.Extensions.Add(extensionMetadata);
                        }
                        else
                        {
                            extensionMetadata.Disabled = true;
                            extensionMetadata.Error = exception.Message;
                            App.Repository.Extensions.Update(extensionMetadata);
                        }
                    }
                }

                CompositionContainer aggregateContainer = new CompositionContainer(aggregateCatalog);
                aggregateContainer.ComposeParts(this);

                foreach (ExtensionMetadata extensionMetadata in App.Repository.Extensions.Where(x => !x.Disabled))
                {
                    if (!Extensions.Any(x => x.Metadata.DisplayName == extensionMetadata.DisplayName))
                    {
                        extensionMetadata.Disabled = true;
                        extensionMetadata.Error = $"Assembly Not Found: {extensionMetadata.AssemblyName}";

                        App.Repository.Extensions.Update(extensionMetadata);    
                    }
                }
            }
        }

        private class DummyExtensionLoader
        {
            [Import]
            public ExportFactory<IPreviewExtension, IPreviewExtensionMetadata> Extension { get; set; }
        }
    }
}
