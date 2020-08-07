﻿//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2018 Sam Hocevar <sam@hocevar.net>
//              2014—2015 Benjamin Litzelmann
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Windows.Threading;

namespace WinCompose
{

public class LogEntry
{
    public DateTime DateTime { get; set; }
    public string Message { get; set; }
}

public class LogList : ObservableCollection<LogEntry>
{
    // Override the CollectionChanged event so that we can track listeners and call
    // their delegates in the correct thread.
    // FIXME: would a Log.MessageReceived event be more elegant?
    public override event NotifyCollectionChangedEventHandler CollectionChanged
    {
        add
        {
            if (Dispatcher.CurrentDispatcher.Thread.GetApartmentState() == System.Threading.ApartmentState.STA)
                PreferredDispatcher = Dispatcher.CurrentDispatcher;
            ListenerCount += value?.GetInvocationList().Length ?? 0;
            base.CollectionChanged += value;
        }

        remove
        {
            base.CollectionChanged -= value;
            ListenerCount -= value?.GetInvocationList().Length ?? 0;
        }
    }

    public Dispatcher PreferredDispatcher = Dispatcher.CurrentDispatcher;
    public int ListenerCount { get; set; }
}

public static class Log
{
    private static readonly LogList m_entries = new LogList();
    public static LogList Entries => m_entries;

#if DEBUG
    static Log()
    {
        m_entries.CollectionChanged += ConsoleDebug;
    }

    private static void ConsoleDebug(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (LogEntry entry in e.NewItems)
            {
                string msgf = $"{entry.DateTime:yyyy/MM/dd HH:mm:ss.fff} {entry.Message}";
                System.Diagnostics.Debug.WriteLine(msgf);
            }
        }
    }
#endif

    public static void Debug(string format, params object[] args)
    {
        // We don’t do anything unless we have listeners
        if (m_entries.ListenerCount > 0)
        {
            DateTime date = DateTime.Now;
            // Only use string.Format() if arguments are given, because if the
            // formatting was made in the caller it probably didn’t check for {}
            // in the resulting string.
            var msg = args.Length > 0 ? string.Format(format, args) : format;
            ThreadPool.QueueUserWorkItem(x =>
            {
                m_entries.PreferredDispatcher.Invoke(DispatcherPriority.Background, DebugSTA, date, msg);
            });
        }
    }

    private delegate void DebugDelegate(DateTime date, string msg);
    private static readonly DebugDelegate DebugSTA = (date, msg) =>
    {
        var entry = new LogEntry() { DateTime = date, Message = msg };
        while (m_entries.Count > 1024)
            m_entries.RemoveAt(0);
        m_entries.Add(entry);
    };
}

}
