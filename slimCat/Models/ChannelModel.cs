using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using lib;
using Microsoft.Practices.Prism.Events;

namespace Models
{
    /// <summary>
    /// The channel model is used as a base for channels and conversations
    /// </summary>
    public abstract class ChannelModel : SysProp, IDisposable
    {
        #region Fields
        private ChannelType _type;
        private ChannelMode _mode;
        protected ObservableCollection<IMessage> _messages = new ObservableCollection<IMessage>();
        protected ObservableCollection<IMessage> _ads = new ObservableCollection<IMessage>();
        protected ChannelSettingsModel _settings;
        private string _title;
        private int _lastRead;
        private bool _isSelected = false;
        private readonly string _identity; // an ID never changes
        #endregion

        #region Properties
        /// <summary>
        /// An ID is used to unambigiously identify the channel or character's name
        /// </summary>
        public string ID { get { return _identity; } }

        public ChannelType Type
        {
            get { return _type; }
            set { _type = value; OnPropertyChanged("Type"); }
        }

        public ObservableCollection<IMessage> Messages { get { return _messages; } }

        public ObservableCollection<IMessage> Ads { get { return _ads; } }

        public ChannelMode Mode
        {
            get { return _mode; }
            set { _mode = value; OnPropertyChanged("Mode"); }
        }

        public string Title
        {
            get
            {
                return (_title == null? ID : _title );
            }
            set
            {
                _title = value; OnPropertyChanged("Title");
            }
        }

        /// <summary>
        /// Used to determine if the channel should make itself more visible on the UI
        /// </summary>
        public virtual bool NeedsAttention
        {
            get { return (!IsSelected) && (Unread >= Settings.FlashInterval && Settings.ShouldFlash); }
        }

        /// <summary>
        /// A number displayed on the UI along with the rest of the channel data
        /// </summary>
        public abstract int DisplayNumber { get; }

        /// <summary>
        /// Number of messages we haven't read
        /// </summary>
        public int Unread
        {
            get { return Messages.Count - LastReadCount; }
        }

        /// <summary>
        /// The number of messages we've read up to
        /// </summary>
        public virtual int LastReadCount
        {
            get { return _lastRead; }
            set
            {
                if (_lastRead != value)
                {
                    _lastRead = value;
                    UpdateBindings();
                }
            }
        }

        public virtual ChannelSettingsModel Settings { get { return _settings; } }

        /// <summary>
        /// If the channel is selected or not
        /// </summary>
        public virtual bool IsSelected
        {
            get { return _isSelected; }

            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    if (value)
                        LastReadCount = Messages.Count;
                    UpdateBindings();
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public virtual bool CanClose { get { return IsSelected; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Channel data model
        /// </summary>
        /// <param name="identity">Name of the channel (or character, for PMs)</param>
        /// <param name="kind">Type of the channel</param>
        /// <param name="mode">The rules associated with the channel (only ads, only posts, or both)</param>
        public ChannelModel(string identity, ChannelType kind, ChannelMode mode = ChannelMode.both)
        {
            try
            {
                if (identity == null) throw new ArgumentNullException("identity");

                _identity = identity;
                Type = kind;
                Mode = mode;
                LastReadCount = 0;
            }

            catch (Exception ex)
            {
                ex.Source = "Channel Model, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Updates the bound data so the UI can react accordingly
        /// </summary>
        protected virtual void UpdateBindings()
        {
            OnPropertyChanged("NeedsAttention");
            OnPropertyChanged("DisplayNumber");
            OnPropertyChanged("CanClose");
        }

        public virtual void AddMessage(IMessage message)
        {
            if (_messages.Count >= ApplicationSettings.BackLogMax)
                _messages.RemoveAt(0);

            _messages.Add(message);

            if (_isSelected)
                _lastRead = _messages.Count;

            UpdateBindings();
        }
        #endregion

        #region IDispose
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _messages.Clear();
                _ads.Clear();
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents the possible channel types
    /// </summary>
    public enum ChannelType
    {
        /* Official Channels */
        pub,
        // pub channels are official channels which are open to the public and abide by F-list's rules and moderation.


        /* Private Channels */
        priv,
        // priv channels are private channels which are open to the public

        pm,
        // pm channels are private channels which can only be accessed by two characters

        closed,
        // closed channels are private channels which can only be joined with an outstanding invite

        utility, 
        // utility channels are channels which have custom functionality, such as the home page
    }

    /// <summary>
    /// Represents possible channel modes
    /// </summary>
    public enum ChannelMode
    {
        ads,
        // ad-only channels, e.g LfRP

        chat,
        // no-ad channels, e.g. most private channels

        both,
        // both messages and ads, e.g most public channels
    }
}
