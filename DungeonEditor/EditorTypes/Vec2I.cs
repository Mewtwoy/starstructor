﻿/*Starstructor, the Starbound Toolet 
Copyright (C) 2013-2014 Chris Stamford
Contact: cstamford@gmail.com

Source file contributers:
 Adam Heinermann    contact: aheinerm@gmail.com

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License along
with this program; if not, write to the Free Software Foundation, Inc.,
51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace DungeonEditor.EditorTypes
{
    [ReadOnly(true)]
    public struct Vec2I
    {
        int m_x;
        int m_y;

        public int x
        {
            get { return m_x; }
            set { m_x = value; }
        }
        public int y
        {
            get { return m_y; }
            set { m_y = value; }
        }


        public Vec2I(int x_ = 0, int y_ = 0)
        {
            m_x = x_;
            m_y = y_;
        }

        public Vec2I(List<int> source)
        {
            m_x = 0;
            if (source.Count > 0)
                m_x = source[0];

            m_y = 0;
            if (source.Count > 1)
                m_y = source[1];
        }

        public override string ToString()
        {
            return "(" + x + ", " + y + ")";
        }

        public class Serializer : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vec2I);
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                List<int> result = serializer.Deserialize<List<int>>(reader);
                if (result == null || result.Count != 2)
                    return null;

                return new Vec2I(result[0], result[1]);
            }
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                List<int> output = new List<int> { 0, 0 };
                if (value is Vec2I)
                {
                    Vec2I vec = (Vec2I)value;
                    output[0] = vec.x;
                    output[1] = vec.y;
                }
                serializer.Serialize(writer, output);
            }
        }
    }
}
