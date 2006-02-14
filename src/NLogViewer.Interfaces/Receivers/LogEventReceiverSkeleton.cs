// 
// Copyright (c) 2004,2005 Jaroslaw Kowalski <jkowalski@users.sourceforge.net>
// 
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of the Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.Threading;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Globalization;

using NLogViewer.Configuration;
using NLogViewer.Events;

namespace NLogViewer.Receivers
{
	public abstract class LogEventReceiverSkeleton : ILogEventReceiver
	{
        private ILogEventProcessor _processor = null;
        private Thread _inputThread = null;
        private bool _quitThread;

		public LogEventReceiverSkeleton()
		{
		}

        public virtual void Configure(NameValueCollection parameters)
        {
            foreach (string name in parameters.Keys)
            {
                PropertyInfo pi = this.GetType().GetProperty(name);
                if (pi == null)
                    return;

                // NLogViewerTrace.Write("Setting {0} to {1}", 
                object value = Convert.ChangeType(parameters[name], pi.PropertyType, CultureInfo.InvariantCulture);
                pi.SetValue(this, value, null);
            }
        }

        public virtual void Start()
        {
            _quitThread = false;
            _inputThread = new Thread(new ThreadStart(InputThread));
            _inputThread.IsBackground = true;
            _inputThread.Start();
        }

        public virtual void Stop()
        {
            if (_inputThread != null)
            {
                _quitThread = true;
                if (!_inputThread.Join(2000))
                {
                    _inputThread.Abort();
                }
            }
        }

        public virtual bool IsRunning
        {
            get { return _inputThread.IsAlive; }
        }

        public abstract void InputThread();

        public bool QuitInputThread
        {
            get { return _quitThread; }
        }

        public void EventReceived(LogEvent logEvent)
        {
            ILogEventProcessor p = _processor;
            if (p != null)
            {
                p.ProcessLogEvent(logEvent);
            }
        }

        public void Connect(ILogEventProcessor processor)
        {
            _processor = processor;
        }

        public void Disconnect()
        {
            _processor = null;
        }
    }
}
