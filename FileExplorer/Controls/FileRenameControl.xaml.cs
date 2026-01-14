using System;
using System.Collections.Generic;
using DevExpress.Xpf.Grid;
using FileExplorer.Core;
using FileExplorer.Model;

namespace FileExplorer.Controls
{
    public partial class FileRenameControl : GridControl
    {
        private HashSet<Int32> ManuallyEditedRowIndexList = new HashSet<Int32>();

        public FileRenameControl()
        {
            InitializeComponent();

            CustomColumnSort += (s, e) =>
            {
                this.NaturalSort(e, false);
            };

            CustomUnboundColumnData += (s, e) =>
            {
                if (e.IsGetData && e.Column.FieldName == "RowNumber")
                    e.Value = e.Source.GetRowHandleByListIndex(e.ListSourceRowIndex) + 1;
                else if (e.Column.FieldName == "NewName")
                {
                    FileModel fileModel = GetRowByListIndex(e.ListSourceRowIndex) as FileModel;
                    if (fileModel != null)
                    {
                        if (e.IsSetData)
                        {
                            if (!ManuallyEditedRowIndexList.Contains(e.ListSourceRowIndex))
                                ManuallyEditedRowIndexList.Add(e.ListSourceRowIndex);

                            fileModel.Tag = e.Value;
                        }
                        else if (e.IsGetData)
                        {
                            if (ManuallyEditedRowIndexList.Contains(e.ListSourceRowIndex))
                                e.Value = fileModel.Tag;
                            else
                                fileModel.Tag = e.Value;
                        }
                    }
                }
            };
        }

        private void OnValidate(object sender, GridCellValidationEventArgs e)
        {
            if (e.Column.FieldName == "NewName" && e.CellValue != null)
            {
                string newName = e.CellValue.ToString();
                if (newName.ContainsInvalidFileNameCharacters())
                    e.SetError(Properties.Resources.InvalidFileNameMessage);
            }
        }
    }
}
