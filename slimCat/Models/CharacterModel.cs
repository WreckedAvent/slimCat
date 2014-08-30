#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterModel.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Net;
    using System.Net.Cache;
    using System.Web;
    using System.Windows.Media.Imaging;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     A character model which stores data related to characters
    /// </summary>
    public sealed class CharacterModel : SysProp, ICharacter
    {
        #region Fields

        private BitmapImage avatar;

        private Gender gender;
        private bool ignoreUpdates;
        private bool isInteresting;

        private ReportModel lastReport;
        private string name = string.Empty;

        private StatusType status;

        private string statusMessage = string.Empty;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the avatar.
        /// </summary>
        public BitmapImage Avatar
        {
            get { return avatar; }

            set
            {
                avatar = value;
                OnPropertyChanged("Avatar");
            }
        }

        /// <summary>
        ///     Gets or sets the gender.
        /// </summary>
        public Gender Gender
        {
            get { return gender; }

            set
            {
                gender = value;
                OnPropertyChanged("Gender");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has report.
        /// </summary>
        public bool HasReport
        {
            get { return lastReport != null; }
        }

        /// <summary>
        ///     Gets or sets the last report.
        /// </summary>
        public ReportModel LastReport
        {
            get { return lastReport; }

            set
            {
                lastReport = value;
                OnPropertyChanged("LastReport");
                OnPropertyChanged("HasReport");
            }
        }

        public Uri AvatarUri
        {
            get { return new Uri(Constants.UrlConstants.CharacterAvatar + HttpUtility.HtmlEncode(name).ToLower() + ".png", UriKind.Absolute); }
        }

        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        public string Name
        {
            get { return name; }

            set
            {
                name = value;
                OnPropertyChanged("Name");
                OnPropertyChanged("Uri");
            }
        }

        /// <summary>
        ///     Gets or sets the status.
        /// </summary>
        public StatusType Status
        {
            get { return status; }

            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }

        /// <summary>
        ///     Gets or sets the status message.
        /// </summary>
        public string StatusMessage
        {
            get { return statusMessage; }

            set
            {
                statusMessage = value;
                OnPropertyChanged("StatusMessage");
            }
        }

        public bool IsInteresting
        {
            get { return isInteresting; }

            set
            {
                isInteresting = value;
                OnPropertyChanged("IsInteresting");
            }
        }

        public bool IgnoreUpdates
        {
            get { return ignoreUpdates; }
            set
            {
                ignoreUpdates = value;
                OnPropertyChanged("IgnoreUpdates");
            }
        }

        public string LastAd { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The get avatar.
        /// </summary>
        public void GetAvatar()
        {
            if (name == null)
                return;

            var worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
                {
                    var uri = new Uri((string) e.Argument, UriKind.Absolute);

                    using (var webClient = new WebClient())
                    {
                        webClient.Proxy = null; // avoids dynamic proxy discovery delay
                        webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Revalidate);
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
                        Avatar = bitmapImage;

                    worker.Dispose();
                };

            worker.RunWorkerAsync(Constants.UrlConstants.CharacterAvatar + Name.ToLower() + ".png");
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                Name = null;
                StatusMessage = null;
                Avatar.StreamSource.Dispose();
                lastReport.Dispose();
            }

            base.Dispose(isManaged);
        }

        #endregion
    }
}