using System;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.IO;
using System.Windows;

namespace GameImageUtil
{
    /// <summary>
    /// App View Model
    /// </summary>
    public class AppViewModel : Notifiable
    {
        /// <summary>
        /// Gets or Sets the selected Processor
        /// </summary>
        public int SelectedProcessor
        {
            get
            {
                return GetValue<int>("SelectedProcessor");
            }
            set
            {
                CurrentSettings["SelectedProcessor"] = value.ToString();
                SetValue(value, "SelectedProcessor");
            }
        }

        /// <summary>
        /// Gets or Sets the selected Extension
        /// </summary>
        public int SelectedExtension
        {
            get
            {
                return GetValue<int>("SelectedExtension");
            }
            set
            {
                CurrentSettings["SelectedExtension"] = value.ToString();
                SetValue(value, "SelectedExtension");
                NotifyPropertyChanged("DXGIVisibility");
            }
        }

        /// <summary>
        /// Gets or Sets the selected DXGI Format
        /// </summary>
        public int SelectedDXGIFormat
        {
            get
            {
                return GetValue<int>("SelectedDXGIFormat");
            }
            set
            {
                CurrentSettings["SelectedDXGIFormat"] = value.ToString();
                SetValue(value, "SelectedDXGIFormat");
            }
        }

        /// <summary>
        /// Gets or Sets the number of processing threads
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return GetValue("ThreadCount", Environment.ProcessorCount);
            }
            set
            {
                CurrentSettings["ThreadCount"] = value.ToString();
                SetValue(value, "ThreadCount");
            }
        }

        /// <summary>
        /// Gets or Sets the DXGI Visibility
        /// </summary>
        public Visibility DXGIVisibility
        {
            get
            {
                return GetValue("DXGIVisibility", Extensions[SelectedExtension] == ".DDS" ? Visibility.Visible : Visibility.Hidden);
            }
            set
            {
                SetValue(value, "DXGIVisibility");
            }
        }

        /// <summary>
        /// Gets the File Processors
        /// </summary>
        public ObservableCollection<IFileProcessor> Processors { get; private set; }

        /// <summary>
        /// Gets the valid File Extensions for Read/Write
        /// </summary>
        public ObservableCollection<string> Extensions { get; private set; }

        /// <summary>
        /// Gets the valid DXGI Formats for DDS
        /// </summary>
        public ObservableCollection<string> DXGIFormats { get; private set; }

        /// <summary>
        /// Gets or Sets the Current Settings
        /// </summary>
        public Settings CurrentSettings { get; set; }

        /// <summary>
        /// Initializes the View Model
        /// </summary>
        public AppViewModel()
        {
            CurrentSettings = new Settings("data\\Settings.bcfg");
            
            Processors = new ObservableCollection<IFileProcessor>();
            Extensions = new ObservableCollection<string>()
            {
                ".PNG",
                ".TIF",
                ".TIFF",
                ".JPG",
                ".TGA",
                ".BMP",
                ".DDS",
            };
            DXGIFormats = new ObservableCollection<string>()
            {
                "DXT1",
                "DXT3",
                "DXT5",
                "BC4",
                "BC5",
            };

            LoadScripts("data\\scripts");

            var tempList = new List<IFileProcessor>();
            BuildTypeList(tempList);

            tempList.Sort((x, y) => x.Name.CompareTo(y.Name));

            foreach (var list in tempList)
                Processors.Add(list);

            SelectedProcessor  = int.TryParse(CurrentSettings["SelectedProcessor", Math.Max(FindProcessorIndex("AutomaticProcessor"), 0).ToString()], out var a) ? a : SelectedProcessor;
            SelectedExtension  = int.TryParse(CurrentSettings["SelectedExtension", "0"], out var b) ? b : SelectedExtension;
            SelectedDXGIFormat = int.TryParse(CurrentSettings["SelectedDXGIFormat", "0"], out var c) ? c : SelectedDXGIFormat;
            ThreadCount        = int.TryParse(CurrentSettings["ThreadCount", Environment.ProcessorCount.ToString()], out var d) ? d : ThreadCount;
        }

        private void LoadScripts(string folder)
        {
            try
            {
                foreach (var script in Directory.EnumerateFiles(folder, "*.cs_script", SearchOption.AllDirectories))
                {
                    try
                    {
                        var options = new CompilerParameters
                        {
                            GenerateExecutable = false,
                            GenerateInMemory = true
                        };

                        options.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
                        options.ReferencedAssemblies.Add("System.dll");
                        options.ReferencedAssemblies.Add("System.Linq.dll");
                        options.ReferencedAssemblies.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data\\Lib\\PhilLibX.dll"));
                        options.ReferencedAssemblies.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data\\Lib\\PhilLibX.Interop.dll"));
                        options.ReferencedAssemblies.Add("System.Numerics.dll");

                        var result = new CSharpCodeProvider().CompileAssemblyFromSource(options, File.ReadAllText(script));

                        if (result.Errors.HasErrors)
                            File.WriteAllText(script + ".errors.txt", result.Errors[0].ToString());
                        if (result.Errors.HasWarnings)
                            File.WriteAllText(script + ".warnings.txt", result.Errors[0].ToString());
                    }
                    catch (Exception e)
                    {
                        File.WriteAllText(script + ".fatal.txt", e.ToString());
                    }
                }
            }
#if DEBUG
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
#else
            catch { }
#endif
        }

        public static void BuildTypeList<T>(IList<T> list)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsInterface && !type.IsAbstract && typeof(T).IsAssignableFrom(type))
                    {
                        list.Add((T)Activator.CreateInstance(type));
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to locate the processor by ID
        /// </summary>
        /// <param name="id">ID to search for</param>
        /// <returns>Processor if found, otherwise null</returns>
        public IFileProcessor FindProcessor(string id)
        {
            id = id.ToLower();

            foreach (var processor in Processors)
                if (processor.ID.ToLower() == id)
                    return processor;

            return null;
        }

        /// <summary>
        /// Attempts to locate the processor by ID
        /// </summary>
        /// <param name="id">ID to search for</param>
        /// <returns>Processor index if found, otherwise -1</returns>
        public int FindProcessorIndex(string id)
        {
            id = id.ToLower();

            for(int i = 0; i < Processors.Count; i++)
                if (Processors[i].ID.ToLower() == id)
                    return i;

            return -1;
        }
    }
}
