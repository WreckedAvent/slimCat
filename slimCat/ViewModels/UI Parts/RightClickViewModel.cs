#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RightClickViewModel.cs">
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

namespace Slimcat.ViewModels
{
    #region Usings

    using Models;

    #endregion

    /// <summary>
    ///     The right click menu view model.
    /// </summary>
    public sealed class RightClickMenuViewModel : SysProp
    {
        #region Fields

        private readonly bool isModerator;

        private bool hasReports;

        private bool isOpen;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="RightClickMenuViewModel" /> class.
        /// </summary>
        /// <param name="isModerator">
        ///     The is moderator.
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
            get { return isModerator && hasReports; }
        }

        /// <summary>
        ///     Gets a value indicating whether has status message.
        /// </summary>
        public bool HasStatusMessage
        {
            get
            {
                if (Target != null)
                    return Target.StatusMessage.Length > 0;

                return false;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is open.
        /// </summary>
        public bool IsOpen
        {
            get { return isOpen; }

            set
            {
                isOpen = value;
                OnPropertyChanged("IsOpen");
            }
        }

        /// <summary>
        ///     Gets the mark interested.
        /// </summary>
        public string MarkInterested
        {
            get
            {
                if (Target != null)
                {
                    return ApplicationSettings.Interested.Contains(Target.Name)
                        ? "Remove interested mark"
                        : "Add interested mark";
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
                if (Target != null)
                {
                    return ApplicationSettings.NotInterested.Contains(Target.Name)
                        ? "Remove not interested mark"
                        : "Add not interested mark";
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
                if (Target != null)
                {
                    switch (Target.Gender)
                    {
                        case Gender.HermF:
                            return "Feminine Herm";
                        case Gender.HermM:
                            return "Masculine Herm";
                        default:
                            return Target.Gender.ToString();
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
                if (Target != null)
                {
                    switch (Target.Status)
                    {
                        case StatusType.Dnd:
                            return "Do Not Disturb";
                        case StatusType.Looking:
                            return "Looking For Play";
                        default: // we just need to capitalize the first letter
                            return char.ToUpper(Target.Status.ToString()[0])
                                   + Target.Status.ToString().Substring(1);
                    }
                }

                return "None";
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The set new target.
        /// </summary>
        /// <param name="newTarget">
        ///     The target.
        /// </param>
        /// <param name="canIgnore">
        ///     The can ignore.
        /// </param>
        /// <param name="canUnignore">
        ///     The can unignore.
        /// </param>
        /// <param name="thisHasReports">
        ///     The has reports.
        /// </param>
        public void SetNewTarget(ICharacter newTarget, bool canIgnore, bool canUnignore, bool thisHasReports)
        {
            Target = newTarget;
            Target.GetAvatar();

            CanIgnore = canIgnore;
            CanUnignore = canUnignore;
            hasReports = thisHasReports;

            OnPropertyChanged("Target");
            OnPropertyChanged("CanIgnore");
            OnPropertyChanged("CanUnignore");
            OnPropertyChanged("TargetGender");
            OnPropertyChanged("TargetStatus");
            OnPropertyChanged("HasStatus");
            OnPropertyChanged("HasReport");
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            Target = null;
            base.Dispose(isManaged);
        }

        #endregion
    }
}