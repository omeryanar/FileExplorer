using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Mvvm;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Messages;
using FileExplorer.Model;
using LiteDB;

namespace FileExplorer.Persistence
{
    public class MenuItem : PersistentItem
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public CommandType Command
        {
            get { return command; }
            set
            {
                if (command != value)
                {
                    command = value;
                    RaisePropertyChanged(nameof(Command));
                }
            }
        }
        private CommandType command;

        [Required]
        public ParameterType Parameter
        {
            get { return parameter; }
            set
            {
                if (parameter != value)
                {
                    parameter = value;
                    RaisePropertyChanged(nameof(Parameter));
                }
            }
        }
        private ParameterType parameter;

        public string Application { get; set; }

        public string GroupName { get; set; }

        public string Expression { get; set; }

        public string Prefix { get; set; }

        public string Suffix { get; set; }

        public string Shortcut
        {
            get { return shortcut; }
            set
            {
                shortcut = value == null ? String.Empty : value;
            }
        }
        private string shortcut;

        public string ExtensionFilter { get; set; }

        public bool ConfirmBeforeRun { get; set; }

        public ItemTypeFilter ItemTypeFilter { get; set; }

        public SelectionFilter SelectionFilter { get; set; }

        [BsonIgnore]
        public ImageSource Icon
        {
            get
            {
                switch (Command)
                {
                    case CommandType.Open:
                        return Open;
                    case CommandType.OpenInNewTab:
                        return OpenInNewTab;
                    case CommandType.OpenInNewWindow:
                        return OpenInNewWindow;
                    case CommandType.OpenWithApplication:
                        return FileSystemImageHelper.GetImage(Application, IconSize.Small);
                }

                return FileSystemImageHelper.GetImage(Application, IconSize.Small);
            }
        }

        [BsonIgnore]
        public ICommand ExecuteCommand
        {
            get
            {
                if (executeCommand == null)
                    executeCommand = new DelegateCommand<IList<object>>(Execute, CanExecute);

                return executeCommand;
            }
        }
        private ICommand executeCommand;

        [BsonIgnore]
        public PropertyDescriptorCollection Properties
        {
            get
            {
                if (properties == null)
                    properties = TypeDescriptor.GetProperties(typeof(FileModel));

                return properties;
            }
        }
        private PropertyDescriptorCollection properties;

        [BsonIgnore]
        protected static ImageSource Open = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/ICO/Open.ico"));

        [BsonIgnore]
        protected static ImageSource OpenInNewTab = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/ICO/OpenFolder.ico"));

        [BsonIgnore]
        protected static ImageSource OpenInNewWindow = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/ICO/OpenNewWindow.ico"));

        public bool CanExecute(IList<object> items)
        {
            if (items == null || items.Count == 0)
                return false;

            if (items.Count > 1 && SelectionFilter == SelectionFilter.Single)
                return false;

            if (items.Count == 1 && SelectionFilter == SelectionFilter.Multiple)
                return false;

            if (items.OfType<FileModel>().Any(x => x.IsDirectory) && ItemTypeFilter == ItemTypeFilter.File)
                return false;

            if (items.OfType<FileModel>().Any(x => !x.IsDirectory) && ItemTypeFilter == ItemTypeFilter.Folder)
                return false;

            if (items.OfType<FileModel>().Any(x => ExtensionFilter != null && ExtensionFilter.Split('|').Any(y => x.FullName.OrdinalEndsWith(y)) == false))
                return false;

            return true;
        }

        public void Execute(IList<object> items)
        {
            List<string> parameters = new List<string>();
            List<FileModel> files = items.OfType<FileModel>().ToList();
            string directory = files.FirstOrDefault()?.ParentPath;

            if (Parameter == ParameterType.Name)
                parameters = files.Select(x => String.Format("\"{0}\"", x.FullName)).ToList();
            else if (Parameter == ParameterType.Path)
                parameters = files.Select(x => String.Format("\"{0}\"", x.FullPath)).ToList();
            else if (!String.IsNullOrEmpty(Expression))
            {
                List<FileModel> expressionResults = new List<FileModel>();
                foreach (FileModel file in files)
                {
                    object result = new ExpressionEvaluator(Properties, CriteriaOperator.Parse(Expression)).Evaluate(file);
                    if (result != null)
                    {
                        parameters.Add(result.ToString());

                        string parsingName = FileSystemHelper.GetFileParsingName(result.ToString());
                        if (!String.IsNullOrEmpty(parsingName))
                            expressionResults.Add(FileModel.FromPath(parsingName));
                    }
                }
                files = expressionResults;
            }

            CommandMessage message = new CommandMessage
            {
                Arguments = $"{Prefix}{parameters.Join(" ")}{Suffix}",
                Directory = directory,
                Parameters = files,
                MenuItem = this
            };

            Messenger.Default.Send(message);
        }

        public override string ToString()
        {
            return Name;
        }

        public MenuItem()
        {
            Name = FileExplorer.Properties.Resources.NewMenuItem;
            Shortcut = String.Empty;
            Application = Utilities.AppPath;
        }
    }
}
