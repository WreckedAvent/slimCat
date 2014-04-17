#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheCount.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Timers;

    #endregion

    /// <summary>
    ///     Caches some int and fetches a new one every so often, displaying how this int has changed
    /// </summary>
    public sealed class CacheCount : IDisposable
    {
        #region Fields

        private readonly int updateResolution;

        private Func<int> getNewCount;

        private bool initialized;

        private int newCount;

        private int oldCount;

        private IList<int> oldCounts;

        private Timer updateTick;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CacheCount" /> class.
        ///     creates a new cached count of something
        /// </summary>
        /// <param name="getNewCount">
        ///     function used to get the new count
        /// </param>
        /// <param name="updateResolution">
        ///     how often, in seconds, it is updated
        /// </param>
        /// <param name="wait">
        ///     The wait.
        /// </param>
        public CacheCount(Func<int> getNewCount, int updateResolution, int wait = 0)
        {
            oldCounts = new List<int>();
            this.getNewCount = getNewCount;
            this.updateResolution = updateResolution;

            updateTick = new Timer(updateResolution*1000);
            updateTick.Elapsed += (s, e) => Update();

            if (wait > 0)
            {
                var waitToStartTick = new Timer(wait);
                waitToStartTick.Elapsed += (s, e) =>
                    {
                        updateTick.Start();
                        waitToStartTick.Dispose();
                    };
                waitToStartTick.Start();
            }
            else
                updateTick.Start();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     The get display string.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string GetDisplayString()
        {
            if (!initialized)
                return string.Empty;

            var change = newCount - oldCount;

            var toReturn = new StringBuilder();
            if (change != 0)
            {
                toReturn.Append("Δ=");
                toReturn.Append(change);
            }

            if (oldCounts.Count <= 0)
                return toReturn.ToString().Trim();

            if (Math.Abs(Average() - 0) > 0.01)
            {
                toReturn.Append(" μ=");
                toReturn.Append(string.Format("{0:0}", Average()));
            }

            if (Math.Abs(StdDev() - 0) > 0.01)
            {
                toReturn.Append(" σ=");
                toReturn.Append(string.Format("{0:0.##}", StdDev()));
            }

            toReturn.Append(string.Format(" Stability: {0:0.##}%", StabilityIndex()));

            return toReturn.ToString().Trim();
        }

        /// <summary>
        ///     returns a measure of how stable the values are
        /// </summary>
        /// <returns>
        ///     The <see cref="double" />.
        /// </returns>
        public double StabilityIndex()
        {
            var threshold = Average()/10; // standard deviations above this are considered unstable

            // in this case, an average distance of 20% from our average is considered high
            return Math.Max(Math.Min(Math.Log10(threshold/StdDev())*100, 100), 0);

            // this scary looking thing just ensures that this value is in between 0 and 100
            // and becomes exponentially closer to 0 as the standard deviation approaches the threshold
        }

        /// <summary>
        ///     returns the adjusted standard deviation for the cached values
        /// </summary>
        /// <returns>
        ///     The <see cref="double" />.
        /// </returns>
        public double StdDev()
        {
            var squares = oldCounts.Select(x => Math.Pow(x - Average(), 2)).ToList();

            // this is the squared distance from average
            return Math.Sqrt(squares.Sum()/(squares.Count() > 1 ? squares.Count() - 1 : squares.Count()));

            // calculates population std dev from our sample
        }

        /// <summary>
        ///     The update.
        /// </summary>
        public void Update()
        {
            if (initialized)
            {
                oldCount = newCount;

                // 60/updateres*30 returns how many update resolutions fit in 30 minutes
                if (oldCounts.Count > ((60/updateResolution)*30))
                    oldCounts.RemoveAt(0);

                oldCounts.Add(oldCount);

                newCount = getNewCount();
            }
            else
            {
                oldCount = newCount = getNewCount();

                if (!(oldCount == 0 || newCount == 0))
                    initialized = true;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     returns the average of the cached values
        /// </summary>
        /// <returns>
        ///     The <see cref="double" />.
        /// </returns>
        private double Average()
        {
            return oldCounts.Average();
        }

        /// <summary>
        ///     The dispose.
        /// </summary>
        /// <param name="isManaged">
        ///     The is managed.
        /// </param>
        private void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            updateTick.Stop();
            updateTick.Dispose();
            updateTick = null;

            getNewCount = null;
            oldCounts.Clear();
            oldCounts = null;
        }

        #endregion
    }
}