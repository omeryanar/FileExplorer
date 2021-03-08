using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FileExplorer.Model
{
    public class FileModelCollection : ICollection<FileModel>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        #region Fields

        private List<FileModel> InnerList;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region Properties

        public bool IsReadOnly { get { return false; } }

        public int Count { get { return InnerList.Count; } }

        #endregion

        #region Constructors

        public FileModelCollection()
        {
            InnerList = new List<FileModel>();
        }

        public FileModelCollection(IEnumerable<FileModel> items)
        {
            InnerList = new List<FileModel>(items);
        }

        #endregion

        #region IEnumerable

        public IEnumerator<FileModel> GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }

        #endregion

        public void Add(FileModel item)
        {
            int index = Count;
            InnerList.Add(item);            
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void Insert(int index, FileModel item)
        {
            InnerList.Insert(index, item);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public bool Remove(FileModel item)
        {
            int index = InnerList.IndexOf(item);
            if (index >= 0 && InnerList.Remove(item))
            {
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                return true;
            }

            return false;
        }

        public void AddRange(IEnumerable<FileModel> items)
        {
            int index = Count;
            List<FileModel> newItems = new List<FileModel>(items);
            if (newItems.Count == 0)
                return;

            InnerList.AddRange(newItems);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, index));
        }

        public void RemoveRange(int index, int count)
        {
            if (count == 0 || Count <= index + count)
                return;

            List<FileModel> oldItems = new List<FileModel>(count);
            for (int i = index; i < count; i++)
                oldItems.Add(InnerList[i]);
            
            InnerList.RemoveRange(index, count);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index));
        }

        public void Clear()
        {
            if (Count == 0)
                return;

            InnerList.Clear();
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(FileModel item)
        {
            return InnerList.Contains(item);
        }

        public void CopyTo(FileModel[] array, int arrayIndex)
        {
            for (int i = arrayIndex; i < InnerList.Count; i++)
                array[i] = InnerList[i];
        }

        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Move && e.Action != NotifyCollectionChangedAction.Replace)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            CollectionChanged?.Invoke(this, e);
        }
    }
}
