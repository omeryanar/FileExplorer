using System;
using System.IO;
using LiteDB;

namespace FileExplorer.Persistence
{
    public class Repository
    {
        public PersistentCollection<MenuItem> MenuItems { get; private set; }

        public PersistentCollection<Expression> Expressions { get; private set; }

        public PersistentCollection<FolderLayout> FolderLayouts { get; private set; }

        public Repository(string databaseName)
        {
            LiteDatabase database = new LiteDatabase(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, databaseName));

            MenuItems = new PersistentCollection<MenuItem>(database, "MenuItems");
            Expressions = new PersistentCollection<Expression>(database, "Expressions");
            FolderLayouts = new PersistentCollection<FolderLayout>(database, "FolderLayouts");
        }
    }
}
