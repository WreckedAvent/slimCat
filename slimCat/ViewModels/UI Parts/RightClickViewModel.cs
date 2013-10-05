// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RightClickViewModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   The right click menu view model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.ViewModels
{
    using System;

    using Models;

    /// <summary>
    ///     The right click menu view model.
    /// </summary>
    public sealed class RightClickMenuViewModel : SysProp, IDisposable
    {
        #region Fields

        private readonly bool isModerator;

        private bool hasReports;

        private bool isOpen;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RightClickMenuViewModel"/> class.
        /// </summary>
        /// <param name="isModerator">
        /// The is moderator.
        /// </param>
        public RightClickMenuViewModel(bool isModerator)
        {
            this.isModerator = isModerator;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether can ignore.
        /// </summary>
        public bool CanIgnore { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether can unignore.
        /// </summary>
        public bool CanUnignore { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether has report.
        /// </summary>
        public bool HasReport
        {
            get
            {
                return this.isModerator && this.hasReports;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has status message.
        /// </summary>
        public bool HasStatusMessage
        {
            get
            {
                if (this.Target != null)
                {
                    return this.Target.StatusMessage.Length > 0;
                }

                return false;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is open.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return this.isOpen;
            }

            set
            {
                this.isOpen = value;
                this.OnPropertyChanged("IsOpen");
            }
        }

        /// <summary>
        ///     Gets the mark interested.
        /// </summary>
        public string MarkInterested
        {
            get
            {
                if (this.Target != null)
                {
                    return ApplicationSettings.Interested.Contains(this.Target.Name) ? "Remove interested mark" : "Add interested mark";
                }

                return string.Empty;
            }
        }

        /// <summary>
        ///     Gets the mark uninterested.
        /// </summary>
        public string MarkUninterested
        {
            get
            {
                if (this.Target != null)
                {
                    return ApplicationSettings.NotInterested.Contains(this.Target.Name) ? "Remove not interested mark" : "Add not interested mark";
                }

                return string.Empty;
            }
        }

        /// <summary>
        ///     Gets the target.
        /// </summary>
        public ICharacter Target { get; private set; }

        /// <summary>
        ///     Gets the target gender.
        /// </summary>
        public string TargetGender
        {
            get
            {
                if (this.Target != null)
                {
                    switch (this.Target.Gender)
                    {
                        case Gender.HermF:
                            return "Feminine Herm";
                        case Gender.HermM:
                            return "Masculine Herm";
                        default:
                            return this.Target.Gender.ToString();
                    }
                }

                return "None";
            }
        }

        /// <summary>
        ///     Gets the target status.
        /// </summary>
        public string TargetStatus
        {
            get
            {
                if (this.Target != null)
                {
                    switch (this.Target.Status)
                    {
                        case StatusType.Dnd:
                            return "Do Not Disturb";
                        case StatusType.Looking:
                            return "Looking For Play";
                        default: // we just need to capitalize the first letter
                            return char.ToUpper(this.Target.Status.ToString()[0])
                                   + this.Target.Status.ToString().Substring(1);
                    }
                }

                return "None";
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The set new target.
        /// </summary>
        /// <param name="newTarget">
        /// The target.
        /// </param>
        /// <param name="canIgnore">
        /// The can ignore.
        /// </param>
        /// <param name="canUnignore">
        /// The can unignore.
        /// </param>
        /// <param name="hasReports">
        /// The has reports.
        /// </param>
        public void SetNewTarget(ICharacter newTarget, bool canIgnore, bool canUnignore, bool hasReports)
        {
            this.Target = newTarget;
            this.Target.GetAvatar();

            this.CanIgnore = canIgnore;
            this.CanUnignore = canUnignore;
            this.hasReports = hasReports;

            this.OnPropertyChanged("Target");
            this.OnPropertyChanged("CanIgnore");
            this.OnPropertyChanged("CanUnignore");
            this.OnPropertyChanged("TargetGender");
            this.OnPropertyChanged("TargetStatus");
            this.OnPropertyChanged("HasStatus");
            this.OnPropertyChanged("HasReport");
        }

        #endregion

        #region Methods

        private void Dipose(bool isManaged)
        {
            this.Target = null;
        }

        #endregion
    }
}