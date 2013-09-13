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

namespace ViewModels
{
    using System;

    using Models;

    /// <summary>
    ///     The right click menu view model.
    /// </summary>
    public sealed class RightClickMenuViewModel : SysProp, IDisposable
    {
        #region Fields

        private readonly bool _isModerator;

        private bool _canIgnore;

        private bool _canUnignore;

        private bool _hasReports;

        private bool _isOpen;

        private ICharacter _target;

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
            this._isModerator = isModerator;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether can ignore.
        /// </summary>
        public bool CanIgnore
        {
            get
            {
                return this._canIgnore;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether can unignore.
        /// </summary>
        public bool CanUnignore
        {
            get
            {
                return this._canUnignore;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has report.
        /// </summary>
        public bool HasReport
        {
            get
            {
                return this._isModerator && this._hasReports;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has status message.
        /// </summary>
        public bool HasStatusMessage
        {
            get
            {
                if (this._target != null)
                {
                    return this._target.StatusMessage.Length > 0;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is open.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return this._isOpen;
            }

            set
            {
                this._isOpen = value;
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
                    if (ApplicationSettings.Interested.Contains(this.Target.Name))
                    {
                        return "Remove interested mark";
                    }
                    else
                    {
                        return "Add interested mark";
                    }
                }
                else
                {
                    return string.Empty;
                }
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
                    if (ApplicationSettings.NotInterested.Contains(this.Target.Name))
                    {
                        return "Remove not interested mark";
                    }
                    else
                    {
                        return "Add not interested mark";
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        ///     Gets the target.
        /// </summary>
        public ICharacter Target
        {
            get
            {
                return this._target;
            }
        }

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
                        case Gender.Herm_F:
                            return "Feminine Herm";
                        case Gender.Herm_M:
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
                        case StatusType.dnd:
                            return "Do Not Disturb";
                        case StatusType.looking:
                            return "Looking For Play";
                        default: // we just need to capitalize the first letter
                            return char.ToUpper(this.Target.Status.ToString()[0])
                                   + this.Target.Status.ToString().Substring(1);
                    }
                }
                else
                {
                    return "None";
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dipose(true);
        }

        /// <summary>
        /// The set new target.
        /// </summary>
        /// <param name="target">
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
        public void SetNewTarget(ICharacter target, bool canIgnore, bool canUnignore, bool hasReports)
        {
            this._target = target;
            this._target.GetAvatar();

            this._canIgnore = canIgnore;
            this._canUnignore = canUnignore;
            this._hasReports = hasReports;

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

        private void Dipose(bool IsManaged)
        {
            this._target = null;
        }

        #endregion
    }
}