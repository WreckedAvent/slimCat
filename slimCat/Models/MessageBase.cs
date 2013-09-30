namespace Slimcat.Models
{
    using System;

    using Slimcat.Utilities;

    /// <summary>
    ///     The message base.
    /// </summary>
    public abstract class MessageBase : IDisposable
    {
        #region Fields

        private readonly DateTimeOffset posted;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageBase" /> class.
        /// </summary>
        protected MessageBase()
        {
            this.posted = DateTimeOffset.Now;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the posted time.
        /// </summary>
        public DateTimeOffset PostedTime
        {
            get
            {
                return this.posted;
            }
        }

        /// <summary>
        ///     Gets the time stamp.
        /// </summary>
        public string TimeStamp
        {
            get
            {
                return this.posted.ToTimeStamp();
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        #endregion

        #region Methods

        protected abstract void Dispose(bool isManaged);

        #endregion
    }
}