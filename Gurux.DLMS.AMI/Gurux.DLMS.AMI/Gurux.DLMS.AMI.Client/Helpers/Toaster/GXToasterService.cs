//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Timers;

namespace Gurux.DLMS.AMI.Client.Helpers.Toaster
{
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
            if (RemoveElapsed())
            {
                ToasterTimerElapsed?.Invoke(this, EventArgs.Empty);
            }
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