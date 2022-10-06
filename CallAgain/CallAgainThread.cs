using CallAgain.Settings;
using ColossalFramework;
using ICities;
using System.Diagnostics;
using System.Threading;

namespace CallAgain
{
    public class CallAgainThread
    {
        public static CallAgain? m_callAgain = null;
        private static volatile bool s_runThread = true;
        private static Thread? s_callAgainThread = null;
        private static EventWaitHandle? s_waitHandle = null;

        public static void StartThreads()
        {
            if (s_callAgainThread == null)
            {
                s_runThread = true;

                // AutoResetEvent releases 1 thread only each time Set() is called.
                s_waitHandle = new AutoResetEvent(false);

                s_callAgainThread = new Thread(new CallAgainThread().ThreadMain);
                s_callAgainThread.IsBackground = true;
                s_callAgainThread.Start();
            }
        }

        public static void StopThreads()
        {
            s_runThread = false;
            if (s_waitHandle != null)
            {
                s_waitHandle.Set();
            }
            s_waitHandle = null;
        }

        public CallAgainThread()
        {

        }

        public void ThreadMain()
        {
            SimulationManager instance = Singleton<SimulationManager>.instance;
            while (s_runThread)
            {
                // Wait for CallAgainUpdateRate timeout
                if (s_waitHandle != null)
                {
                    s_waitHandle.WaitOne(ModSettings.GetSettings().CallAgainUpdateRate * 1000);
                }

                // Call again
                if (s_runThread && !instance.SimulationPaused && ModSettings.GetSettings().CallAgainEnabled)
                {
                    if (m_callAgain == null)
                    {
                        m_callAgain = new CallAgain();
                    }
                    if (m_callAgain != null)
                    {
                        m_callAgain.Update();
                    }
                }
            }
        }
    }
}
