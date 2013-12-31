/*Starstructor, the Starbound Toolet
Copyright (C) 2013-2014  Chris Stamford
Contact: cstamford@gmail.com

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
using DungeonEditor.StarboundObjects;
using DungeonEditor.StarboundObjects.Tiles;
using Newtonsoft.Json;
using System.ComponentModel;

namespace DungeonEditor.EditorObjects
{
    [ReadOnly(true)]
    public class EditorFile
    {
        [JsonIgnore] protected List<EditorBrush> m_blockMap = new List<EditorBrush>();
        [JsonIgnore] protected string m_filePath;

        [JsonIgnore] protected List<EditorMapPart> m_readableParts = new List<EditorMapPart>();

        [JsonIgnore]
        public virtual string FilePath
        {
            get { return m_filePath; }
            set { m_filePath = value; }
        }

        [JsonIgnore]
        public List<EditorMapPart> ReadableParts
        {
            get { return m_readableParts; }
            set { m_readableParts = value; }
        }

        [JsonIgnore]
        public List<EditorBrush> BlockMap
        {
            get { return m_blockMap; }
            set { m_blockMap = value; }
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

            StarboundAsset asset = null;

            // Load the background tile
            if (parent.AssetMap.ContainsKey(name + extension))
            {
                asset = parent.AssetMap[name + extension];
                brush.BackAsset = asset;
            }
            else
            {
                // If this is an internal asset - liquids, etc
                // This is a hack to display liquids until liquid parsing has been implemented
                // (low priority)
                if (name == "lava")
                {
                    asset = new StarboundTile();
                    asset.AssetName = name;
                    asset.Image = EditorHelpers.GetGeneratedRectangle(8, 8, 207, 16, 32, 255);
                }
                else if (name == "acid")
                {
                    asset = new StarboundTile();
                    asset.AssetName = name;
                    asset.Image = EditorHelpers.GetGeneratedRectangle(8, 8, 107, 141, 63, 255);
                }
                else if (name == "water")
                {
                    asset = new StarboundTile();
                    asset.AssetName = name;
                    asset.Image = EditorHelpers.GetGeneratedRectangle(8, 8, 0, 78, 111, 255);
                }
                else if (name == "liquidtar" || name == "tentaclejuice")
                {
                    asset = new StarboundTile();
                    asset.AssetName = name;
                    asset.Image = EditorHelpers.GetGeneratedRectangle(8, 8, 200, 191, 231, 255);
                }
                    // Else just load the asset
                else
                {
                    asset = parent.LoadAsset(name, type);
                }

                if (asset != null)
                {
                    parent.AssetMap[name + extension] = asset;
                    brush.BackAsset = asset;
                }
            }
        }

        public virtual void LoadBrushWithFrontAsset(EditorBrush brush, Editor parent, string name, string type)
        {
            string extension = EditorHelpers.GetExtensionFromBrushType(type);

            if (!brush.NeedsFrontAsset || extension == null)
                return;

            StarboundAsset asset = null;

            // Load the foreground tile
            if (parent.AssetMap.ContainsKey(name + extension))
            {
                asset = parent.AssetMap[name + extension];
                brush.FrontAsset = asset;
            }
            else
            {
                asset = parent.LoadAsset(name, type);

                if (asset != null)
                {
                    parent.AssetMap[name + extension] = asset;
                    brush.FrontAsset = asset;
                }
            }
        }
    }
}