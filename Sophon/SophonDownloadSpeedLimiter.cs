using System;
using System.Threading;

namespace Sophon
{
    public class SophonDownloadSpeedLimiter
    {
        internal event EventHandler<int> CurrentChunkProcessingChangedEvent;
        internal event EventHandler<long> DownloadSpeedChangedEvent;

        internal long? InitialRequestedSpeed { get; set; }
        private EventHandler<long> _innerListener;
        internal int CurrentChunkProcessing;

        private SophonDownloadSpeedLimiter(long initialRequestedSpeed)
        {
            InitialRequestedSpeed = initialRequestedSpeed;
        }

        public static SophonDownloadSpeedLimiter CreateInstance(long initialSpeed) => new(initialSpeed);

        public EventHandler<long> GetListener() => _innerListener ??= OnDownloadSpeedChanged;

        private void OnDownloadSpeedChanged(object sender, long newSpeed)
        {
            InitialRequestedSpeed = newSpeed;
            DownloadSpeedChangedEvent?.Invoke(this, newSpeed);
        }

        internal void IncrementChunkProcessedCount()
        {
            int newCount = Interlocked.Increment(ref CurrentChunkProcessing);
            CurrentChunkProcessingChangedEvent?.Invoke(this, newCount);
        }

        internal void DecrementChunkProcessedCount()
        {
            int newCount = Interlocked.Decrement(ref CurrentChunkProcessing);
            CurrentChunkProcessingChangedEvent?.Invoke(this, newCount);
        }
    }
}