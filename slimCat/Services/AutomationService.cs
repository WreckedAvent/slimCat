#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AutomationService.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using System.Timers;
    using lib;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Utilities;

    #endregion

    public class AutomationService : IAutomationService
    {
        private const int OneMinute = 1000*60;
        private readonly Timer awayTimer;
        private readonly IChatModel cm;
        private readonly IEventAggregator events;
        private readonly Timer fullscreenTimer = new Timer(2*OneMinute);
        private readonly Timer idleTimer;
        private readonly ICharacterManager manager;

        public AutomationService(IEventAggregator events, ICharacterManager manager, IChatModel cm)
        {
            this.events = events;
            this.manager = manager;
            this.cm = cm;

            idleTimer = new Timer(ApplicationSettings.AutoIdleTime*OneMinute);
            awayTimer = new Timer(ApplicationSettings.AutoAwayTime*OneMinute);

            idleTimer.Elapsed += IdleTimerOnElapsed;
            awayTimer.Elapsed += AwayTimerOnElapsed;
            fullscreenTimer.Elapsed += FullscreenTimerOnElapsed;

            events.GetEvent<UserCommandEvent>().Subscribe(OnUserCommandSent);
        }

        public void ResetStatusTimers()
        {
            idleTimer.Stop();
            awayTimer.Stop();
            fullscreenTimer.Stop();

            idleTimer.Interval = ApplicationSettings.AutoIdleTime*OneMinute;
            awayTimer.Interval = ApplicationSettings.AutoAwayTime*OneMinute;

            if (ApplicationSettings.AllowAutoIdle) idleTimer.Start();
            if (ApplicationSettings.AllowAutoAway) awayTimer.Start();
            if (ApplicationSettings.AllowAutoBusy) fullscreenTimer.Start();
        }

        public bool IsDuplicateAd(string name, string message)
        {
            if (!ApplicationSettings.AllowAdDedup) return false;

            var character = manager.Find(name);
            if (character.LastAd != null && character.LastAd == message)
                return true;

            character.LastAd = message;
            return false;
        }

        public void UserDidAction()
        {
            OnUserCommandSent(new Dictionary<string, object> {{"type", "override"}});
        }

        private void FullscreenTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (cm.CurrentCharacter == null 
                || !ApplicationSettings.AllowAutoBusy) return;

            if (cm.CurrentCharacter.Status != StatusType.Online
                && cm.CurrentCharacter.Status != StatusType.Idle
                && cm.CurrentCharacter.Status != StatusType.Away) return;

            if (!FullScreenHelper.ForegroundIsFullScreen()) return;

            cm.CurrentCharacter.Status = StatusType.Busy;
            events.SendUserCommand("busy", new[] {cm.CurrentCharacter.StatusMessage});
            fullscreenTimer.Stop();
        }

        private void OnUserCommandSent(IDictionary<string, object> obj)
        {
            if (obj.ContainsKey(Constants.Arguments.Status))
                return;

            if (cm.CurrentCharacter == null) return;

            ResetStatusTimers();

            if (!ApplicationSettings.AllowStatusAutoReset) return;

            if (cm.CurrentCharacter.Status != StatusType.Idle
                && cm.CurrentCharacter.Status != StatusType.Away) return;

            cm.CurrentCharacter.Status = StatusType.Online;
            events.SendUserCommand("online", new[] {cm.CurrentCharacter.StatusMessage});
        }

        private void AwayTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (cm.CurrentCharacter == null 
                || cm.CurrentCharacter.Status == StatusType.Away
                || !ApplicationSettings.AllowAutoAway) return;

            cm.CurrentCharacter.Status = StatusType.Away;
            events.SendUserCommand("away", new[] {cm.CurrentCharacter.StatusMessage});

            awayTimer.Stop();
        }

        private void IdleTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (cm.CurrentCharacter == null 
                || cm.CurrentCharacter.Status != StatusType.Online 
                || !ApplicationSettings.AllowAutoIdle) return;

            cm.CurrentCharacter.Status = StatusType.Idle;
            events.SendUserCommand("idle", new[] {cm.CurrentCharacter.StatusMessage});

            idleTimer.Stop();
        }
    }
}