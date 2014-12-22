namespace ServiceControl.Operations
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Contracts.Operations;
    using NServiceBus;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Satellites;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Transport;
    using Raven.Client;
    using ServiceBus.Management.Infrastructure.Settings;

    public class AuditQueueImport : IAdvancedSatellite, IDisposable
    {
        public IBuilder Builder { get; set; }
        public ISendMessages Forwarder { get; set; }

#pragma warning disable 618
        public PipelineExecutor PipelineExecutor { get; set; }
        public LogicalMessageFactory LogicalMessageFactory { get; set; }

#pragma warning restore 618

        public AuditQueueImport(IDequeueMessages receiver)
        {
            disabled = false; //receiver is MsmqDequeueStrategy;
        }

        public bool Handle(TransportMessage message)
        {
            InnerHandle(message);

            return true;
        }

        void InnerHandle(TransportMessage message)
        {
            var receivedMessage = new ImportSuccessfullyProcessedMessage(message);

            using (var childBuilder = Builder.CreateChildBuilder())
            {
                PipelineExecutor.CurrentContext.Set(childBuilder);

                foreach (var enricher in childBuilder.BuildAll<IEnrichImportedMessages>())
                {
                    enricher.Enrich(receivedMessage);
                }

                var logicalMessage = LogicalMessageFactory.Create(typeof(ImportSuccessfullyProcessedMessage),
                    receivedMessage);

                PipelineExecutor.InvokeLogicalMessagePipeline(logicalMessage);
                throughputCalculator.Done();
            }

            if (Settings.ForwardAuditMessages)
            {
                Forwarder.Send(message, Settings.AuditLogQueue);
            }
        }

        public void Start()
        {
            Logger.InfoFormat("Audit import is now started, feeding audit messages from: {0}", InputAddress);
            throughputCalculator.Start();
        }

        public void Stop()
        {
            throughputCalculator.Stop();
        }

        public Address InputAddress
        {
            get { return Settings.AuditQueue; }
        }

        public bool Disabled
        {
            get { return disabled; }
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            satelliteImportFailuresHandler = new SatelliteImportFailuresHandler(Builder.Build<IDocumentStore>(),
                Path.Combine(Settings.LogPath, @"FailedImports\Audit"), tm => new FailedAuditImport
                {
                    Message = tm,
                });

            return receiver => { receiver.FailureManager = satelliteImportFailuresHandler; };
        }

        public void Dispose()
        {
            if (satelliteImportFailuresHandler != null)
            {
                satelliteImportFailuresHandler.Dispose();
            }
        }

        SatelliteImportFailuresHandler satelliteImportFailuresHandler;

        static readonly ILog Logger = LogManager.GetLogger(typeof(AuditQueueImport));
        readonly AverageThroughputCalculator throughputCalculator = new AverageThroughputCalculator(
            TimeSpan.FromSeconds(5),5,avg => Console.WriteLine(string.Format("Average throughput in last 5 seconds: {0,10:0.000}",avg))
            );
        bool disabled;
    }

    public class AverageThroughputCalculator
    {
        private readonly Action<double> onProbe;
        private readonly int[] buffer;
        private readonly Stopwatch[] watches;
        private readonly int period;
        private readonly Timer timer;
        private int currentSlot;

        public AverageThroughputCalculator(TimeSpan windowLenght, int probeFrequency, Action<double> onProbe)
        {
            this.onProbe = onProbe;
            buffer = new int[probeFrequency];
            watches = new Stopwatch[probeFrequency];
            period = (int)(windowLenght.TotalMilliseconds / probeFrequency);
            timer = new Timer(Tick, null, Timeout.Infinite, period);
        }

        private void Tick(object state)
        {
            var newSlot = (currentSlot + 1) % buffer.Length;
            var oldestValue = buffer[newSlot];
            buffer[newSlot] = 0;
            currentSlot = newSlot;
            watches[newSlot].Stop();
            var elapsed = watches[newSlot].Elapsed.TotalSeconds;
            watches[newSlot].Reset();
            watches[newSlot].Start();
            var sum = oldestValue + buffer.Where((t, i) => i != currentSlot).Sum();
            var average = sum / elapsed;
            onProbe(average);
        }

        public void Done()
        {
            Interlocked.Increment(ref buffer[currentSlot]);
        }

        public void Start()
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0;
                watches[i] = new Stopwatch();
                watches[i].Start();
            }
            currentSlot = 0;
            timer.Change(0, period);
        }

        public void Stop()
        {
            timer.Change(Timeout.Infinite, period);
        }
    }
}