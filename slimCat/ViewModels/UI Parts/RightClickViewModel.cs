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

namespace slimCat.ViewModels
{
    #region Usings

    using Models;
    using Services;
    using Utilities;

    #endregion

    /// <summary>
    ///     The right click menu view model.
    /// </summary>
    public sealed class RightClickMenuViewModel : SysProp
    {
        #region Fields

        private readonly bool isModerator;
        private readonly ICharacterManager manager;
        private readonly IPermissionService permissionService;

        private bool hasReports;

        private bool isOpen;

        #endregion

        #region Constructors and Destructors

        public RightClickMenuViewModel(bool isModerator, ICharacterManager manager, IPermissionService permissionService)
        {
            this.isModerator = isModerator;
            this.manager = manager;
            this.permissionService = permissionService;
        }

        #endregion

        #region Public Properties

        public bool CanIgnoreUpdates
        {
            get
            {
                if (Target != null)
                    return manager.IsOfInterest(Target.Name, false);

                return false;
            }
        }

        public bool HasReport
        {
            get { return isModerator && hasReports; }
        }

        public bool HasStatusMessage
        {
            get
            {
                if (Target != null)
                    return Target.StatusMessage.Length > 0;

                return false;
            }
        }

        public bool IsOpen
        {
            get { return isOpen; }

            set
            {
                if (isOpen == value) return;

                Logging.LogLine((value ? "Opening" : "Closing") + " modal", "right-click vm");

                isOpen = value;
                OnPropertyChanged("IsOpen");
            }
        }

        public string MarkInterested
        {
            get
            {
                if (Target != null)
                {
                    return manager.IsOnList(Target.Name, ListKind.Interested)
                        ? "Remove interested mark"
                        : "Add interested mark";
                }

                return string.Empty;
            }
        }

        public string MarkUninterested
        {
            get
            {
                if (Target != null)
                {
                    return manager.IsOnList(Target.Name, ListKind.NotInterested)
                        ? "Remove not interested mark"
                        : "Add not interested mark";
                }

                return string.Empty;
            }
        }

        public string IgnoreUpdate
        {
            get
            {
                if (Target != null)
                {
                    return manager.IsOnList(Target.Name, ListKind.IgnoreUpdates)
                        ? "Unignore updates"
                        : "Ignore updates";
                }

                return string.Empty;
            }
        }

        public ICharacter Target { get; private set; }

        public string TargetGender
        {
            get
            {
                if (Target == null) return "None";

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
        }

        public bool IsChannelModerator
        {
            get
            {
                if (Target == null) return false;

                return permissionService.IsChannelModerator(Target.Name);
            }
        }

        public bool IsGlobalModerator
        {
            get
            {
                if (Target == null) return false;

                return permissionService.IsAdmin(Target.Name);
            }
        }
        public bool IsIgnored
        {
            get
            {
                if (Target != null)
                    return manager.IsOnList(Target.Name, ListKind.Ignored, false);

                return false;
            }
        }

        public bool IsBookmarked
        {
            get
            {
                if (Target != null)
                    return manager.IsOnList(Target.Name, ListKind.Bookmark, false);

                return false;
            }
        }

        public bool IsFriend
        {
            get
            {
                if (Target != null)
                    return manager.IsOnList(Target.Name, ListKind.Friend, false);

                return false;
            }
        }

        public bool IsOfInterest
        {
            get
            {
                if (Target != null)
                    return manager.IsOnList(Target.Name, ListKind.Interested, false);

                return false;
            }
        }

        public bool IsUninteresting
        {
            get
            {
                if (Target != null)
                    return manager.IsOnList(Target.Name, ListKind.NotInterested, false);

                return false;
            }
        }

        public bool IsUpdatesIgnored
        {
            get
            {
                if (Target == null) return false;

                return manager.IsOnList(Target.Name, ListKind.IgnoreUpdates, false);
            }
        }

        public string TargetStatus
        {
            get
            {
                if (Target == null) return "None";

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
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Sets the new target.
        /// </summary>
        /// <param name="newTarget">The new target.</param>
        /// <param name="thisHasReports">if set to <c>true</c> the character has reports.</param>
        public void SetNewTarget(ICharacter newTarget, bool thisHasReports)
        {
            Target = newTarget;
            Target.GetAvatar();

            Logging.LogLine("target set to \"{0}\"".FormatWith(Target.Name), "right-click vm");

            hasReports = thisHasReports;

            OnPropertyChanged("Target");
            OnPropertyChanged("CanIgnore");
            OnPropertyChanged("CanUnignore");

            OnPropertyChanged("TargetGender");
            OnPropertyChanged("TargetStatus");

            OnPropertyChanged("HasStatus");
            OnPropertyChanged("HasReport");

            OnPropertyChanged("CanIgnoreUpdates");

            OnPropertyChanged("IsChannelModerator");
            OnPropertyChanged("IsGlobalModerator");

            OnPropertyChanged("IsIgnored");
            OnPropertyChanged("IsBookmarked");
            OnPropertyChanged("IsFriend");

            OnPropertyChanged("IsOfInterest");
            OnPropertyChanged("IsUninteresting");

            OnPropertyChanged("IsUpdatesIgnored");
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