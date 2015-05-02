#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReportCommand.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System.Diagnostics;
    using Services;
    using Utilities;

    #endregion

    public class ReportFiledEventArgs : CharacterUpdateEventArgs
    {
        public string CallId { get; set; }

        public string Complaint { get; set; }

        public string Reported { get; set; }

        public int? LogId { get; set; }

        public string LogLink
        {
            get
            {
                var logId = LogId;
                if (logId != null)
                    return Constants.UrlConstants.ReadLog + logId.Value;

                return string.Empty;
            }
        }

        public string Tab { get; set; }

        public override string ToString()
        {
            string toReturn;
            if (Reported == null && Tab == null)
                toReturn = "has requested staff assistance";
            else if (Reported != Tab)
                toReturn = "has reported " + Reported + " in " + Tab;
            else
                toReturn = "has reported" + Reported;

            if (LogId != null)
                toReturn += $" [url={LogLink}]view log[/url]";

            return toReturn;
        }

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            DoLoudToast(toastsManager);
        }

        public override void NavigateTo(IChatState chatState)
        {
            Process.Start(LogLink);
        }
    }

    public class ReportHandledEventArgs : CharacterUpdateEventArgs
    {
        public string Handled { get; set; }

        public override string ToString()
        {
            return "has handled a report filed by " + Handled;
        }

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            DoNormalToast(toastsManager);
        }
    }
}

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Models;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void NewReportCommand(IDictionary<string, object> command)
        {
            var type = command.Get(Constants.Arguments.Action);
            if (string.IsNullOrWhiteSpace(type))
                return;

            if (type.Equals("report"))
            {
                // new report
                var report = command.Get("report");
                var callId = command.Get("callid");
                var logId = command.ContainsKey("logid") ? command["logid"] as int? : null;

                var reportIsClean = false;

                // "report" is in some sort of arbitrary and non-compulsory format
                // attempt to decipher it
                if (report == null) return;

                var rawReport = report.Split('|').Select(x => x.Trim()).ToList();

                var starters = new[] {"Current Tab/Channel:", "Reporting User:", string.Empty};

                // each section should start with one of these
                var reportData = new List<string>();

                for (var i = 0; i < rawReport.Count; i++)
                {
                    if (rawReport[i].StartsWith(starters[i]))
                        reportData.Add(rawReport[i].Substring(starters[i].Length).Trim());
                }

                if (reportData.Count == 3)
                    reportIsClean = true;

                var reporterName = command.Get(Constants.Arguments.Character);
                var reporter = CharacterManager.Find(reporterName);

                if (reportIsClean)
                {
                    Events.GetEvent<NewUpdateEvent>()
                        .Publish(
                            new CharacterUpdateModel(
                                reporter,
                                new ReportFiledEventArgs
                                {
                                    Reported = reportData[0],
                                    Tab = reportData[1],
                                    Complaint = reportData[2],
                                    LogId = logId,
                                    CallId = callId,
                                }));

                    reporter.LastReport = new ReportModel
                    {
                        Reporter = reporter,
                        Reported = reportData[0],
                        Tab = reportData[1],
                        Complaint = reportData[2],
                        CallId = callId,
                        LogId = logId
                    };
                }
                else
                {
                    Events.GetEvent<NewUpdateEvent>()
                        .Publish(
                            new CharacterUpdateModel(
                                reporter,
                                new ReportFiledEventArgs
                                {
                                    Complaint = report,
                                    CallId = callId,
                                    LogId = logId,
                                }));

                    reporter.LastReport = new ReportModel
                    {
                        Reporter = reporter,
                        Complaint = report,
                        CallId = callId,
                        LogId = logId
                    };
                }
            }
            else if (type.Equals("confirm"))
            {
                // someone else handling a report
                var handlerName = command.Get("moderator");
                var handled = command.Get(Constants.Arguments.Character);
                var handler = CharacterManager.Find(handlerName);

                Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            handler, new ReportHandledEventArgs {Handled = handled}));
            }
        }
    }

    public partial class UserCommandService
    {
        private void OnReportRequested(IDictionary<string, object> command)
        {
            if (!command.ContainsKey(Constants.Arguments.Report))
                command.Add(Constants.Arguments.Report, string.Empty);

            var logId = -1; // no log

            // report format: "Current Tab/Channel: <channel> | Reporting User: <reported user> | <report body>
            var reportText =
                $"Current Tab/Channel: {command.Get(Constants.Arguments.Channel)} | Reporting User: {command.Get(Constants.Arguments.Name)} | {command.Get(Constants.Arguments.Report)}";

            // upload on a worker thread to avoid blocking
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var channelText = command.Get(Constants.Arguments.Channel);
                if (!string.IsNullOrWhiteSpace(channelText) && !channelText.Equals("None"))
                {
                    // we could just use _model.SelectedChannel, but the user might change tabs immediately after reporting, creating a race condition
                    ChannelModel channel;
                    if (channelText == command.Get(Constants.Arguments.Name))
                        channel = model.CurrentPms.FirstByIdOrNull(channelText);
                    else
                        channel = model.CurrentChannels.FirstByIdOrNull(channelText);

                    if (channel != null)
                    {
                        var report = new ReportModel
                        {
                            Reporter = model.CurrentCharacter,
                            Reported = command.Get(Constants.Arguments.Name),
                            Complaint = command.Get(Constants.Arguments.Report),
                            Tab = channelText
                        };


                        logId = api.UploadLog(report, channel.Messages.Union(channel.Ads));
                    }
                }

                command.Remove(Constants.Arguments.Name);
                command[Constants.Arguments.Report] = reportText;
                command[Constants.Arguments.LogId] = logId;

                if (!command.ContainsKey(Constants.Arguments.Action))
                    command[Constants.Arguments.Action] = Constants.Arguments.ActionReport;

                connection.SendMessage(command);
            }).Start();
        }

        private void OnHandleLatestReportRequested(IDictionary<string, object> command)
        {
            command.Clear();
            var latest = (from n in model.Notifications
                let update = n as CharacterUpdateModel
                where update?.Arguments is ReportFiledEventArgs
                select update).FirstOrDefault();

            if (latest == null)
                return;

            var args = latest.Arguments as ReportFiledEventArgs;

            command.Add(Constants.Arguments.Type, Constants.ClientCommands.AdminAlert);
            if (args != null) command.Add(Constants.Arguments.CallId, args.CallId);
            command.Add(Constants.Arguments.Action, Constants.Arguments.ActionConfirm);

            channelService.JoinChannel(ChannelType.PrivateMessage, latest.TargetCharacter.Name);

            var logId = -1;
            if (command.ContainsKey(Constants.Arguments.LogId))
                int.TryParse(command.Get(Constants.Arguments.LogId), out logId);

            if (logId != -1)
                Process.Start(Constants.UrlConstants.ReadLog + logId);

            connection.SendMessage(command);
        }

        private void OnHandleLatestReportByUserRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey(Constants.Arguments.Name))
            {
                var target = characterManager.Find(command.Get(Constants.Arguments.Name));

                if (!target.HasReport)
                {
                    events.GetEvent<ErrorEvent>()
                        .Publish("Cannot find report for specified character!");
                    return;
                }

                command[Constants.Arguments.Type] = Constants.ClientCommands.AdminAlert;
                command.Add(Constants.Arguments.CallId, target.LastReport.CallId);
                if (!command.ContainsKey(Constants.Arguments.Action))
                    command[Constants.Arguments.Action] = Constants.Arguments.ActionConfirm;

                channelService.JoinChannel(ChannelType.PrivateMessage, target.Name);

                var logId = -1;
                if (command.ContainsKey(Constants.Arguments.LogId))
                    int.TryParse(command.Get(Constants.Arguments.LogId), out logId);

                if (logId != -1)
                    Process.Start(Constants.UrlConstants.ReadLog + logId);

                connection.SendMessage(command);
            }

            OnHandleLatestReportRequested(command);
        }
    }
}