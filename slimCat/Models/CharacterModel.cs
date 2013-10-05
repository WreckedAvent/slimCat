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

namespace Slimcat.Models
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Net;
    using System.Net.Cache;
    using System.Windows.Media.Imaging;

    using Slimcat.ViewModels;

    /// <summary>
    ///     A character model which stores data related to characters
    /// </summary>
    public sealed class CharacterModel : SysProp, ICharacter, IDisposable
    {
        #region Fields

        private BitmapImage avatar;

        private Gender gender;

        private string name = string.Empty;

        private ReportModel lastReport;

        private StatusType status;

        private string statusMessage = string.Empty;

        private bool isInteresting;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the avatar.
        /// </summary>
        public BitmapImage Avatar
        {
            get
            {
                return this.avatar;
            }

            set
            {
                this.avatar = value;
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
                return this.gender;
            }

            set
            {
                this.gender = value;
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
                return this.lastReport != null;
            }
        }

        /// <summary>
        ///     Gets or sets the last report.
        /// </summary>
        public ReportModel LastReport
        {
            get
            {
                return this.lastReport;
            }

            set
            {
                this.lastReport = value;
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
                return this.name;
            }

            set
            {
                this.name = value;
                this.OnPropertyChanged("Name");
            }
        }

        /// <summary>
        ///     Gets or sets the status.
        /// </summary>
        public StatusType Status
        {
            get
            {
                return this.status;
            }

            set
            {
                this.status = value;
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
                return this.statusMessage;
            }

            set
            {
                this.statusMessage = value;
                this.OnPropertyChanged("StatusMessage");
            }
        }

        public bool IsInteresting
        {
            get
            {
                return this.isInteresting;
            }

            set
            {
                this.isInteresting = value;
                this.OnPropertyChanged("IsInteresting");
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The get avatar.
        /// </summary>
        public void GetAvatar()
        {
            if (this.name == null)
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
                            var imageBytes = webClient.DownloadData(uri);

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
        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                this.Name = null;
                this.StatusMessage = null;
                this.Avatar.StreamSource.Dispose();
                this.lastReport.Dispose();
            }

            base.Dispose(isManaged);
        }

        #endregion
    }
}