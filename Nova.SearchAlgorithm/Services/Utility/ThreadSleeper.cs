using System.Threading;

namespace Nova.SearchAlgorithm.Services.Utility
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