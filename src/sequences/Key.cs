﻿//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2016 Sam Hocevar <sam@hocevar.net>
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
using System.ComponentModel;
using System.Globalization;

namespace WinCompose
{

/// <summary>
/// The KeyConverter class allows to convert a string or a string-like
/// object to a Key object and back.
/// </summary>
public class KeyConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context,
                                        Type src_type)
    {
        if (src_type != typeof(string))
            return base.CanConvertFrom(context, src_type);

        return true;
    }

    public override object ConvertFrom(ITypeDescriptorContext context,
                                       CultureInfo culture, object val)
    {
        var str_val = val as string;
        if (str_val == null)
            return base.ConvertFrom(context, culture, val);

        if (str_val.StartsWith("VK."))
        {
            try
            {
                var enum_val = Enum.Parse(typeof(VK), str_val.Substring(3));
                return new Key((VK)enum_val);
            }
            catch
            {
                // Silently catch parsing exception.
            }
        }
        return new Key(str_val);
    }

    public override object ConvertTo(ITypeDescriptorContext context,
                                     CultureInfo culture, object val,
                                     Type dst_type)
    {
        if (dst_type != typeof(string))
            return base.ConvertTo(context, culture, val, dst_type);

        return val.ToString();
    }
}

/// <summary>
/// The Key class describes anything that can be hit on the keyboard,
/// resulting in either a printable string or a virtual key code.
/// </summary>
[TypeConverter(typeof(KeyConverter))]
public class Key
{
    /// <summary>
    /// A dictionary of symbols that we use for some non-printable key labels.
    /// </summary>
    private static readonly Dictionary<VK, string> m_key_labels = new Dictionary<VK, string>
    {
        { VK.UP,    "▲" },
        { VK.DOWN,  "▼" },
        { VK.LEFT,  "◀" },
        { VK.RIGHT, "▶" },
    };

    /// <summary>
    /// A list of keys for which we have a friendly name. This is used in
    /// the GUI, where the user can choose which key acts as the compose
    /// key. It needs to be lazy-initialised, because if we create Key objects
    /// before the application language is set, the descriptions will not be
    /// properly translated.
    /// </summary>
    private static Dictionary<Key, string> m_key_names = null;

    private static Dictionary<Key, string> GetKeyNames()
    {
        return new Dictionary<Key, string>
        {
            { new Key(VK.LMENU), i18n.Text.KeyLMenu },
            { new Key(VK.RMENU), i18n.Text.KeyRMenu },
            { new Key(VK.LCONTROL), i18n.Text.KeyLControl },
            { new Key(VK.RCONTROL), i18n.Text.KeyRControl },
            { new Key(VK.LWIN), i18n.Text.KeyLWin },
            { new Key(VK.RWIN), i18n.Text.KeyRWin },
            { new Key(VK.CAPITAL), i18n.Text.KeyCapital },
            { new Key(VK.NUMLOCK), i18n.Text.KeyNumLock },
            { new Key(VK.PAUSE), i18n.Text.KeyPause },
            { new Key(VK.APPS), i18n.Text.KeyApps },
            { new Key(VK.ESCAPE), i18n.Text.KeyEscape },
            { new Key(VK.SCROLL), i18n.Text.KeyScroll },
            { new Key(VK.INSERT), i18n.Text.KeyInsert },

            { new Key(" "),    i18n.Text.KeySpace },
            { new Key("\r"),   i18n.Text.KeyReturn },
            { new Key("\x1b"), i18n.Text.KeyEscape },
        };
    }

    private readonly VK m_vk;

    private readonly string m_str;

    public Key(string str) { m_str = str; }

    public Key(VK vk) { m_vk = vk; }

    public VK VirtualKey { get { return m_vk; } }

    public bool IsPrintable()
    {
        return m_str != null;
    }

    /// <summary>
    /// A friendly name that we can put in e.g. a dropdown menu
    /// </summary>
    public string FriendlyName
    {
        get
        {
            // Lazy initialisation of m_key_names (see above)
            if (m_key_names == null)
                m_key_names = GetKeyNames();

            string ret;
            if (m_key_names.TryGetValue(this, out ret))
                return ret;
            return ToString();
        }
    }

    /// <summary>
    /// A label that we can print on keycap icons
    /// </summary>
    public string KeyLabel
    {
        get
        {
            string ret;
            if (m_key_labels.TryGetValue(m_vk, out ret))
                return ret;
            return ToString();
        }
    }

    /// <summary>
    /// Serialize key to a printable string we can parse back into
    /// a <see cref="Key"/> object
    /// </summary>
    public override string ToString()
    {
        return m_str ?? string.Format("VK.{0}", m_vk);
    }

    public override bool Equals(object o)
    {
        return o is Key && this == (o as Key);
    }

    public static bool operator ==(Key a, Key b)
    {
        bool is_a_null = ReferenceEquals(a, null);
        bool is_b_null = ReferenceEquals(b, null);
        if (is_a_null || is_b_null)
            return is_a_null == is_b_null;
        return a.m_str != null ? a.m_str == b.m_str : a.m_vk == b.m_vk;
    }

    public static bool operator !=(Key a, Key b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Hash key by returning its printable representation’s hashcode or, if
    /// unavailable, its virtual key code’s hashcode.
    /// </summary>
    public override int GetHashCode()
    {
        return m_str != null ? m_str.GetHashCode() : ((int)m_vk).GetHashCode();
    }
};

}