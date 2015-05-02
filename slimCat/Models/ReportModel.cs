#region Copyright

// <copyright file="ReportModel.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
// 
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>

#endregion

namespace slimCat.Models
{
    #region Usings

    using System;

    #endregion

    /// <summary>
    ///     A model for storing data related to character reports
    /// </summary>
    public sealed class ReportModel : IDisposable
    {
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

        private void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            Complaint = null;
            Reporter = null;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the call id.
        /// </summary>
        public string CallId { get; set; }

        /// <summary>
        ///     Gets or sets the complaint.
        /// </summary>
        public string Complaint { get; set; }

        /// <summary>
        ///     Gets or sets the log id.
        /// </summary>
        public int? LogId { get; set; }

        /// <summary>
        ///     Gets or sets the reported.
        /// </summary>
        public string Reported { get; set; }

        /// <summary>
        ///     Gets or sets the reporter.
        /// </summary>
        public ICharacter Reporter { get; set; }

        /// <summary>
        ///     Gets or sets the tab.
        /// </summary>
        public string Tab { get; set; }

        #endregion
    }
}