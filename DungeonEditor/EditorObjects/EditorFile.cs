﻿using DungeonEditor.StarboundObjects;
using DungeonEditor.StarboundObjects.Objects;
using DungeonEditor.StarboundObjects.Tiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonEditor.EditorObjects
{
    public class EditorFile
    {
        [JsonIgnore]
        protected string m_filePath;

        [JsonIgnore]
        protected List<EditorMapPart> m_readableParts = new List<EditorMapPart>();

        [JsonIgnore]
        protected List<EditorBrush> m_blockMap = new List<EditorBrush>();

        [JsonIgnore]
        public virtual string FilePath
        {
            get
            {
                return m_filePath;
            }
            set
            {
                m_filePath = value;
            }
        }

        [JsonIgnore]
        public List<EditorMapPart> ReadableParts
        {
            get
            {
                return m_readableParts;
            }
            set
            {
                m_readableParts = value;
            }
        }

        [JsonIgnore]
        public List<EditorBrush> BlockMap
        {
            get
            {
                return m_blockMap;
            }
            set
            {
                m_blockMap = value;
            }
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
