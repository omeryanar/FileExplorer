using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace FileExplorer.Model
{
    public class FileModelReadOnlyCollection : IReadOnlyCollection<FileModel>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        #region Fields

        private List<FileModel> InnerList = new List<FileModel>();

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

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

        public int Count => InnerList.Count;

        public FileModelReadOnlyCollection(params FileModelCollection[] collections)
        {
            foreach (FileModelCollection collection in collections)
            {
                InnerList.AddRange(collection);
                
                collection.CollectionChanged += (s, e) =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            int count = Count;
                            InnerList.AddRange(e.NewItems.OfType<FileModel>());
                            RaiseCollectionChanged(e.Action, e.NewItems, count);
                            break;

                        case NotifyCollectionChangedAction.Remove:
                            foreach (FileModel item in e.OldItems)
                            {
                                int index = InnerList.FindIndex(x => x.FullPath == item.FullPath);
                                if (index != -1)
                                {
                                    InnerList.RemoveAt(index);
                                    RaiseCollectionChanged(e.Action, item, index);
                                }
                            }                            
                            break;

                        case NotifyCollectionChangedAction.Reset:
                            RaiseCollectionChanged(e.Action, null, -1);
                            break;
                    }
                };
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
