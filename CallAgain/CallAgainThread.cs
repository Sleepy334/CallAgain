using CallAgain.Settings;
using ICities;
using System.Diagnostics;

namespace CallAgain
{
    public class CallAgainThread : ThreadingExtensionBase
    {
        private long m_LastCallAgainElapsedTime = 0;
        private Stopwatch? m_watch = null;
        public static CallAgain? s_callAgain = null;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (!CallAgainLoader.IsLoaded())
            {
                return;
            }

            if (m_watch == null)
            {
                m_watch = new Stopwatch();
            }

            // Update panel
            if (SimulationManager.instance.SimulationPaused)
            {
                if (m_watch.IsRunning)
                {
                    m_watch.Stop();
                    m_LastCallAgainElapsedTime = 0;
                }
            }
            else
            {
                if (!m_watch.IsRunning)
                {
                    m_watch.Start();
                    m_LastCallAgainElapsedTime = m_watch.ElapsedMilliseconds;
                }

                // Call again
                if (ModSettings.GetSettings().CallAgainEnabled && (m_watch.ElapsedMilliseconds - m_LastCallAgainElapsedTime > (ModSettings.GetSettings().CallAgainUpdateRate * 1000)))
                {
                    if (s_callAgain == null)
                    {
                        s_callAgain = new CallAgain();
                    }
                    if (s_callAgain != null)
                    {
                        s_callAgain.Update(m_watch);
                    }

                    m_LastCallAgainElapsedTime = m_watch.ElapsedMilliseconds;
                }
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
        }
    }
}
