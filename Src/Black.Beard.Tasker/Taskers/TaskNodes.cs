using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bb.Taskers
{

    public class TaskNodes<T>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskNodes{T}"/> class.
        /// </summary>
        public TaskNodes()
        {
            _nodes = new List<TaskNode<T>>();
            _runnings = new Dictionary<string, Count>();
        }


        /// <summary>
        /// Creates a new node with the specified task name.
        /// </summary>
        /// <typeparam name="TNode">The type of the node. mùust inherit from TaskNode<T></typeparam>
        /// <param name="taskName">Name of the task.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public TNode Create<TNode>(string taskName, Func<T, bool> action)
            where TNode : TaskNode<T>, new()
        {

            var node = new TNode()
            {
                TaskName = taskName,
                Action = action,
                Parent = this,
            };

            _nodes.Add(node);

            return node;

        }

        /// <summary>
        /// Creates a new node with the specified task name.
        /// </summary>
        /// <param name="taskName">Name of the task.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public TaskNode<T> Create(string taskName, Func<T, bool> action)
        {

            var node = new TaskNode<T>()
            {
                TaskName = taskName,
                Action = action,
                Parent = this,
            };

            _nodes.Add(node);
            if (!_runnings.ContainsKey(node.TaskName))
                _runnings.Add(node.TaskName, new Count());

            return node;

        }

        internal TaskNode<T> Get(string taskName)
        {
            return _nodes.FirstOrDefault(c => c.TaskName == taskName);
        }

        /// <summary>
        /// Runs with the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="continueIfException">if set to <c>true</c> [continue if exception].</param>
        /// <returns></returns>
        public TaskNodes<T> Run(T context, bool continueIfException = false)
        {

            _continueIfException = continueIfException;

            foreach (var node in _nodes)
                node.TryLaunch(context);

            return this;

        }

        /// <summary>
        /// Waits the specified seconds.
        /// </summary>
        /// <param name="seconds">The seconds. if -1 wait infinity</param>
        public void Wait(int seconds = 60)
        {

            var expire = seconds == -1
                ? DateTime.Now.AddYears(10)
                : DateTime.Now.AddSeconds(seconds);

            while (IsRunning)
            {

                Thread.Yield();

                if (DateTime.Now > expire)
                    break;

            }

        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning => _runnings.Any(c => c.Value.Value > 0);

        /// <summary>
        /// Gets a value indicating whether this instance is failed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is failed; otherwise, <c>false</c>.
        /// </value>
        public bool IsFailed => _nodes.Any(c => !c.IsSuccess);

        public IEnumerable<TaskNode<T>> Nodes => _nodes.Where(c => !c.IsEnded);

        protected internal virtual void Event(EventKind kind, TaskNode<T> taskNode, Exception exception = null)
        {
            string msg = string.Empty;
            if (exception != null)
                msg = $"{taskNode.TaskName} is {kind} at {DateTime.Now}. {exception.Message}";
            else
                msg = $"{taskNode.TaskName} is {kind} at {DateTime.Now}";

#if DEBUG
            Debug.WriteLine(msg);
#endif
            Trace.WriteLine(msg);
        }



        /// <summary>
        /// Resets this task failed.
        /// </summary>
        public void Reset()
        {

            foreach (var node in _nodes) // On reset les nodes en erreurs
                if (node.IsEnded && !node.IsSuccess)
                    node.Reset();

        }


        internal void Start(TaskNode<T> task)
        {
            _runnings[task.TaskName].Increment();
        }

        internal void Stop(TaskNode<T> task)
        {
            _runnings[task.TaskName].Decrement();
        }

        private Dictionary<string, Count> _runnings;
        internal bool _continueIfException;
        private readonly List<TaskNode<T>> _nodes;
        private readonly object _lock = new object();

        public class Count
        {

            internal void Decrement()
            {
                lock (_lock)
                    _value = --_value;
            }

            internal void Increment()
            {
                lock (_lock)
                    _value = ++_value;
            }

            public int Value => _value;

            private readonly object _lock = new object();
            private int _value = 0;

        }

    }

}