using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using FileExplorer.Core;

namespace FileExplorer.ViewModel
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class RenameViewModel
    {
        [Required]
        [RegularExpression(@"^[^\/<>*?:""|\\]*$")]
        public virtual string Name { get; set; }

        public virtual string Parent { get; set; }

        [RegularExpression(@"^[^\/<>*?:""|\\]*$")]
        public virtual string Extension { get; set; }

        public virtual DateTime DateCreated { get; set; }

        public virtual DateTime DateModified { get; set; }

        public virtual DateTime DateAccessed { get; set; }

        public virtual bool ShowPatternButtons { get; set; }

        public virtual bool ShowAdvancedOptions { get; set; }

        public List<UICommand> UICommandList { get; private set; }

        public RenameViewModel()
        {
            UICommandList = new List<UICommand>
            {
                new UICommand
                {
                    Caption = Properties.Resources.OK,
                    Id = MessageBoxResult.OK,
                    IsDefault = true,
                    IsCancel = false,
                    Command = new DelegateCommand(() => { }, () => IsValid())
                },
                new UICommand
                {
                    Caption = Properties.Resources.Cancel,
                    Id = MessageBoxResult.Cancel,
                    IsDefault = false,
                    IsCancel = true
                }
            };
        }

        public void ChangeFormat(string formatType)
        {
            switch (formatType)
            {
                case "Aa Aa":
                    Name = Name.TitleCase();
                    break;
                case "AA AA":
                    Name = Name.ToUpperInvariant();
                    break;
                case "Aa aa":
                    Name = Name.SentenceCase();
                    break;
                case "aa aa":
                    Name = Name.ToLowerInvariant();
                    break;
                case "aA aA":
                    Name = Name.ToggleCase();
                    break;
                case "Parent":
                    Name = Parent;
                    break;
            }
        }

        private bool IsValid()
        {
            if (this is IDataErrorInfo dataErrorInfo)
                return String.IsNullOrEmpty(dataErrorInfo[nameof(Name)]) && String.IsNullOrEmpty(dataErrorInfo[nameof(Extension)]);

            return true;
        }
    }
}
