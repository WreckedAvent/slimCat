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

    using lib;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Timers;
    using Utilities;

    #endregion

    public class AutomationService : IAutomationService
    {
        #region Fields
        private const int OneMinute = 1000*60;
        private readonly Timer awayTimer;
        private readonly IChatModel cm;
        private readonly IEventAggregator events;
        private readonly Timer fullscreenTimer = new Timer(2*OneMinute);
        private readonly Timer idleTimer;
        private readonly ICharacterManager manager;
        #endregion

        #region Constructors
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
        #endregion

        #region Methods
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
            if (character.LastAd != null && (ApplicationSettings.AllowAggressiveAdDedup || character.LastAd == message))
            {
                Logging.Log("Duplicate ad from " + name);
                return true;
            }

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
                || !ApplicationSettings.AllowAutoBusy
                || cm.CurrentCharacter.Status == StatusType.Busy) return;

            if (cm.CurrentCharacter.Status != StatusType.Online
                && cm.CurrentCharacter.Status != StatusType.Idle
                && cm.CurrentCharacter.Status != StatusType.Away) return;

            if (!FullScreenHelper.ForegroundIsFullScreen()) return;

            Log("Setting user status to busy");
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

            Log("Resetting user status to online");
            cm.CurrentCharacter.Status = StatusType.Online;
            events.SendUserCommand("online", new[] {cm.CurrentCharacter.StatusMessage});
        }

        private void AwayTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (cm.CurrentCharacter == null 
                || cm.CurrentCharacter.Status == StatusType.Away
                || !ApplicationSettings.AllowAutoAway) return;

            Log("Setting user status to away");
            cm.CurrentCharacter.Status = StatusType.Away;
            events.SendUserCommand("away", new[] {cm.CurrentCharacter.StatusMessage});

            awayTimer.Stop();
        }

        private void IdleTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (cm.CurrentCharacter == null 
                || cm.CurrentCharacter.Status != StatusType.Online 
                || !ApplicationSettings.AllowAutoIdle) return;

            Log("Setting user status to idle");
            cm.CurrentCharacter.Status = StatusType.Idle;
            events.SendUserCommand("idle", new[] {cm.CurrentCharacter.StatusMessage});

            idleTimer.Stop();
        }

        [Conditional("DEBUG")]
        private void Log(string text)
        {
            Logging.LogLine(text, "auto serv");
        }
        #endregion
    }
}