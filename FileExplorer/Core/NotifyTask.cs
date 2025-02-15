using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace FileExplorer.Core
{
    public sealed class NotifyTask : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a task notifier watching the specified task.
        /// </summary>
        /// <param name="task">The task to watch.</param>
        private NotifyTask(Task task)
        {
            Task = task;
            TaskCompleted = MonitorTaskAsync(task);
        }

        private async Task MonitorTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // Ignore exceptions
            }
            finally
            {
                NotifyProperties(task);
            }
        }

        private void NotifyProperties(Task task)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;

            if (task.IsCanceled)
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsCanceled)));
            }
            else if (task.IsFaulted)
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Exception)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(InnerException)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(ErrorMessage)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsFaulted)));
            }
            else
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsSuccessfullyCompleted)));
            }
            propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsCompleted)));
            propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsNotCompleted)));
        }

        /// <summary>
        /// Gets the task being watched. This property never changes and is never <c>null</c>.
        /// </summary>
        public Task Task { get; }

        /// <summary>
        /// Gets a task that completes successfully when <see cref="Task"/> completes (successfully, faulted, or canceled). This property never changes and is never <c>null</c>.
        /// </summary>
        public Task TaskCompleted { get; }

        /// <summary>
        /// Gets the current task status. This property raises a notification when the task completes.
        /// </summary>
        public TaskStatus Status => Task.Status;

        /// <summary>
        /// Gets whether the task has completed. This property raises a notification when the value changes to <c>true</c>.
        /// </summary>
        public bool IsCompleted => Task.IsCompleted;

        /// <summary>
        /// Gets whether the task is busy (not completed). This property raises a notification when the value changes to <c>false</c>.
        /// </summary>
        public bool IsNotCompleted => !Task.IsCompleted;

        /// <summary>
        /// Gets whether the task has completed successfully. This property raises a notification when the value changes to <c>true</c>.
        /// </summary>
        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        /// <summary>
        /// Gets whether the task has been canceled. This property raises a notification only if the task is canceled (i.e., if the value changes to <c>true</c>).
        /// </summary>
        public bool IsCanceled => Task.IsCanceled;

        /// <summary>
        /// Gets whether the task has faulted. This property raises a notification only if the task faults (i.e., if the value changes to <c>true</c>).
        /// </summary>
        public bool IsFaulted => Task.IsFaulted;

        /// <summary>
        /// Gets the wrapped faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
        /// </summary>
        public AggregateException Exception => Task.Exception;

        /// <summary>
        /// Gets the original faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
        /// </summary>
        public Exception InnerException => Exception?.InnerException;

        /// <summary>
        /// Gets the error message for the original faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
        /// </summary>
        public string ErrorMessage => InnerException?.Message;

        /// <summary>
        /// Event that notifies listeners of property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Creates a new task notifier watching the specified task.
        /// </summary>
        /// <param name="task">The task to watch.</param>
        public static NotifyTask Create(Task task)
        {
            return new(task);
        }

        /// <summary>
        /// Creates a new task notifier watching the specified task.
        /// </summary>
        /// <typeparam name="TResult">The type of the task result.</typeparam>
        /// <param name="task">The task to watch.</param>
        /// <param name="defaultResult">The default "result" value for the task while it is not yet complete.</param>
        public static NotifyTask<TResult> Create<TResult>(Task<TResult> task, TResult defaultResult = default!)
        {
            return new(task, defaultResult);
        }

        /// <summary>
        /// Executes the specified asynchronous code and creates a new task notifier watching the returned task.
        /// </summary>
        /// <param name="asyncAction">The asynchronous code to execute.</param>
        public static NotifyTask Create(Func<Task> asyncAction)
        {
            _ = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
            return Create(asyncAction());
        }

        /// <summary>
        /// Executes the specified asynchronous code and creates a new task notifier watching the returned task.
        /// </summary>
        /// <param name="asyncAction">The asynchronous code to execute.</param>
        /// <param name="defaultResult">The default "result" value for the task while it is not yet complete.</param>
        public static NotifyTask<TResult> Create<TResult>(Func<Task<TResult>> asyncAction, TResult defaultResult = default!)
        {
            _ = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
            return Create(asyncAction(), defaultResult);
        }
    }

    public sealed class NotifyTask<TResult> : INotifyPropertyChanged
    {
        /// <summary>
        /// The "result" of the task when it has not yet completed.
        /// </summary>
        private readonly TResult _defaultResult;

        /// <summary>
        /// Initializes a task notifier watching the specified task.
        /// </summary>
        /// <param name="task">The task to watch.</param>
        /// <param name="defaultResult">The value to return from <see cref="Result"/> while the task is not yet complete.</param>
        internal NotifyTask(Task<TResult> task, TResult defaultResult)
        {
            _defaultResult = defaultResult;
            Task = task;
            TaskCompleted = MonitorTaskAsync(task);
        }

        private async Task MonitorTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // Ignore exceptions.
            }
            finally
            {
                NotifyProperties(task);
            }
        }

        private void NotifyProperties(Task task)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;

            if (task.IsCanceled)
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsCanceled)));
            }
            else if (task.IsFaulted)
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Exception)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(InnerException)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(ErrorMessage)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsFaulted)));
            }
            else
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Result)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsSuccessfullyCompleted)));
            }
            propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsCompleted)));
            propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsNotCompleted)));
        }

        /// <summary>
        /// Gets the task being watched. This property never changes and is never <c>null</c>.
        /// </summary>
        public Task<TResult> Task { get; }

        /// <summary>
        /// Gets a task that completes successfully when <see cref="Task"/> completes (successfully, faulted, or canceled). This property never changes and is never <c>null</c>.
        /// </summary>
        public Task TaskCompleted { get; }

        /// <summary>
        /// Gets the result of the task. Returns the "default result" value specified in the constructor if the task has not yet completed successfully. This property raises a notification when the task completes successfully.
        /// </summary>
        public TResult Result => (Task.Status == TaskStatus.RanToCompletion) ? Task.Result : _defaultResult;

        /// <summary>
        /// Gets the current task status. This property raises a notification when the task completes.
        /// </summary>
        public TaskStatus Status => Task.Status;

        /// <summary>
        /// Gets whether the task has completed. This property raises a notification when the value changes to <c>true</c>.
        /// </summary>
        public bool IsCompleted => Task.IsCompleted;

        /// <summary>
        /// Gets whether the task is busy (not completed). This property raises a notification when the value changes to <c>false</c>.
        /// </summary>
        public bool IsNotCompleted => !Task.IsCompleted;

        /// <summary>
        /// Gets whether the task has completed successfully. This property raises a notification when the value changes to <c>true</c>.
        /// </summary>
        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        /// <summary>
        /// Gets whether the task has been canceled. This property raises a notification only if the task is canceled (i.e., if the value changes to <c>true</c>).
        /// </summary>
        public bool IsCanceled => Task.IsCanceled;

        /// <summary>
        /// Gets whether the task has faulted. This property raises a notification only if the task faults (i.e., if the value changes to <c>true</c>).
        /// </summary>
        public bool IsFaulted => Task.IsFaulted;

        /// <summary>
        /// Gets the wrapped faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
        /// </summary>
        public AggregateException Exception => Task.Exception;

        /// <summary>
        /// Gets the original faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
        /// </summary>
        public Exception InnerException => Exception?.InnerException;

        /// <summary>
        /// Gets the error message for the original faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
        /// </summary>
        public string ErrorMessage => InnerException?.Message;

        /// <summary>
        /// Event that notifies listeners of property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public sealed class PropertyChangedEventArgsCache
    {
        /// <summary>
        /// The underlying dictionary. This instance is its own mutex.
        /// </summary>
        private readonly Dictionary<string, PropertyChangedEventArgs> _cache = new Dictionary<string, PropertyChangedEventArgs>();

        /// <summary>
        /// Private constructor to prevent other instances.
        /// </summary>
        private PropertyChangedEventArgsCache()
        {
        }

        /// <summary>
        /// The global instance of the cache.
        /// </summary>
        public static PropertyChangedEventArgsCache Instance { get; } = new PropertyChangedEventArgsCache();

        /// <summary>
        /// Retrieves a <see cref="PropertyChangedEventArgs"/> instance for the specified property, creating it and adding it to the cache if necessary.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        public PropertyChangedEventArgs Get(string propertyName)
        {
            lock (_cache)
            {
                PropertyChangedEventArgs result;
                if (_cache.TryGetValue(propertyName, out result))
                    return result;
                result = new PropertyChangedEventArgs(propertyName);
                _cache.Add(propertyName, result);
                return result;
            }
        }
    }
}
