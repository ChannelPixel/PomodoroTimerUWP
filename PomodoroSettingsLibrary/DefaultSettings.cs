using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace PomodoroSettingsLibrary
{
    public class DefaultSettings
    {
        private TimeSpan sessionTime = new TimeSpan(0, 25, 0);
        private TimeSpan shortBreakTime = new TimeSpan(0, 5, 0);
        private TimeSpan longBreakTime = new TimeSpan(0, 30, 0);

        public TimeSpan GetSessionTime()
        {
            return this.sessionTime;
        }

        public TimeSpan GetShortBreakTime()
        {
            return this.shortBreakTime;
        }

        public TimeSpan GetLongBreakTime()
        {
            return this.longBreakTime;
        }

        //SETTER METHODS
        public void SetSessionTime(TimeSpan aTimespan)
        {
            this.sessionTime = aTimespan;
        }

        public void SetShortBreakTime(TimeSpan aTimespan)
        {
            this.shortBreakTime = aTimespan;
        }

        public void SetLongBreakTime(TimeSpan aTimespan)
        {
            this.longBreakTime = aTimespan;
        }
    }
}
