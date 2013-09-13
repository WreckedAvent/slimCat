// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterModel.cs" company="Justin Kadrovach">
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
//   A character model which stores data related to characters
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Models
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Net;
    using System.Net.Cache;
    using System.Windows.Media.Imaging;

    /// <summary>
    ///     A character model which stores data related to characters
    /// </summary>
    public class CharacterModel : SysProp, ICharacter, IDisposable
    {
        #region Fields

        private BitmapImage _avatar;

        private Gender _gender;

        private string _name = string.Empty;

        private Orientation _orient;

        private ReportModel _report;

        private StatusType _status;

        private string _statusMessage = string.Empty;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the avatar.
        /// </summary>
        public BitmapImage Avatar
        {
            get
            {
                return this._avatar;
            }

            set
            {
                this._avatar = value;
                this.OnPropertyChanged("Avatar");
            }
        }

        /// <summary>
        ///     Gets or sets the gender.
        /// </summary>
        public Gender Gender
        {
            get
            {
                return this._gender;
            }

            set
            {
                this._gender = value;
                this.OnPropertyChanged("Gender");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has report.
        /// </summary>
        public bool HasReport
        {
            get
            {
                return this._report != null;
            }
        }

        /// <summary>
        ///     Gets or sets the last report.
        /// </summary>
        public ReportModel LastReport
        {
            get
            {
                return this._report;
            }

            set
            {
                this._report = value;
                this.OnPropertyChanged("LastReport");
                this.OnPropertyChanged("HasReport");
            }
        }

        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        public string Name
        {
            get
            {
                return this._name;
            }

            set
            {
                this._name = value;
                this.OnPropertyChanged("Name");
            }
        }

        /// <summary>
        ///     Gets or sets the orientation.
        /// </summary>
        public Orientation Orientation
        {
            get
            {
                return this._orient;
            }

            set
            {
                this._orient = value;
                this.OnPropertyChanged("Orientation");
            }
        }

        /// <summary>
        ///     Gets or sets the status.
        /// </summary>
        public StatusType Status
        {
            get
            {
                return this._status;
            }

            set
            {
                this._status = value;
                this.OnPropertyChanged("Status");
            }
        }

        /// <summary>
        ///     Gets or sets the status message.
        /// </summary>
        public string StatusMessage
        {
            get
            {
                return this._statusMessage;
            }

            set
            {
                this._statusMessage = value;
                this.OnPropertyChanged("StatusMessage");
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        ///     The get avatar.
        /// </summary>
        public void GetAvatar()
        {
            if (this._name == null)
            {
                return;
            }

            var worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
                {
                    var uri = new Uri((string)e.Argument, UriKind.Absolute);

                    using (var webClient = new WebClient())
                    {
                        webClient.Proxy = null; // avoids dynamic proxy discovery delay
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

                            var imageStream = new MemoryStream(imageBytes);
                            var image = new BitmapImage();

                            image.BeginInit();
                            image.StreamSource = imageStream;
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.EndInit();

                            image.Freeze();
                            imageStream.Close();

                            e.Result = image;
                        }
                        catch (Exception)
                        {
                        }
                    }
                };

            worker.RunWorkerCompleted += (s, e) =>
                {
                    var bitmapImage = e.Result as BitmapImage;
                    if (bitmapImage != null)
                    {
                        this.Avatar = bitmapImage;
                    }

                    worker.Dispose();
                };

            worker.RunWorkerAsync("http://static.f-list.net/images/avatar/" + this.Name.ToLower() + ".png");
        }

        #endregion

        #region Methods

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="IsManaged">
        /// The is managed.
        /// </param>
        protected virtual void Dispose(bool IsManaged)
        {
            this.Name = null;
            this.StatusMessage = null;
            this.Avatar.StreamSource.Dispose();
            this._report.Dispose();
        }

        #endregion
    }

    /// <summary>
    ///     For everything that interacts directly with character data
    /// </summary>
    public interface ICharacter
    {
        #region Public Properties

        /// <summary>
        ///     Call GetAvatar before this is used
        /// </summary>
        BitmapImage Avatar { get; set; }

        /// <summary>
        ///     Gets or sets the gender.
        /// </summary>
        Gender Gender { get; set; }

        /// <summary>
        ///     Gets a value indicating whether has report.
        /// </summary>
        bool HasReport { get; }

        /// <summary>
        ///     Gets or sets the last report.
        /// </summary>
        ReportModel LastReport { get; set; }

        /// <summary>
        ///     The full name is the character's gender, op status, and name in one line
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///     Gets or sets the orientation.
        /// </summary>
        Orientation Orientation { get; set; }

        /// <summary>
        ///     Gets or sets the status.
        /// </summary>
        StatusType Status { get; set; }

        /// <summary>
        ///     Gets or sets the status message.
        /// </summary>
        string StatusMessage { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The get avatar.
        /// </summary>
        void GetAvatar();

        #endregion
    }

    #region Enums

    /// <summary>
    ///     Types of available status
    /// </summary>
    public enum StatusType
    {
        /// <summary>
        ///     The offline.
        /// </summary>
        offline, 

        /// <summary>
        ///     The online.
        /// </summary>
        online, 

        /// <summary>
        ///     The away.
        /// </summary>
        away, 

        /// <summary>
        ///     The busy.
        /// </summary>
        busy, 

        /// <summary>
        ///     The looking.
        /// </summary>
        looking, 

        /// <summary>
        ///     The idle.
        /// </summary>
        idle, 

        /// <summary>
        ///     The dnd.
        /// </summary>
        dnd, 

        /// <summary>
        ///     The crown.
        /// </summary>
        crown, 
    }

    /// <summary>
    ///     Orientation selections
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        ///     The none.
        /// </summary>
        None, 

        /// <summary>
        ///     The straight.
        /// </summary>
        Straight, 

        /// <summary>
        ///     The gay.
        /// </summary>
        Gay, 

        /// <summary>
        ///     The bisexual_ mpref.
        /// </summary>
        Bisexual_Mpref, 

        /// <summary>
        ///     The bisexual_ fpref.
        /// </summary>
        Bisexual_Fpref, 

        /// <summary>
        ///     The bisexual.
        /// </summary>
        Bisexual, 

        /// <summary>
        ///     The asexual.
        /// </summary>
        Asexual, 

        /// <summary>
        ///     The pansexual.
        /// </summary>
        Pansexual, 

        /// <summary>
        ///     The unsure.
        /// </summary>
        Unsure, 

        /// <summary>
        ///     The bisexual_ curios.
        /// </summary>
        Bisexual_Curios
    }

    /// <summary>
    ///     Gender selections
    /// </summary>
    public enum Gender
    {
        /// <summary>
        ///     The none.
        /// </summary>
        None, 

        /// <summary>
        ///     The male.
        /// </summary>
        Male, 

        /// <summary>
        ///     The female.
        /// </summary>
        Female, 

        /// <summary>
        ///     The herm_ f.
        /// </summary>
        Herm_F, 

        /// <summary>
        ///     The shemale.
        /// </summary>
        Shemale, 

        /// <summary>
        ///     The cuntboy.
        /// </summary>
        Cuntboy, 

        /// <summary>
        ///     The herm_ m.
        /// </summary>
        Herm_M, 

        /// <summary>
        ///     The transgender.
        /// </summary>
        Transgender
    }

    /// <summary>
    ///     D/s selections
    /// </summary>
    public enum DomSubRoles
    {
        /// <summary>
        ///     The always_ dom.
        /// </summary>
        Always_Dom, 

        /// <summary>
        ///     The dom.
        /// </summary>
        Dom, 

        /// <summary>
        ///     The switch.
        /// </summary>
        Switch, 

        /// <summary>
        ///     The sub.
        /// </summary>
        Sub, 

        /// <summary>
        ///     The always_ sub.
        /// </summary>
        Always_Sub, 
    }

    #endregion

    /// <summary>
    ///     A model for storing data related to character reports
    /// </summary>
    public sealed class ReportModel : IDisposable
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the call id.
        /// </summary>
        public string CallId { get; set; }

        /// <summary>
        ///     Gets or sets the complaint.
        /// </summary>
        public string Complaint { get; set; }

        /// <summary>
        ///     Gets or sets the log id.
        /// </summary>
        public int? LogId { get; set; }

        /// <summary>
        ///     Gets or sets the reported.
        /// </summary>
        public string Reported { get; set; }

        /// <summary>
        ///     Gets or sets the reporter.
        /// </summary>
        public ICharacter Reporter { get; set; }

        /// <summary>
        ///     Gets or sets the tab.
        /// </summary>
        public string Tab { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        #endregion

        #region Methods

        private void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                this.Complaint = null;
                this.Reporter = null;
            }
        }

        #endregion
    }
}