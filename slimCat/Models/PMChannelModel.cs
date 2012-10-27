using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using slimCat;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using SimpleJson;
using ViewModels;
using System.Collections.Specialized;

namespace Models
{
    /// <summary>
    /// Used for Private-Message communication between users
    /// </summary>
    public sealed class PMChannelModel : ChannelModel, IDisposable
    {
        #region Fields
        private int _lastCount;
        private ICharacter _PMCharacter;
        private Typing_Status _typing;
        private System.Timers.Timer _tick;
        private StringBuilder _isTypingString;
        #endregion

        #region Properties
        public int Unread
        {
            get 
            {
                if (!IsSelected)
                    return Messages.Count - LastUnreadCount;
                else
                    return 0;
            }
        }

        public int LastUnreadCount
        {
            get { return _lastCount; }
            set
            { 
                _lastCount = value;
                UpdateBindings();
            }
        }

        public ICharacter PMCharacter
        {
            get { return _PMCharacter; }
            set { _PMCharacter = value; OnPropertyChanged("PMCharacter"); }
        }

        public Typing_Status TypingStatus
        {
            get { return _typing; }
            set { 
                _typing = value; 
                OnPropertyChanged("TypingStatus");
                OnPropertyChanged("TypingString");

                if (value == Typing_Status.typing)
                    _tick.Enabled = true;
                else
                    _tick.Enabled = false;
            }
        }

        public String TypingString
        {
            get
            {
                switch (TypingStatus)
                {
                    case Typing_Status.paused:
                        return "~";
                    case Typing_Status.clear:
                        return "";
                    case Typing_Status.typing:
                        return _isTypingString.ToString();
                }

                return "";
            }
        }

        public override bool NeedsAttention
        {
            get { return (Unread > 0); }
        }

        public override int DisplayNumber
        {
            get { return Unread; }
        }

        public override bool IsSelected
        {
            get
            {
                return base.IsSelected;
            }
            set
            {
                if (base.IsSelected != value)
                {
                    base.IsSelected = value;
                    UpdateBindings();
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Channel data model which will always be a PM
        /// </summary>
        /// <param name="character">The character that sent the message</param>
        public PMChannelModel(ICharacter character) 
            : base(character.Name, ChannelType.pm)

        {
            PMCharacter = character;
            PMCharacter.GetAvatar();
            LastUnreadCount = 0;

            _tick = new System.Timers.Timer(1000);
            _isTypingString = new StringBuilder();

            Messages.CollectionChanged += OnCollectionChanged;

            #region Disposable
            _tick.Elapsed += (s, e) =>
                {
                    if (_isTypingString.Length < 3)
                        _isTypingString.Append(".");
                    else
                    {
                        _isTypingString.Clear();
                        _isTypingString.Append(".");
                    }

                    OnPropertyChanged("TypingString");
                };
            #endregion
        }
        #endregion

        #region Event Methods
        private void OnCollectionChanged(object s, NotifyCollectionChangedEventArgs e)
        {
            UpdateBindings();
            if (IsSelected)
                LastUnreadCount = Messages.Count;
        }
        #endregion

        protected override void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                Messages.CollectionChanged -= OnCollectionChanged;
                _tick.Dispose();
                _tick = null;
                _isTypingString = null;
            }

            base.Dispose(IsManaged);
        }
    }

    /// <summary>
    /// The possible states of typing
    /// </summary>
    public enum Typing_Status
    {
        clear,
        paused,
        typing
    }
}
