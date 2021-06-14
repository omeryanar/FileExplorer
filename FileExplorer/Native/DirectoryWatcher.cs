using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileExplorer.Core;

namespace FileExplorer.Native
{
    public class FileEvent
    {
        public FileEvent(string path, ChangeType changeType)
        {
            Path = path;
            ChangeType = changeType;
        }

        public FileEvent(string path, string newPath)
        {
            Path = path;
            NewPath = newPath;
            ChangeType = ChangeType.Renamed;
        }

        public string Path { get; set; }
        public string NewPath { get; set; }
        public ChangeType ChangeType { get; set; }
    }

    public enum ChangeType
    {
        Changed,
        Created,
        Deleted,
        Renamed
    }

    public class DirectoryWatcher
    {
        public DirectoryWatcher(string path, Action<FileEvent> onEvent, Action<ErrorEventArgs> onError)
        {
            fileSystemWatcher = new FileSystemWatcher(path);
            fileSystemWatcher.InternalBufferSize = 65536;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;

            fileSystemWatcher.Error += (s, e) => { onError(e); };

            fileSystemWatcher.Changed += (s, e) => { fileEventQueue.Add(new FileEvent(e.FullPath, ChangeType.Changed)); };
            fileSystemWatcher.Created += (s, e) => { fileEventQueue.Add(new FileEvent(e.FullPath, ChangeType.Created)); };
            fileSystemWatcher.Deleted += (s, e) => { fileEventQueue.Add(new FileEvent(e.FullPath, ChangeType.Deleted)); };
            fileSystemWatcher.Renamed += (s, e) => { fileEventQueue.Add(new FileEvent(e.OldFullPath, e.FullPath)); };

            eventProcessor = new EventProcessor(onEvent);
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    FileEvent e = fileEventQueue.Take();
                    eventProcessor.ProcessEvent(e);
                }
            });
            thread.IsBackground = true; // this ensures the thread does not block the process from terminating!
            thread.Start();
        }

        public void Start()
        {
            try
            {
                fileSystemWatcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                Journal.WriteLog(e);
            }
        }

        public void Stop()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }

        private BlockingCollection<FileEvent> fileEventQueue = new BlockingCollection<FileEvent>();

        private FileSystemWatcher fileSystemWatcher;

        private EventProcessor eventProcessor;
    }

    internal class EventProcessor
    {
        private static int EVENT_DELAY = 100; // aggregate and only emit events when changes have stopped for this duration (in ms)

        private object LOCK = new object();
        private Task delayTask = null;

        private List<FileEvent> events = new List<FileEvent>();
        private Action<FileEvent> handleEvent;

        private long lastEventTime = 0;
        private long delayStarted = 0;

        public EventProcessor(Action<FileEvent> onEvent)
        {
            handleEvent = onEvent;
        }

        public void ProcessEvent(FileEvent fileEvent)
        {
            lock (LOCK)
            {
                long now = DateTime.Now.Ticks;

                // Add into our queue
                events.Add(fileEvent);
                lastEventTime = now;

                // Process queue after delay
                if (delayTask == null)
                {
                    // Create function to buffer events
                    Action<Task> func = null;
                    func = (Task value) =>
                    {
                        lock (LOCK)
                        {
                            // Check if another event has been received in the meantime
                            if (delayStarted == lastEventTime)
                            {
                                // Normalize and handle
                                foreach (FileEvent e in NormalizeEvents(events.ToArray()))
                                    handleEvent(e);

                                // Reset
                                events.Clear();
                                delayTask = null;
                            }

                            // Otherwise we have received a new event while this task was
                            // delayed and we reschedule it.
                            else
                            {
                                delayStarted = lastEventTime;
                                delayTask = Task.Delay(EVENT_DELAY).ContinueWith(func);
                            }
                        }
                    };

                    // Start function after delay
                    delayStarted = lastEventTime;
                    delayTask = Task.Delay(EVENT_DELAY).ContinueWith(func);
                }
            }
        }

        private IEnumerable<FileEvent> NormalizeEvents(FileEvent[] events)
        {
            var mapPathToEvents = new Dictionary<string, FileEvent>();
            var eventsWithoutDuplicates = new List<FileEvent>();

            // Normalize Duplicates
            foreach (FileEvent e in events)
                NormalizeEvent(e, eventsWithoutDuplicates, mapPathToEvents);

            // Handle deletes
            var addedChangeEvents = new List<FileEvent>();
            var deletedPaths = new List<string>();

            // This algorithm will remove all DELETE events up to the root folder
            // that got deleted if any. This ensures that we are not producing
            // DELETE events for each file inside a folder that gets deleted.
            //
            // 1.) split ADD/CHANGE and Deleted events
            // 2.) sort short deleted paths to the top
            // 3.) for each DELETE, check if there is a deleted parent and ignore the event in that case

            return eventsWithoutDuplicates
                .Where((e) =>
                {
                    if (e.ChangeType != ChangeType.Deleted)
                    {
                        addedChangeEvents.Add(e);
                        return false; // remove ADD / CHANGE
                    }

                    return true;
                })
                .OrderBy((e) => e.Path.Length) // shortest Path first
                .Where((e) =>
                {
                    //if (deletedPaths.Any(d => IsParent(e.Path, d)))
                    if (deletedPaths.Any(d => e.Path.StartsWith(d + "\\", StringComparison.OrdinalIgnoreCase)))
                    {
                        return false; // DELETE is ignored if parent is deleted already
                    }

                    // otherwise mark as deleted
                    deletedPaths.Add(e.Path);

                    return true;
                })
                .Concat(addedChangeEvents);
        }

        private void NormalizeEvent(FileEvent newEvent, List<FileEvent> eventsWithoutDuplicates, Dictionary<string, FileEvent> mapPathToEvents)
        {
            if (mapPathToEvents.ContainsKey(newEvent.Path))
            {
                FileEvent existingEvent = mapPathToEvents[newEvent.Path];
                ChangeType existingChangeType = existingEvent.ChangeType;
                ChangeType newChangeType = newEvent.ChangeType;

                // ignore CREATE followed by DELETE in one go
                if (existingChangeType == ChangeType.Created && newChangeType == ChangeType.Deleted)
                {
                    mapPathToEvents.Remove(existingEvent.Path);
                    eventsWithoutDuplicates.Remove(existingEvent);
                }

                // flatten DELETE followed by CREATE into CHANGE
                else if (existingChangeType == ChangeType.Deleted && newChangeType == ChangeType.Created)
                {
                    existingEvent.ChangeType = ChangeType.Changed;
                }

                // Do nothing. Keep the created event
                else if (existingChangeType == ChangeType.Created && newChangeType == ChangeType.Changed)
                {
                }

                // Pretend as if CREATE followed by DELETE and renormalize CREATE
                else if (existingChangeType == ChangeType.Created && newChangeType == ChangeType.Renamed)
                {
                    mapPathToEvents.Remove(existingEvent.Path);
                    eventsWithoutDuplicates.Remove(existingEvent);

                    existingEvent.Path = newEvent.NewPath;
                    NormalizeEvent(existingEvent, eventsWithoutDuplicates, mapPathToEvents);
                }

                // Pretend as if DELETE followed by CREATE and renormalize CREATE
                else if (existingChangeType == ChangeType.Renamed && newChangeType == ChangeType.Created)
                {
                    existingEvent.ChangeType = ChangeType.Changed;

                    newEvent.Path = existingEvent.NewPath;
                    NormalizeEvent(newEvent, eventsWithoutDuplicates, mapPathToEvents);
                }

                // Otherwise apply change type
                else
                {
                    existingEvent.ChangeType = newChangeType;
                    existingEvent.NewPath = newEvent.NewPath;
                }
            }

            // New event
            else
            {
                mapPathToEvents.Add(newEvent.Path, newEvent);
                eventsWithoutDuplicates.Add(newEvent);
            }
        }
    }
}
