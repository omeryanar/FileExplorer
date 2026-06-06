using System;
using System.IO;
using LiteDB;

namespace FileExplorer.Persistence
{
    public class Repository
    {
        public LiteDatabase Database { get; private set; }

		public PersistentCollection<MenuItem> MenuItems { get; private set; }

        public PersistentCollection<Expression> Expressions { get; private set; }

        public PersistentCollection<FolderLayout> FolderLayouts { get; private set; }

        public PersistentCollection<ExtensionMetadata> Extensions { get; private set; }

        public Repository(string databaseName)
        {
            string connectionString = $"Filename={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, databaseName)}; Upgrade=true";
            Database = new LiteDatabase(connectionString);

			MenuItems = new PersistentCollection<MenuItem>(Database, "MenuItems");
            Expressions = new PersistentCollection<Expression>(Database, "Expressions");
            FolderLayouts = new PersistentCollection<FolderLayout>(Database, "FolderLayouts");
            Extensions = new PersistentCollection<ExtensionMetadata>(Database, "Extensions");
        }
	}
}
