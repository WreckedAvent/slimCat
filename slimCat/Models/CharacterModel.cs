using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Models
{
    /// <summary>
    /// A character model which stores data related to characters
    /// </summary>
    public class CharacterModel : SysProp, ICharacter, IDisposable
    {
        #region Fields
        private string _name = "";
        private BitmapImage _avatar;
        private Orientation _orient;
        private Gender _gender;
        private StatusType _status;
        private string _statusMessage = "";
        #endregion

        #region Properties
        public string Name 
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        public BitmapImage Avatar
        {
            get { return _avatar; }
            set { _avatar = value; OnPropertyChanged("Avatar"); }
        }

        public Orientation Orientation
        {
            get { return _orient; }
            set { _orient = value; OnPropertyChanged("Orientation"); }
        }

        public Gender Gender
        {
            get { return _gender; }
            set { _gender = value; OnPropertyChanged("Gender"); }
        }

        public StatusType Status
        {
            get { return _status; }
            set
            { 
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        public String StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                OnPropertyChanged("StatusMessage");
            }
        }

        #endregion

        #region Methods
        public void GetAvatar()
        {
            if (_name == null)
                return;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                Uri uri = new Uri((string)e.Argument, UriKind.Absolute);

                using (WebClient webClient = new WebClient())
                {
                    webClient.Proxy = null;  //avoids dynamic proxy discovery delay
                    webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
                    try
                    {
                        byte[] imageBytes = null;
                        imageBytes = webClient.DownloadData(uri);

                        if (imageBytes == null)
                        {
                            e.Result = null;
                            return;
                        }
                        MemoryStream imageStream = new MemoryStream(imageBytes);
                        BitmapImage image = new BitmapImage();

                        image.BeginInit();
                        image.StreamSource = imageStream;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();

                        image.Freeze();
                        imageStream.Close();

                        e.Result = image;
                    }

                    catch (Exception) { }
                }
            };

            worker.RunWorkerCompleted += (s, e) =>
            {
               BitmapImage bitmapImage = e.Result as BitmapImage;
                if (bitmapImage != null)
                {
                    Avatar = bitmapImage;
                }
                worker.Dispose();
            };

            worker.RunWorkerAsync(
                "http://static.f-list.net/images/avatar/" + Name.ToLower() + ".png");
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool IsManaged)
        {
            Name = null;
            StatusMessage = null;
            // TODO: Release avatar from memory
        }
    }

    /// <summary>
    /// For everything that interacts directly with character data
    /// </summary>
    public interface ICharacter
    {
        /// <summary>
        /// The full name is the character's gender, op status, and name in one line
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Call GetAvatar before this is used
        /// </summary>
        BitmapImage Avatar { get; set; }


        Orientation Orientation { get; set; }
        Gender Gender { get; set; }
        StatusType Status { get; set; }
        string StatusMessage { get; set; }

        void GetAvatar();
    }

    #region Enums
    /// <summary>
    /// Types of available status
    /// </summary>
    public enum StatusType
    {
        None,
        online,
        away,
        busy,
        looking,
        idle,
        dnd,
        crown,
    }

    /// <summary>
    /// Orientation selections
    /// </summary>
    public enum Orientation
    {
        None,
        Straight,
        Gay,
        Bisexual_Mpref,
        Bisexual_Fpref,
        Bisexual,
        Asexual,
        Pansexual,
        Unsure,
        Bisexual_Curios
    }

    /// <summary>
    /// Gender selections
    /// </summary>
    public enum Gender
    {
        None,
        Male,
        Female,
        Herm_F,
        Shemale,
        Cuntboy,
        Herm_M,
        Transgender
    }

    /// <summary>
    /// D/s selections
    /// </summary>
    public enum DomSubRoles
    {
        Always_Dom,
        Dom,
        Switch,
        Sub,
        Always_Sub,
    }
    #endregion
}
