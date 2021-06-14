using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FileExplorer.Helpers
{
    public sealed class LockProvider
    {
        public IDisposable Lock(object key)
        {
            GetOrCreate(key).Wait();
            return new Releaser { Key = key };
        }

        public async Task<IDisposable> LockAsync(object key)
        {
            await GetOrCreate(key).WaitAsync().ConfigureAwait(false);
            return new Releaser { Key = key };
        }
        
        private static readonly Dictionary<object, RefCounted<SemaphoreSlim>> Semaphores = new Dictionary<object, RefCounted<SemaphoreSlim>>();

        private SemaphoreSlim GetOrCreate(object key)
        {
            RefCounted<SemaphoreSlim> item;
            lock (Semaphores)
            {
                if (Semaphores.TryGetValue(key, out item))
                {
                    ++item.RefCount;
                }
                else
                {
                    item = new RefCounted<SemaphoreSlim>(new SemaphoreSlim(1, 1));
                    Semaphores[key] = item;
                }
            }
            return item.Value;
        }

        private sealed class RefCounted<T>
        {
            public RefCounted(T value)
            {
                RefCount = 1;
                Value = value;
            }

            public int RefCount { get; set; }
            public T Value { get; private set; }
        }

        private sealed class Releaser : IDisposable
        {
            public object Key { get; set; }

            public void Dispose()
            {
                RefCounted<SemaphoreSlim> item;
                lock (Semaphores)
                {
                    item = Semaphores[Key];
                    --item.RefCount;
                    if (item.RefCount == 0)
                        Semaphores.Remove(Key);
                }
                item.Value.Release();
            }
        }
    }
}