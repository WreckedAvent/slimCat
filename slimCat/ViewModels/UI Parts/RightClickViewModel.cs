using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models
{
    public class RightClickMenuViewModel : SysProp, IDisposable
    {
        #region Fields
        private bool _isOpen = false;

        private bool _canIgnore;
        private bool _canUnignore;
        private bool _canMark;
        private bool _canUnMark;

        private ICharacter _target;
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
        public bool CanMark { get { return _canMark; } }
        public bool CanUnmark { get { return _canUnMark; } }

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

        public void SetNewTarget(ICharacter target, bool canIgnore, bool canUnignore, bool canMarkAsInterested, bool canMarkAsNotInterested)
        {
            _target = target;
            _target.GetAvatar();

            _canIgnore = canIgnore;
            _canUnignore = canUnignore;
            _canMark = canMarkAsInterested;
            _canUnMark = canMarkAsNotInterested;

            OnPropertyChanged("Target");
            OnPropertyChanged("CanIgnore");
            OnPropertyChanged("CanUnignore");
            OnPropertyChanged("CanMark");
            OnPropertyChanged("CanUnmark");
            OnPropertyChanged("TargetGender");
            OnPropertyChanged("TargetStatus");
        }
        #endregion
    }
}
