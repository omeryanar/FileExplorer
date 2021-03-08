using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using LiteDB;

namespace FileExplorer.Persistence
{
    public abstract class PersistentItem : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PersistentCollection<T> : ObservableCollection<T> where T : PersistentItem
    {
        public PersistentCollection(LiteDatabase database, string collectionName)
            : base(database.GetCollection<T>(collectionName).FindAll())
        {
            Repository = database.GetCollection<T>(collectionName);
        }

        public event EventHandler<T> ItemUpdated;

        public void Update(T item)
        {
            Repository.Update(item);
            ItemUpdated?.Invoke(this, item);
        }

        public void Duplicate(T item)
        {
            T copy = Repository.FindById(item.Id);
            copy.Id = 0;
            Add(copy);
        }

        protected override void InsertItem(int index, T item)
        {
            if (!Contains(item))
                base.InsertItem(index, item);

            Repository.Upsert(item);
        }

        protected override void RemoveItem(int index)
        {
            T item = this[index];
            base.RemoveItem(index);

            Repository.Delete(item.Id);
        }

        private LiteCollection<T> Repository;
    }
}
