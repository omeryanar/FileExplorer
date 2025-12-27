using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace FileExplorer.Model
{
    public class FileModelCollectionView : IReadOnlyCollection<FileModel>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        #region Fields

        private List<FileModel> InnerList;

        private Func<FileModel, bool> Filter;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region IEnumerable

        public IEnumerator<FileModel> GetEnumerator()
        {
            return Filter == null ? InnerList.GetEnumerator() : InnerList.Where(Filter).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Filter == null ? InnerList.GetEnumerator() : InnerList.Where(Filter).GetEnumerator();
        }

        #endregion

        #region IReadOnlyCollection

        public int Count => Filter == null ? InnerList.Count : InnerList.Count(Filter);

        #endregion

        public FileModelCollectionView(FileModelCollection collection, Func<FileModel, bool> filter = null)
        {
            InnerList = new List<FileModel>(collection);
            Filter = filter;

            collection.CollectionChanged += (s, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        Add(e.NewItems, filter);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        Remove(e.OldItems, filter);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        RaiseCollectionChanged(e.Action, null, -1);
                        break;
                }
            };
        }

        private void Add(IList list, Func<FileModel, bool> filter = null)
        {
            List<FileModel> items;
            if (filter == null)
                items = list.OfType<FileModel>().ToList();
            else
                items = list.OfType<FileModel>().Where(filter).ToList();

            if (items.Count > 0)
            {
                int index = Count;
                InnerList.AddRange(items);
                RaiseCollectionChanged(NotifyCollectionChangedAction.Add, items, index);
            }
        }

        private void Remove(IList list, Func<FileModel, bool> filter = null)
        {
            List<FileModel> items;
            if (filter == null)
                items = list.OfType<FileModel>().ToList();
            else
                items = list.OfType<FileModel>().Where(filter).ToList();

            foreach (FileModel item in items)
            {
                int index = InnerList.FindIndex(x => x.FullPath == item.FullPath);
                if (index != -1)
                {
                    InnerList.RemoveAt(index);
                    RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
                }
            }
        }

        private void RaiseCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index));
        }

        private void RaiseCollectionChanged(NotifyCollectionChangedAction action, IList items, int index)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, items, index));
        }
    }
}
