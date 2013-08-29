using Models;
/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViewModels
{
    public sealed class RightClickMenuViewModel : SysProp, IDisposable
    {
        #region Fields
        private bool _isOpen = false;

        private bool _canIgnore;
        private bool _canUnignore;
        private readonly bool _isModerator;
        private bool _hasReports;
        private ICharacter _target;
        #endregion

        #region constructors
        public RightClickMenuViewModel(bool isModerator)
        {
            _isModerator = isModerator;
        }
        #endregion

        #region Properties
        public ICharacter Target
        { 
            get { return _target; }
        }

        public bool IsOpen
        {
            get { return _isOpen; }
            set
            {
                _isOpen = value;
                OnPropertyChanged("IsOpen");
            }
        }

        public bool CanIgnore { get { return _canIgnore; } }
        public bool CanUnignore { get { return _canUnignore; } }
        public string MarkInterested
        {
            get
            {
                if (Target != null)
                {
                    if (ApplicationSettings.Interested.Contains(Target.Name))
                        return "Remove interested mark";
                    else return "Add interested mark";
                }
                else return string.Empty;
            }
        }
        public string MarkUninterested
        {
            get
            {
                if (Target != null)
                {
                    if (ApplicationSettings.NotInterested.Contains(Target.Name))
                        return "Remove not interested mark";
                    else return "Add not interested mark";
                }
                else return string.Empty;
            }
        }

        public string TargetStatus
        {
            get
            {
                if (Target != null)
                {
                    switch (Target.Status)
                    {
                        case StatusType.dnd: return "Do Not Disturb";
                        case StatusType.looking: return "Looking For Play";
                        default: // we just need to capitalize the first letter
                            return char.ToUpper(Target.Status.ToString()[0]) + Target.Status.ToString().Substring(1);
                    }
                }
                else return "None";
            }
        }

        public string TargetGender
        {
            get
            {
                if (Target != null)
                {
                    switch (Target.Gender)
                    {
                        case Gender.Herm_F: return "Feminine Herm";
                        case Gender.Herm_M: return "Masculine Herm";
                        default: return Target.Gender.ToString();
                    }
                }
                return "None";
            }
        }

        public bool HasStatusMessage { get { if (_target != null) return _target.StatusMessage.Length > 0; else return false; } }

        public bool HasReport { get { return _isModerator && _hasReports; } }
        #endregion

        #region Methods
        public void Dispose()
        {
            Dipose(true);
        }

        private void Dipose(bool IsManaged)
        {
            _target = null;
        }

        public void SetNewTarget(ICharacter target, bool canIgnore, bool canUnignore, bool hasReports)
        {
            _target = target;
            _target.GetAvatar();

            _canIgnore = canIgnore;
            _canUnignore = canUnignore;
            _hasReports = hasReports;

            OnPropertyChanged("Target");
            OnPropertyChanged("CanIgnore");
            OnPropertyChanged("CanUnignore");
            OnPropertyChanged("TargetGender");
            OnPropertyChanged("TargetStatus");
            OnPropertyChanged("HasStatus");
            OnPropertyChanged("HasReport");
        }
        #endregion
    }
}
