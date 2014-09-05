#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatWrapperView.xaml.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//  
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Views
{
    #region Usings

    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for ChatWrapperView.xaml
    /// </summary>
    public partial class ChatWrapperView
    {
        #region Constants

        /// <summary>
        ///     The channelbar region.
        /// </summary>
        public const string ChannelbarRegion = "ChannelbarRegion";

        /// <summary>
        ///     The conversation region.
        /// </summary>
        public const string ConversationRegion = "ConversationRegion";

        /// <summary>
        ///     The userbar region.
        /// </summary>
        public const string UserbarRegion = "UserbarRegion";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChatWrapperView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public ChatWrapperView(ChatWrapperViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        #endregion
    }
}