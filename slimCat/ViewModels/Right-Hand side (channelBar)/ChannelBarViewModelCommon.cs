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
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
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
using System.Windows.Input;
using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;

namespace ViewModels
{
    /// <summary>
    /// Contains some things the channelbar viewmodels have in common
    /// </summary>
    public class ChannelbarViewModelCommon : ViewModelBase
    {
        #region Fields
        private GenericSearchSettingsModel _searchSettings = new GenericSearchSettingsModel();
        public GenericSearchSettingsModel SearchSettings { get { return _searchSettings; } }
        #endregion

        #region Constructors
        public ChannelbarViewModelCommon(IUnityContainer contain, IRegionManager regman,
                                          IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            cm.SelectedChannelChanged += (s, e) =>
                {
                    OnPropertyChanged("HasUsers");
                };
        }

        public override void Initialize() { }

        public bool HasUsers
        {
            get
            {
                if (CM.SelectedChannel == null) return false;

                return ((CM.SelectedChannel.Type != ChannelType.pm)
                    && CM.SelectedChannel.Type != ChannelType.utility);
            }
        }
        #endregion
    }
}
