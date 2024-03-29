﻿using System.ComponentModel.DataAnnotations;
using System.IO;
using LiteDB;

namespace FileExplorer.Persistence
{
    public class FolderLayout : PersistentItem
    {
        [Required]
        public string Name { get; set; }

        public string FolderPath { get; set; }

        public bool ApplyToSubFolders { get; set; }

        public byte[] LayoutData
        {
            get => layoutData;
            set
            {
                layoutData = value;
                layoutStream = new MemoryStream(layoutData);
            }
        }
        private byte[] layoutData;

        [BsonIgnore]
        public MemoryStream LayoutStream
        {
            get
            {
                if (layoutStream == null)
                    layoutStream = new MemoryStream(LayoutData);

                return layoutStream;
            }
        }
        private MemoryStream layoutStream;
    }
}
