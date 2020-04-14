using System.Threading;

namespace Atlas.MatchingAlgorithm.Services.Utility
{
    public interface IThreadSleeper
    {
        void Sleep(int milliseconds);
    }
    
    public class ThreadSleeper: IThreadSleeper
    {
        public void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}