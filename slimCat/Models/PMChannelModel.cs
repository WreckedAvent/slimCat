using System;
using System.Text;

namespace Models
{
    /// <summary>
    /// Used for Private-Message communication between users
    /// </summary>
    public sealed class PMChannelModel : ChannelModel, IDisposable
    {
        #region Fields
        private ICharacter _PMCharacter;
        private Typing_Status _typing;
        private System.Timers.Timer _tick;
        private StringBuilder _isTypingString;
        #endregion

        #region Properties
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

        public override int DisplayNumber
        {
            get { return Unread; }
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

            _tick = new System.Timers.Timer(1000);
            _isTypingString = new StringBuilder();
            _settings = new ChannelSettingsModel(true);

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

        protected override void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _tick.Dispose();
                _tick = null;
                _isTypingString = null;
                _settings = new ChannelSettingsModel(true);
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
