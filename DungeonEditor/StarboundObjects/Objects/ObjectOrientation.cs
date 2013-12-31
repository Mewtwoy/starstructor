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

using System;
using System.Collections.Generic;
using System.Drawing;
using DungeonEditor.EditorObjects;
using Newtonsoft.Json;
using System.ComponentModel;

namespace DungeonEditor.StarboundObjects.Objects
{
    [ReadOnly(true)]
    public class ObjectOrientation
    {
        [JsonIgnore] public ObjectFrames LeftFrames;
        [JsonIgnore] public Image LeftImage;
        [JsonIgnore] public ObjectFrames RightFrames;
        [JsonIgnore] public Image RightImage;

        [JsonProperty("image")]
        public string ImageName { get; set; }

        [JsonProperty("dualImage")]
        public string DualImageName { get; set; }

        // Only available if dualImage is true
        [JsonProperty("leftImage")]
        public string LeftImageName { get; set; }

        // Only available if dualImage is true
        [JsonProperty("rightImage")]
        public string RightImageName { get; set; }

        [JsonProperty("imageLayers")]
        public List<ObjectImageLayer> ImageLayers { get; set; }

        // only if imageLayers is not found
        [JsonProperty("unlit")]
        [DefaultValue(false)]
        public bool? Unlit { get; set; }

        [JsonProperty("flipImages")]
        [DefaultValue(false)]
        public bool? FlipImages { get; set; }

        // Default: 0,0
        // Vec2F, so should be double
        [JsonProperty("imagePosition")]
        public List<double> ImagePosition { get; set; }

        [JsonProperty("frames")]
        [DefaultValue(1)]
        public int? AnimFramesCount { get; set; }

        [JsonProperty("animationCycle")]
        [DefaultValue(1.0)]
        public double? AnimationCycle { get; set; }

        // List<Vec2I>
        [JsonProperty("spaces")]
        public List<List<int>> Spaces { get; set; }

        [JsonProperty("spaceScan")]
        public double? SpaceScan { get; set; }

        [JsonProperty("requireTilledAnchors")]
        [DefaultValue(false)]
        public bool? RequireTilledAnchors { get; set; }

        [JsonProperty("requireSoilAnchors")]
        [DefaultValue(false)]
        public bool? RequireSoilAnchors { get; set; }

        // Contains "left", "bottom", "right", "top", "background"
        [JsonProperty("anchors")]
        public List<string> Anchors { get; set; }

        // List<Vec2I>
        [JsonProperty("bgAnchors")]
        public List<List<int>> BackgroundAnchors { get; set; }

        // List<Vec2I>
        [JsonProperty("fgAnchors")]
        public List<List<int>> ForegroundAnchors { get; set; }

        // either "left" or "right", defaults to right if rightImage
        [JsonProperty("direction")]
        [DefaultValue("left")]
        public string Direction { get; set; }

        // either "none", "solid", or "platform"
        [JsonProperty("collision")]
        [DefaultValue("none")]
        public string Collision { get; set; }

        // Vec2F
        [JsonProperty("lightPosition")]
        public List<double> LightPosition { get; set; }

        [JsonProperty("pointAngle")]
        [DefaultValue(0.0)]
        public double PointAngle { get; set; }

        //particleEmitter       optional, object with more properties
        //particleEmitters      optional, list of particleEmitter


        public int GetWidth(int gridFactor = Editor.DEFAULT_GRID_FACTOR, ObjectDirection direction = ObjectDirection.DIRECTION_NONE)
        {
            float sizeScaleFactor = Editor.DEFAULT_GRID_FACTOR/(float) gridFactor;

            ObjectFrames frames;

            if (direction == ObjectDirection.DIRECTION_LEFT && LeftFrames != null)
            {
                frames = LeftFrames;
            }
            else
            {
                frames = RightFrames;
            }

            return (int) Math.Ceiling(frames.FrameGrid.Size[0]/sizeScaleFactor);
        }

        public int GetHeight(int gridFactor = Editor.DEFAULT_GRID_FACTOR, ObjectDirection direction = ObjectDirection.DIRECTION_NONE)
        {
            float sizeScaleFactor = Editor.DEFAULT_GRID_FACTOR/(float) gridFactor;

            ObjectFrames frames;

            if (direction == ObjectDirection.DIRECTION_LEFT && LeftFrames != null)
            {
                frames = LeftFrames;
            }
            else
            {
                frames = RightFrames;
            }

            return (int) Math.Ceiling(frames.FrameGrid.Size[1]/sizeScaleFactor);
        }

        public int GetOriginX(int gridFactor = Editor.DEFAULT_GRID_FACTOR, ObjectDirection direction = ObjectDirection.DIRECTION_NONE)
        {
            float sizeScaleFactor = Editor.DEFAULT_GRID_FACTOR/(float) gridFactor;

            int originX = 0;
            originX += (int) Math.Floor(ImagePosition[0]/sizeScaleFactor);

            return originX;
        }

        public int GetOriginY(int gridFactor = Editor.DEFAULT_GRID_FACTOR, ObjectDirection direction = ObjectDirection.DIRECTION_NONE)
        {
            float sizeScaleFactor = Editor.DEFAULT_GRID_FACTOR/(float) gridFactor;

            int originY = -GetHeight(gridFactor, direction) + gridFactor;
            originY -= (int) Math.Floor(ImagePosition[1]/sizeScaleFactor);

            return originY;
        }
    }
}