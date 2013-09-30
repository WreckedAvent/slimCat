namespace Slimcat.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Timers;

    /// <summary>
    ///     Caches some int and fetches a new one every so often, displaying how this int has changed
    /// </summary>
    public sealed class CacheCount : IDisposable
    {
        #region Fields

        private readonly int updateResolution;

        private Func<int> getNewCount;

        private bool intialized;

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
            this.oldCounts = new List<int>();
            this.getNewCount = getNewCount;
            this.updateResolution = updateResolution;

            this.updateTick = new Timer(updateResolution * 1000);
            this.updateTick.Elapsed += (s, e) => this.Update();

            if (wait > 0)
            {
                var waitToStartTick = new Timer(wait);
                waitToStartTick.Elapsed += (s, e) =>
                    {
                        this.updateTick.Start();
                        waitToStartTick.Dispose();
                    };
                waitToStartTick.Start();
            }
            else
            {
                this.updateTick.Start();
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     returns the average of the cached values
        /// </summary>
        /// <returns>
        ///     The <see cref="double" />.
        /// </returns>
        public double Average()
        {
            return this.oldCounts.Average();
        }

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        ///     The get display string.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string GetDisplayString()
        {
            if (!this.intialized)
            {
                return string.Empty;
            }

            var change = this.newCount - this.oldCount;

            var toReturn = new StringBuilder();
            if (change != 0)
            {
                toReturn.Append("Δ=");
                toReturn.Append(change);
            }

            if (this.oldCounts.Count > 0)
            {
                if (Math.Abs(this.Average() - 0) > 0.01)
                {
                    toReturn.Append(" μ=");
                    toReturn.Append(string.Format("{0:0}", this.Average()));
                }

                if (Math.Abs(this.StdDev() - 0) > 0.01)
                {
                    toReturn.Append(" σ=");
                    toReturn.Append(string.Format("{0:0.##}", this.StdDev()));
                }

                toReturn.Append(string.Format(" Stability: {0:0.##}%", this.StabilityIndex()));
            }

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
            double threshold = this.Average() / 10; // standard deviations above this are considered unstable

            // in this case, an average distance of 20% from our average is considered high
            return Math.Max(Math.Min(Math.Log10(threshold / this.StdDev()) * 100, 100), 0);

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
            var squares = this.oldCounts.Select(x => Math.Pow(x - this.Average(), 2)).ToList();

            // this is the squared distance from average
            return Math.Sqrt(squares.Sum() / (squares.Count() > 1 ? squares.Count() - 1 : squares.Count()));

            // calculates population std dev from our sample
        }

        /// <summary>
        ///     The update.
        /// </summary>
        public void Update()
        {
            if (this.intialized)
            {
                this.oldCount = this.newCount;

                // 60/updateres*30 returns how many update resolutions fit in 30 minutes
                if (this.oldCounts.Count > ((60 / this.updateResolution) * 30))
                {
                    this.oldCounts.RemoveAt(0);
                }

                this.oldCounts.Add(this.oldCount);

                this.newCount = this.getNewCount();
            }
            else
            {
                this.oldCount = this.newCount = this.getNewCount();

                if (!(this.oldCount == 0 || this.newCount == 0))
                {
                    this.intialized = true;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The dispose.
        /// </summary>
        /// <param name="isManaged">
        ///     The is managed.
        /// </param>
        private void Dispose(bool isManaged)
        {
            if (!isManaged)
            {
                return;
            }

            this.updateTick.Stop();
            this.updateTick.Dispose();
            this.updateTick = null;

            this.getNewCount = null;
            this.oldCounts.Clear();
            this.oldCounts = null;
        }

        #endregion
    }
}