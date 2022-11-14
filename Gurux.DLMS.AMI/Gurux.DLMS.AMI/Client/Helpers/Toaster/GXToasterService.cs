using System.Timers;

namespace Gurux.DLMS.AMI.Client.Helpers.Toaster
{
    /// <summary>
    /// This interface is used to handle toaster services.
    /// </summary>
    interface IGXToasterService
    {
        /// <summary>
        /// Maximum toast count.
        /// </summary>
        int MaxCount
        {
            get;
            set;
        }

        /// <summary>
        /// Add new toast.
        /// </summary>
        /// <param name="toast"></param>
        void Add(GXToast toast);

        /// <summary>
        /// Are the any toasts.
        /// </summary>
        bool Any { get; }

        /// <summary>
        /// Get toasts.
        /// </summary>
        /// <returns></returns>
        List<GXToast> GetToasts();

        /// <summary>
        /// Remove toasts.
        /// </summary>
        /// <param name="toast">Toast to remove.</param>
        public void Remove(GXToast toast);

        /// <summary>
        /// Notify that toaster has changed.
        /// </summary>
        event EventHandler? ToasterChanged;
        /// <summary>
        /// Notify that toaster time has elapsed and it's removed.
        /// </summary>
        event EventHandler? ToasterTimerElapsed;
    }

    /// <inheritdoc />
    public class GXToasterService : IGXToasterService, IDisposable
    {
        private readonly List<GXToast> _toastList = new List<GXToast>();
        private System.Timers.Timer _timer = new System.Timers.Timer();
        public event EventHandler? ToasterChanged;
        public event EventHandler? ToasterTimerElapsed;

        /// <inheritdoc />
        public bool Any
        {
            get
            {
                return _toastList.Any();
            }
        }

        /// <inheritdoc />
        public int MaxCount
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXToasterService()
        {
            MaxCount = 20;
            _timer.Interval = 1000;
            _timer.AutoReset = true;
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        /// <inheritdoc />
        public List<GXToast> GetToasts()
        {
            RemoveElapsed();
            return _toastList;
        }

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            RemoveElapsed();
            ToasterTimerElapsed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Add(GXToast toast)
        {
            if (MaxCount == _toastList.Count)
            {
                //Remove oldest item when toater is full.
                Remove(_toastList.First());
            }
            _toastList.Add(toast);
            if (!RemoveElapsed())
                ToasterChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Remove(GXToast toast)
        {
            if (_toastList.Contains(toast))
            {
                _toastList.Remove(toast);
                if (!RemoveElapsed())
                    ToasterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Remove elapsed toasters.
        /// </summary>
        /// <returns></returns>
        private bool RemoveElapsed()
        {
            var removed = _toastList.Where(item => item.IsElapsed).ToList();
            if (removed != null && removed.Any())
            {
                removed.ForEach(toast => _toastList.Remove(toast));
                ToasterChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Close timer.
        /// </summary>
        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= TimerElapsed;
                _timer.Stop();
            }
        }
    }
}