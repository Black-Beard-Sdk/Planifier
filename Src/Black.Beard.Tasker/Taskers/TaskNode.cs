using Bb.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bb.Taskers
{

    [System.Diagnostics.DebuggerDisplay("{TaskName} : {IsEnded ? IsSuccess ? \"Success\" : \"Failed\" : \"Not started\"}")]
    public class TaskNode<T>
    {

        public TaskNode()
        {
            _guards = new List<Guard>();
        }

        public string TaskName { get; internal set; }

        public Func<T, bool> Action { get; internal set; }

        public bool IsSuccess { get; private set; }

        public bool IsEnded { get; private set; }

        public bool IsLaunched { get; private set; }

        public TaskNode<T> ContinueWith(string taskName, bool stopIfFailed = true)
        {
            TaskNode<T> node = Parent.Get(taskName) ?? throw new Exception($"task node {taskName} can't be resolved");
            return ContinueWith(node, stopIfFailed);
        }

        /// <summary>
        /// Continues with the specified node
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="stopIfFailed">if set to <c>true</c> [stop if the current node is failed].</param>
        /// <returns></returns>
        public TaskNode<T> ContinueWith(TaskNode<T> node, bool stopIfFailed = true)
        {
            node._guards.Add(new Guard(this, stopIfFailed));
            return node;
        }

        internal void Reset()
        {
            if (IsSuccess == false)
                IsEnded = false;
        }

        internal TaskNodes<T> Parent { get; set; }

        internal void TryLaunch(T context)
        {

            if (!IsLaunched && !IsEnded && CanStart()) // En cas de restart
                lock (_lock)    // Eviter de lancer la node deux fois
                    if (!IsLaunched && !IsEnded && CanStart())
                    {

                        IsLaunched = true;
                        Parent.Start(this);

                        Task.Run(
                        () =>
                        {
                            try
                            {
                                Run(context);
                            }
                            finally
                            {
                                IsLaunched = false;
                                Parent.Stop(this);
                            }
                        });

                    }

        }

        private bool CanStart()
        {

            foreach (var item in _guards)
                if (!item.Evaluate())
                    return false;

            return true;

        }

        private void Run(T context)
        {

            Parent.Event(EventKind.Started, this);

            try
            {
                IsSuccess = Action(context);
                IsEnded = true;
                if (IsSuccess)
                    Parent.Event(EventKind.Ended, this);
                else
                    Parent.Event(EventKind.Failed, this);
            }
            catch (Exception e)
            {
                IsEnded = true;
                Parent.Event(EventKind.Failed, this, e);
                if (!Parent._continueIfException)
                    throw new TaskNodeException($"task {TaskName} failed. {e.Message}", e);
            }

            Parent.Run(context, Parent._continueIfException);

        }

        private readonly object _lock = new object();
        private readonly List<Guard> _guards = new List<Guard>();

        [System.Diagnostics.DebuggerDisplay("{TaskName}")]
        private class Guard
        {

            public Guard(TaskNode<T> taskNode, bool stopIfFailed)
            {
                _taskNode = taskNode;
                _stopIfFailed = stopIfFailed;
            }

            public string TaskName => _taskNode.TaskName;

            public bool Evaluate()
            {

                if (_taskNode.IsEnded)
                {

                    if (!_taskNode.IsSuccess && _stopIfFailed)
                        return false;

                    return true;

                }

                return false;

            }

            private readonly TaskNode<T> _taskNode;
            private readonly bool _stopIfFailed;

        }

    }

}
