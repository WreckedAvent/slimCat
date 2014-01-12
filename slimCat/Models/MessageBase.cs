#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageBase.cs">
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

namespace Slimcat.Models
{
    #region Usings

    using System;
    using Utilities;

    #endregion

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
            posted = DateTimeOffset.Now;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the posted time.
        /// </summary>
        public DateTimeOffset PostedTime
        {
            get { return posted; }
        }

        /// <summary>
        ///     Gets the time stamp.
        /// </summary>
        public string TimeStamp
        {
            get { return posted.ToTimeStamp(); }
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

        #endregion

        #region Methods

        protected abstract void Dispose(bool isManaged);

        #endregion
    }
}