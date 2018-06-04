using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SelfDestruct
{
    /// <summary>
    /// CountdownTimer is a Timer object designed to be used for countdown sequences.
    /// It inherits the System.Timers.Timer class.
    /// </summary>
    class CountdownTimer : Timer
    {

        private double initialStart = 0;


        public CountdownTimer() : base() {

        }

        public void Start() {

            initialStart = currTimeMillis();
            base.Start();
        }

        /// <summary>
        /// Allows the timer to be started again while retaining the initial Interval property.
        /// </summary>
        public void Continue() {
            double elapsedTime = currTimeMillis() - initialStart;
            Interval = Math.Max(0, Interval - elapsedTime);

            if (Interval <= 0) {
                Interval = 10;
            }

            base.Start();
        }

        /// <summary>
        /// Wrapper method for getting the system time in milliseconds.
        /// </summary>
        /// <returns>
        /// Returns the current CPU time in milliseconds.
        /// </returns>
        private double currTimeMillis() {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

    }
}
