/*Starstructor, the Starbound Toolet 
Copyright (C) 2013-2014 Chris Stamford
Contact: cstamford@gmail.com

Source file contributers:
 Chris Stamford     contact: cstamford@gmail.com
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

using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;
using System.ComponentModel;
using Starstructor.StarboundTypes;
using Starstructor.StarboundTypes.Renderer;

namespace Starstructor.EditorObjects
{
    public class EditorFile
    {
        [JsonIgnore] 
        protected List<EditorBrush> m_blockMap = new List<EditorBrush>();

        [JsonIgnore] 
        protected string m_filePath;

        [JsonIgnore] 
        protected List<EditorMapPart> m_readableParts = new List<EditorMapPart>();

        [JsonIgnore, Browsable(false)]
        public virtual string FilePath
        {
            get { return m_filePath; }
            set { m_filePath = value; }
        }

        [JsonIgnore, Browsable(false)]
        public List<EditorMapPart> ReadableParts
        {
            get { return m_readableParts; }
            set { m_readableParts = value; }
        }

        [JsonIgnore, Browsable(false)]
        public List<EditorBrush> BlockMap
        {
            get { return m_blockMap; }
            set { m_blockMap = value; }
        }

        public EditorMapPart FindPart(string name)
        {
            return m_readableParts.Find(x => x.Name == name);
        }

        public virtual void LoadParts(Editor parent)
        {
        }

        public virtual void GenerateBrushAndAssetMaps(Editor parent)
        {
        }

        public virtual void LoadBrushWithBackAsset(EditorBrush brush, Editor parent, string name, string type)
        {
            string extension = EditorHelpers.GetExtensionFromBrushType(type);

            // Objects and NPCs must always be a front asset
            if (!brush.NeedsBackAsset || extension == null || type == "object" || type == "npc")
                return;

            // Load the background tile
            StarboundAsset asset = parent.LoadAsset(name, type);

            if (asset != null) brush.BackAsset = asset;
        }

        public virtual void LoadBrushWithFrontAsset(EditorBrush brush, Editor parent, string name, string type)
        {
            string extension = EditorHelpers.GetExtensionFromBrushType(type);

            if (!brush.NeedsFrontAsset || extension == null)
                return;

            // Load the foreground tile
            StarboundAsset asset = parent.LoadAsset(name, type);

            if (asset == null)
            {
                Image image = null;

                // If this is an internal asset - liquids, etc
                // This is a hack to display liquids until liquid parsing has been implemented
                // (low priority)
                if (name == "lava")
                {
                    image = EditorHelpers.GetGeneratedRectangle(16,
                        24, 207, 16, 32, 255);
                }
                else if (name == "acid")
                {
                    image = EditorHelpers.GetGeneratedRectangle(16,
                        24, 107, 141, 63, 255);
                }
                else if (name == "water")
                {
                    image = EditorHelpers.GetGeneratedRectangle(16,
                        24, 0, 78, 111, 255);
                }
                else if (name == "liquidtar" || name == "tentaclejuice")
                {
                    image = EditorHelpers.GetGeneratedRectangle(16,
                        24, 200, 191, 231, 255);
                }

                if (image != null)
                {
                    StarboundMaterial newAsset = new StarboundMaterial(true);
                    newAsset.MaterialName = name;
                    newAsset.Image = image;
                    asset = newAsset;
                }
            }

            if (asset != null) brush.FrontAsset = asset;
        }
    }
}