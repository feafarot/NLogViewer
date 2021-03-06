using System;
using System.IO;
using System.Xml;
using nDumbster.smtp;
using System.Collections.Specialized;

using NLogViewer.Receivers;
using NLogViewer.Events;
using System.Text;
using NLogViewer.Parsers;

namespace NLogViewer.Receivers
{
    [LogEventReceiver("SMTP", 
        "SMTP Receiver", 
        "Receives events using a mock SMTP server")]
    public class NDumbsterSmtpEventReceiver : LogEventReceiverWithParserSkeleton
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private SimpleSmtpServer _smtpServer = null;
        private int _port = SimpleSmtpServer.DEFAULT_SMTP_PORT;

        public NDumbsterSmtpEventReceiver()
        {
        }

        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public override void Start()
        {
            logger.Info("Starting SMTP server on port {0}", _port);
            _smtpServer = SimpleSmtpServer.Start(_port);
            base.Start ();
        }

        public override void InputThread()
        {
            while (!InputThreadQuitRequested())
            {
                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(100));

                if (_smtpServer.ReceivedEmailCount > 0)
                {
                    foreach(SmtpMessage message in _smtpServer.ReceivedEmail)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(message.Body);
                        MemoryStream ms = new MemoryStream(bytes);
                        using (ILogEventParserInstance context = Parser.Begin(ms))
                        {
                            LogEvent logEvent = CreateLogEvent();

                            if (context.ReadNext(logEvent))
                            {
                                foreach (string header in message.Headers.AllKeys)
                                {
                                    logEvent[header] = message.Headers[header];
                                }

                                EventReceived(logEvent);
                            }
                        }
                    }

                    _smtpServer.ClearReceivedEmail();
                }
            }
        }

        public override void Stop()
        {
            logger.Info("Stopping SMTP Server on port {0}", _port);
            base.Stop ();
            if (_smtpServer != null)
            {
                _smtpServer.Stop();
                _smtpServer = null;
            }
        }
    }
}
