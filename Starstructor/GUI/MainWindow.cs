/*Starstructor, the Starbound Toolet 
Copyright (C) 2013-2014 Chris Stamford
Contact: cstamford@gmail.com

Source file contributers:
 Chris Stamford     contact: cstamford@gmail.com
 Adam Heinermann    contact: aheinerm@gmail.com
 Daniel Davison     contact: sircapsalot@gmail.com [github.com/ddavison]

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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Starstructor.EditorObjects;
using Microsoft.Win32;
using Starstructor.StarboundTypes;
using Starstructor.StarboundTypes.Dungeons;
using Starstructor.StarboundTypes.Ships;
using Image = System.Drawing.Image;
using TreeNode = System.Windows.Forms.TreeNode;

namespace Starstructor.GUI
{
    public partial class MainWindow : Form
    {
        private readonly Dictionary<TreeNode, EditorBrush> m_brushNodeMap
            = new Dictionary<TreeNode, EditorBrush>();

        private readonly Dictionary<TreeNode, EditorMap> m_mapNodeMap
            = new Dictionary<TreeNode, EditorMap>();

        private int m_gridFactor;
        public Editor m_parent;
        private EditorBrush m_selectedBrush;
        private EditorMap m_selectedMap;

        public EditorMap SelectedMap
        {
            get { return m_selectedMap; }
            private set
            {
                EditorMap parentNext = value;
                if (parentNext is EditorMapLayer)
                    parentNext = (parentNext as EditorMapLayer).Parent;

                EditorMap parentPrev = m_selectedMap;
                if (parentPrev is EditorMapLayer)
                    parentPrev = (parentPrev as EditorMapLayer).Parent;

                bool wantReset = parentNext != parentPrev;
                m_selectedMap = value;

                // Update the work area
                UpdateImageBox(wantReset, wantReset);

                // Update menus/properties
                takeScreenshotToolStripMenuItem.Enabled = (value != null);
                UpdatePropertiesPanel();

                TreeNode desiredNode = null;

                if (value != null)
                {
                    try
                    {
                        desiredNode = m_mapNodeMap.First(x => x.Value == value).Key;
                    }
                    catch (Exception ex)
                    {
                        Editor.Log.Write(ex.ToString());
                        MessageBox.Show("Something bad happened. Consult log file for more information. Report this on the forums.");
                    }
                }

                PartTreeView.SelectedNode = desiredNode;
            }
        }

        public MainWindow(Editor parent)
        {
            m_parent = parent;

            InitializeComponent();

            // Callbacks added here since the designer enjoys making life miserable
            MainPictureBox.MouseEnter += MainPictureBox_MouseEnter;  
            BottomBarGfxCombo.SelectedIndexChanged += BottomBarGfxCombo_SelectedIndexChanged;
            RightPanelTabControl.Selected += RightPanelTabControl_Selected;
            PartTreeView.AfterSelect += PartTreeView_AfterSelect;
            BrushesTreeView.AfterSelect += BrushesTreeView_AfterSelect;

            // Set selected node when right clicking for context menu
            PartTreeView.NodeMouseClick += (sender, args) => PartTreeView.SelectedNode = args.Node;
            BrushesTreeView.NodeMouseClick += (sender, args) => BrushesTreeView.SelectedNode = args.Node;

            PartTreeView.BeforeLabelEdit += PartTreeView_BeforeLabelEdit;
            BrushesTreeView.BeforeLabelEdit += BrushesTreeView_BeforeLabelEdit;

            PartTreeView.AfterLabelEdit += PartTreeView_AfterLabelEdit;
            BrushesTreeView.AfterLabelEdit += BrushesTreeView_AfterLabelEdit;
        }

        public void PartTreeView_AfterLabelEdit(Object sender, NodeLabelEditEventArgs args)
        {
            if (!String.IsNullOrWhiteSpace(args.Label))
            {
                if (m_mapNodeMap.ContainsKey(args.Node))
                {
                    UpdatePartName(m_mapNodeMap[args.Node].Name, args.Label);
                    m_mapNodeMap[args.Node].Name = args.Label;
                }

                UpdatePropertiesPanel();
            }
            else
            {
                args.CancelEdit = true;
            }
        }

        public void UpdatePartName(string oldName, string newName)
        {
            StarboundDungeon dungeon = m_parent.ActiveFile as StarboundDungeon;

            if (dungeon == null) return;

            for (int i = 0; i < dungeon.Metadata.Anchor.Count; ++i)
            {
                if (dungeon.Metadata.Anchor[i] == oldName) dungeon.Metadata.Anchor[i] = newName;
            }
        }

        public void BrushesTreeView_AfterLabelEdit(Object sender, NodeLabelEditEventArgs args)
        {
            if (!String.IsNullOrWhiteSpace(args.Label))
            {
                if (m_brushNodeMap.ContainsKey(args.Node))
                    m_brushNodeMap[args.Node].Comment = args.Label;

                UpdatePropertiesPanel();
            }
            else
            {
                args.CancelEdit = true;
            }
        }

        public void PartTreeView_BeforeLabelEdit(Object sender, NodeLabelEditEventArgs args)
        {
            args.CancelEdit = true;

            if (m_mapNodeMap.ContainsKey(PartTreeView.SelectedNode))
            {
                EditorMap map = m_mapNodeMap[PartTreeView.SelectedNode];

                if (map is DungeonPart) args.CancelEdit = false;
            }
        }

        public void BrushesTreeView_BeforeLabelEdit(Object sender, NodeLabelEditEventArgs args)
        {
            // This should be ok
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            WindowState = Editor.Settings.WindowState;
            Left = Editor.Settings.WindowX;
            Top = Editor.Settings.WindowY;
            Width = Editor.Settings.WindowWidth;
            Height = Editor.Settings.WindowHeight;
            MainPictureBox.m_parent = this;

            viewCollisionsToolStripMenuItem.Checked = Editor.Settings.ViewCollisionGrid;
            BottomBarGfxCombo.SelectedIndex = Editor.Settings.GraphicalDisplay ? 0 : 1;
            Text = m_parent.Name + " v" + m_parent.Version;
            OpenFileDlg.InitialDirectory = Editor.Settings.AssetDirPath;
            UpdateRecentHistoryList();

            // Find the asset path
            if (Editor.Settings.AssetDirPath == null)
            {
                // Try to auto-find directory
                string path = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 211820",
                    "InstallLocation", null);

                // If found
                if (path != null)
                {
                    path = Path.Combine(path, "assets");
                    Editor.Settings.AssetDirPath = path;
                    m_parent.SaveSettings();
                }
                // Otherwise prompt the user
                else
                {
                    MessageBox.Show(
                        "Could not find Starbound folder. Please navigate to Starbound's assets directory on the next screen.");

                    DirPopup guiPopup = new DirPopup();
                    guiPopup.ShowDialog();
                }
            }

            // Start loading assets in background thread
            EditorAssets.RefreshAssets();
        }

        private void UpdateRecentHistoryList()
        {
            recentFilesToolStripMenuItem.DropDownItems.Clear();
            recentFilesToolStripMenuItem.Enabled = Editor.Settings.RecentFiles.Count > 0;

            foreach (var newItem in Editor.Settings.RecentFiles.Select(file => recentFilesToolStripMenuItem.DropDownItems.Add(file)))
            {
                newItem.Click += recentFileHistory_Click;
            }
        }
        private void recentFileHistory_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item == null)
                return;

            OpenFile(item.Text);
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            MainPictureBox.Focus();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmExit())
            {
                e.Cancel = true;
                return;
            }

            Editor.Settings.WindowState = WindowState;
            Editor.Settings.WindowX = Left;
            Editor.Settings.WindowY = Top;
            Editor.Settings.WindowWidth = Width;
            Editor.Settings.WindowHeight = Height;
            Editor.Settings.GraphicalDisplay = BottomBarGfxCombo.SelectedIndex == 0;

            m_parent.SaveSettings();
        }

        private void CleanUp()
        {
            MainPictureBox.SetImage(null, m_gridFactor);
            MainPictureBox.SetSelectedBrush(null);
            VisualGraphicBrushImageBox.Image = null;
            VisualRgbaBrushImageBox.Image = null;
            PartTreeView.Nodes.Clear();
            BrushesTreeView.Nodes.Clear();
            BottomBarBrushLabel.Text = "";
            BottomBarPositionLabel.Text = "Grid: ";
            BottomBarZoomLabel.Text = "Zoom: ";
            m_brushNodeMap.Clear();
            m_mapNodeMap.Clear();
            m_parent.CleanUpResource();
            m_selectedBrush = null;
            SelectedMap = null;
            RightPanelProperties.SelectedObject = null;

            // Disable the wizard buttons
            ButtonImportBrush.Enabled = false;
            ButtonImportPart.Enabled = false;

            // Disable the context menus
            newPartToolStripMenuItem.Enabled = false;
            renamePartToolStripMenuItem.Enabled = false;
            resizePartToolStripMenuItem.Enabled = false;
            deletePartToolStripMenuItem.Enabled = false;

            newBrushToolStripMenuItem.Enabled = false;
            renameBrushToolStripMenuItem.Enabled = false;
            deleteBrushToolStripMenuItem.Enabled = false;

            // Update menu items, regardless of how we ended up here
            UpdateUndoRedoItems();
            closeToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;

            // Force the garbage collector to clean up
            // But it won't do it until next file load because that would be too easy
            GC.Collect();
        }

        private static DialogResult PromptClosingProject()
        {
            return MessageBox.Show(
                "Are you sure you wish to close your current opened dungeon?",
                "Exit", MessageBoxButtons.YesNo);
        }

        private static DialogResult PromptSaveWork()
        {
            return MessageBox.Show(
                "Are you sure you would like to save all modified assets in this project?",
                "Save", MessageBoxButtons.YesNo);
        }

        private static DialogResult PromptSaveWorkWhenQuitting()
        {
            return MessageBox.Show(
                "Your project has unsaved work. Are you sure you want to exit without saving? (you can save from the file menu.)",
                "Unsaved work", MessageBoxButtons.YesNo);
        }

        private static DialogResult PromptGenericYesNo(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo); 
        }

        public void UpdateBottomBar(float zoom)
        {
            BottomBarZoomLabel.Text = "Zoom: " + Math.Round(zoom, 1) + "x";
            BottomBarZoomLabel.Refresh();
        }

        public void UpdateBottomBar(int gridX, int gridY)
        {
            BottomBarPositionLabel.Text = "Grid: ";
            
            if (gridX == -1 || gridY == -1)
            {
                BottomBarPositionLabel.Text += "N/A";
            }
            else
            {
                BottomBarPositionLabel.Text += "(" + gridX + ", " + gridY + ")";
            }

            BottomBarPositionLabel.Refresh();
        }


        public void SetSelectedBrush(EditorBrush brush)
        {
            m_selectedBrush = brush;
            string colour = m_selectedBrush.Colour.ToString();

            // Tidy this display up at some point
            BottomBarBrushLabel.Text = m_selectedBrush.Comment;

            if (m_selectedBrush.FrontAsset != null)
            {
                BottomBarBrushLabel.Text += " front: " + m_selectedBrush.FrontAsset.ToString();
            }

            if (m_selectedBrush.BackAsset != null)
            {
                BottomBarBrushLabel.Text += " back: " + m_selectedBrush.BackAsset.ToString();
            }

            BottomBarBrushLabel.Text += " " + colour;

            // Populate the colour box
            VisualRgbaBrushImageBox.Image = EditorHelpers.GetGeneratedRectangle(1, 1,
                m_selectedBrush.Colour.R, m_selectedBrush.Colour.G,
                m_selectedBrush.Colour.B, m_selectedBrush.Colour.A);

            VisualGraphicBrushImageBox.Image = m_selectedBrush.GetAssetPreview();

            MainPictureBox.SetSelectedBrush(m_selectedBrush);
            UpdatePropertiesPanel();
        }

        public EditorMapLayer GetSelectedLayer()
        {
            return SelectedMap == null ? null : SelectedMap.GetActiveLayer();
        }

        public EditorMapPart GetSelectedPart()
        {
            return SelectedMap == null ? null : SelectedMap.GetActivePart();
        }

        public void OnCanvasLeftClick(int gridX, int gridY, int lastGridX, int lastGridY)
        {
            if (m_selectedBrush == null || SelectedMap == null)
                return;

            // If there's nothing to change, just leave
            if (gridX == lastGridX && gridY == lastGridY)
                return;

            // Get the current layer
            EditorMapLayer activeLayer = GetSelectedLayer();

            // Change the layer brush
            EditorBrush oldBrush = activeLayer.GetBrushAt(gridX, gridY);
            activeLayer.SetUserBrushAt(m_selectedBrush, gridX, gridY);
            UpdateUndoRedoItems();

            SelectedMap.RedrawCanvasFromBrush(oldBrush, m_selectedBrush, gridX, gridY);
            MainPictureBox.Refresh();
        }

        // Updates the undo/redo items by changing their enabled state correctly
        private void UpdateUndoRedoItems()
        {
            EditorMapLayer activeLayer = GetSelectedLayer();
            if (activeLayer == null)
            {
                undoToolStripMenuItem.Enabled = false;
                redoToolStripMenuItem.Enabled = false;
            }
            else
            {
                UndoManager undoManager = activeLayer.UndoManager;

                undoToolStripMenuItem.Enabled = undoManager.CanUndo();
                redoToolStripMenuItem.Enabled = undoManager.CanRedo();
            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditorMapLayer activeLayer = GetSelectedLayer();

            if (activeLayer != null)
            {
                BrushChangeInfo? lastChange = activeLayer.UndoManager.Undo();

                if (lastChange != null)
                {
                    SelectedMap.RedrawCanvasFromBrush(lastChange.Value.m_brushAfter,
                                                        lastChange.Value.m_brushBefore,
                                                        lastChange.Value.m_x,
                                                        lastChange.Value.m_y);
                    MainPictureBox.Refresh();
                }
            }

            UpdateUndoRedoItems();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditorMapLayer activeLayer = GetSelectedLayer();

            if (activeLayer != null)
            {
                BrushChangeInfo? lastChange = activeLayer.UndoManager.Redo();

                if (lastChange != null)
                {
                    SelectedMap.RedrawCanvasFromBrush(lastChange.Value.m_brushAfter,
                                                        lastChange.Value.m_brushBefore,
                                                        lastChange.Value.m_x,
                                                        lastChange.Value.m_y);
                    MainPictureBox.Refresh();
                }
            }

            UpdateUndoRedoItems();
        }

        public void OnCanvasRightClick(int gridX, int gridY, int lastGridX, int lastGridY)
        {
            EditorMapLayer activeLayer = GetSelectedLayer();

            if (activeLayer == null)
                return;

            EditorBrush brush = activeLayer.GetBrushAt(gridX, gridY);

            if (brush == null) 
                return;

            SetSelectedBrush(brush);

            // Select the brush in the treeview
            TreeNode brushNode = (from m in m_brushNodeMap where m.Value == brush select m.Key).FirstOrDefault();

            BrushesTreeView.SelectedNode = brushNode;
        }

        // Populate the part list
        private void PopulatePartTreeView()
        {
            PartTreeView.Nodes.Clear();

            List<TreeNode> anchorNodes = new List<TreeNode>();
            List<TreeNode> extensionNodes = new List<TreeNode>();
            List<TreeNode> baseNodes = new List<TreeNode>();

            foreach (EditorMapPart part in m_parent.ActiveFile.ReadableParts)
            {
                List<TreeNode> childNodes = new List<TreeNode>();

                // Add layers as children
                foreach (EditorMapLayer layer in part.Layers)
                {
                    TreeNode newNode = new TreeNode(layer.Name);
                    m_mapNodeMap[newNode] = layer;
                    childNodes.Add(newNode);
                }

                // Create the part node
                TreeNode parentNode = new TreeNode(part.Name, childNodes.ToArray());
                m_mapNodeMap[parentNode] = part;

                // Dungeon-specific, split parts into Anchors and Extensions
                if (m_parent != null && m_parent.ActiveFile is StarboundDungeon)
                {
                    StarboundDungeon dungeon = m_parent.ActiveFile as StarboundDungeon;

                    if ( dungeon.Metadata.Anchor.Contains(part.Name) )
                        anchorNodes.Add(parentNode);
                    else
                        extensionNodes.Add(parentNode);
                }
                else
                {
                    PartTreeView.Nodes.Add(parentNode);
                }
            }

            PartTreeView.Nodes.AddRange(baseNodes.ToArray());

            // If this is a dungeon, create the anchors and extensions
            if (m_parent != null && m_parent.ActiveFile is StarboundDungeon)
            {
                TreeNode anchorsNode = new TreeNode("Anchors", anchorNodes.ToArray());
                TreeNode extensionsNode = new TreeNode("Extensions", extensionNodes.ToArray());
                anchorsNode.Expand();
                extensionsNode.Expand();
                PartTreeView.Nodes.Add(anchorsNode);
                PartTreeView.Nodes.Add(extensionsNode);
            }
        }

        private void PartTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectPartNode(e.Node);

            // The conditions for allowing the user to edit the selected part, must be a base part
            bool allowEdit = false;

            if (m_mapNodeMap.ContainsKey(PartTreeView.SelectedNode))
            {
                EditorMap map = m_mapNodeMap[PartTreeView.SelectedNode];
                if (map is EditorMapPart)
                    allowEdit = true;
            }

            // The context menu for editing currently selected part
            newPartToolStripMenuItem.Enabled    = true;
            renamePartToolStripMenuItem.Enabled = allowEdit;
            resizePartToolStripMenuItem.Enabled = allowEdit;
            deletePartToolStripMenuItem.Enabled = allowEdit;
        }

        private void UpdatePropertiesPanel()
        {
            TabPage tab = RightPanelTabControl.SelectedTab;

            if ( tab == MainTab )
            {
                if (m_parent.ActiveFile is StarboundDungeon)
                {
                    RightPanelProperties.SelectedObject = ((StarboundDungeon) m_parent.ActiveFile).Metadata;
                }
                else
                {
                    RightPanelProperties.SelectedObject = m_parent.ActiveFile;
                }
            }
            else if ( tab == PartsTab )
            {
                RightPanelProperties.SelectedObject = GetSelectedPart();
            }
            else if ( tab == BrushesTab )
            {
                RightPanelProperties.SelectedObject = m_selectedBrush;
            }
            else
            {
                RightPanelProperties.SelectedObject = null;
            }
        }

        private void RightPanelTabControl_Selected(object sender, TabControlEventArgs e)
        {
            UpdatePropertiesPanel();
        }

        // Populate the brush list
        private void PopulateBrushList()
        {
            BrushesTreeView.Nodes.Clear();

            List<TreeNode> baseNodes = new List<TreeNode>();
            BrushesTreeView.ImageList = new ImageList();
            BrushesTreeView.ImageList.Images.Add("default", EditorHelpers.GetGeneratedRectangle(8,8,255,255,255,255));

            foreach (EditorBrush brush in m_parent.ActiveFile.BlockMap)
            {
                string comment = GetBrushComment(brush);
                TreeNode parentNode = new TreeNode(comment);

                if (brush.GetAssetPreview() != null)
                {
                    BrushesTreeView.ImageList.Images.Add(brush.GetKey(), brush.GetAssetPreview());
                    parentNode.ImageKey = brush.GetKey();
                    parentNode.SelectedImageKey = brush.GetKey();
                }

                baseNodes.Add(parentNode);
                                
                // Add this node to the brush -> node map
                m_brushNodeMap[parentNode] = brush;
            }

            BrushesTreeView.Nodes.AddRange(baseNodes.ToArray());
        }

        private static string GetBrushComment(EditorBrush brush)
        {
            string comment = brush.Comment;

            if (String.IsNullOrWhiteSpace(comment))
                comment = "no comment defined";

            return comment;
        }

        private void BrushesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            bool isValidNode = m_brushNodeMap.ContainsKey(e.Node);

            newBrushToolStripMenuItem.Enabled    = true;
            renameBrushToolStripMenuItem.Enabled = isValidNode;
            deleteBrushToolStripMenuItem.Enabled = isValidNode;

            // If the node is in the map
            if (isValidNode )
            {
                SetSelectedBrush(m_brushNodeMap[e.Node]);
            }
        }

        private void UpdateImageBox(bool resetZoom, bool resetCamera)
        {
            // If no file is loaded, leave
            if (m_parent.ActiveFile == null || SelectedMap == null)
                return;

            EditorMapPart part = GetSelectedPart();

            // If we're displaying the graphic map
            if (BottomBarGfxCombo.SelectedIndex == 0)
            {
                m_gridFactor = 8;

                if (SelectedMap is EditorMapPart)
                {
                    foreach (EditorMapLayer layer in part.Layers)
                    {
                        layer.Selected = true;
                    }

                }
                else if (SelectedMap is EditorMapLayer)
                {
                    foreach (EditorMapLayer layer in part.Layers)
                    {
                        layer.Selected = false;
                    }

                    EditorMapLayer selected = (EditorMapLayer) SelectedMap;
                    selected.Selected = true;
                }


                part.UpdateLayerImage();
                MainPictureBox.SetImage(part.GraphicsMap, resetZoom, resetCamera, m_gridFactor);
            }

            // If we're displaying the colour map
            else if (BottomBarGfxCombo.SelectedIndex == 1)
            {
                m_gridFactor = 1;

                if (SelectedMap is EditorMapPart)
                {
                    MainPictureBox.SetImage(part.ColourMap, resetZoom, resetCamera, m_gridFactor);
                }
                else if (SelectedMap is EditorMapLayer)
                {
                    MainPictureBox.SetImage(GetSelectedLayer().ColourMap, resetZoom, resetCamera, m_gridFactor);
                }
            }

            MainPictureBox.Invalidate();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDlg.FileName = Editor.Settings.AssetDirPath;
            OpenFileDlg.ShowDialog();
        }

        // Open an existing file
        private void OpenFile(string fileName)
        {
            if (!ConfirmExit())
                return;

            CleanUp();

            if (!Directory.Exists(Editor.Settings.AssetDirPath))
            {
                MessageBox.Show("Invalid asset directory set. Please choose a valid asset directory " +
                                "from the Starbound menu in the toolset.", "Error", MessageBoxButtons.OK);
                return;
            }

            if (!m_parent.LoadFile(fileName))
            {
                MessageBox.Show("Unable to load file! Consult the log for more information!");
                UpdateRecentHistoryList();
                return;
            }

            PopulatePartTreeView();
            PopulateBrushList();

            if (PartTreeView.Nodes.Count > 0)
            {
                if (m_parent != null && m_parent.ActiveFile is StarboundDungeon)
                {
                    var dungeon = m_parent.ActiveFile as StarboundDungeon;
                    string desired = dungeon.Metadata.Anchor.First();
                    SelectPartNode(desired);
                }
                else
                {
                    TreeNode node = m_mapNodeMap.First().Key;
                    SelectPartNode(node);
                    //PartTreeView.SelectedNode = node;
                }
            }

            Text = m_parent.Name + " v" + m_parent.Version + " - " + m_parent.ActiveFile.FilePath;

            closeToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            ButtonImportBrush.Enabled = true;
            ButtonImportPart.Enabled = true;
            MainPictureBox.Focus();

            UpdatePropertiesPanel();
            UpdateRecentHistoryList();
        }

        // When the open file dialog presses OK
        private void OpenDungeonOrImageMap_FileOk(object sender, CancelEventArgs e)
        {
            OpenFile(OpenFileDlg.FileName);
        }

        private void SelectPartNode(string name)
        {
            SelectedMap = m_parent.ActiveFile.FindPart(name);
        }

        private void SelectPartNode(TreeNode node)
        {
            if ( !m_mapNodeMap.ContainsKey(node) )
                return;

            SelectedMap = m_mapNodeMap[node];
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ConfirmExit())
            {
                CleanUp();
            }
        }

        private bool ConfirmExit()
        {
            if (m_parent.ActiveFile == null) 
                return true;

            if (CheckForUnsavedWork())
            {
                // If they change their mind at the prompt, leave
                if (PromptSaveWorkWhenQuitting() == DialogResult.No)
                {
                    return false;
                }
            }
                // If they change their mind at the prompt, leave
            else if (PromptClosingProject() == DialogResult.No)
            {
                return false;
            }

            return true;
        }

        private void setDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DirPopup guiPopup = new DirPopup();
            guiPopup.ShowDialog();      // What is going on here???
            OpenFileDlg.InitialDirectory = Editor.Settings.AssetDirPath;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void BottomBarGfxCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateImageBox(true, true);
        }

        private bool CheckForUnsavedWork()
        {
            return m_parent.ActiveFile.ReadableParts.Any(part => part.Layers.Any(layer => layer.Changed));
        }

        private void SaveWork(string path = null, bool overwrite = false)
        {
            if (path == null)
                path = m_parent.ActiveFile.FilePath;

            m_parent.ActiveFile.FilePath = path;
            m_parent.SaveFile(path);

            // Save each changed layer
            foreach (EditorMapPart part in m_parent.ActiveFile.ReadableParts)
            {
                foreach (EditorMapLayer layer in part.Layers)
                {
                    if (!overwrite && !layer.Changed) 
                        continue;

                    string layerPath = Path.Combine(Path.GetDirectoryName(m_parent.ActiveFile.FilePath), layer.Name);
                    Image newColourMap = layer.ColourMap;

                    try
                    {
                        newColourMap.Save(layerPath, ImageFormat.Png);
                        layer.Changed = false;
                    }
                    catch (Exception e)
                    {
                        Editor.Log.Write(e.Message);
                        MessageBox.Show("Failed to save image " + layerPath + ", please try again");
                    }
                }
            }

            // Save each overlay
            if ( m_parent.ActiveFile is StarboundShip )
            {
                foreach ( ShipOverlay overlay in ((StarboundShip)m_parent.ActiveFile).BackgroundOverlays )
                {
                    string overlayPath = Path.Combine(Path.GetDirectoryName(m_parent.ActiveFile.FilePath), overlay.ImageName);

                    try
                    {
                        overlay.Image.Save(overlayPath, ImageFormat.Png);
                    }
                    catch (Exception e)
                    {
                        Editor.Log.Write(e.Message);
                        MessageBox.Show("Failed to save image " + overlayPath + ", please try again");
                    }
                }
            }
        }

        // Time to save
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PromptSaveWork() == DialogResult.Yes)
                SaveWork();
        }

        private void MainPictureBox_MouseEnter(object sender, EventArgs e)
        {
            MainPictureBox.Focus();
        }

        private void viewCollisionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewCollisionsToolStripMenuItem.Checked = !viewCollisionsToolStripMenuItem.Checked;
            Editor.Settings.ViewCollisionGrid = viewCollisionsToolStripMenuItem.Checked;
            
            if (SelectedMap != null)
                GetSelectedPart().UpdateLayerImage();
        }
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateImageBox(false, false);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDlg.InitialDirectory   = Path.GetDirectoryName(m_parent.ActiveFile.FilePath);
            SaveFileDlg.FileName           = Path.GetFileNameWithoutExtension(m_parent.ActiveFile.FilePath);
            
            if ( m_parent.ActiveFile is StarboundDungeon )
            {
                SaveFileDlg.DefaultExt = ".dungeon";
                SaveFileDlg.Filter = "Dungeon Files|*.dungeon";
            }
            else if ( m_parent.ActiveFile is StarboundShip )
            {
                SaveFileDlg.DefaultExt = ".structure";
                SaveFileDlg.Filter = "Ship Files|*.structure";
            }
            else
            {
                SaveFileDlg.DefaultExt = "";
                SaveFileDlg.Filter = "All Files|*.*";
            }

            SaveFileDlg.ShowDialog();
        }

        private void SaveFile_FileOk(object sender, CancelEventArgs e)
        {
            SaveWork(SaveFileDlg.FileName, true);
            Text = m_parent.Name + " v" + m_parent.Version + " - " + m_parent.ActiveFile.FilePath;
        }

        private void takeScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveScreenshotDlg.ShowDialog();
        }

        private void SaveScreenshotDlg_FileOk(object sender, CancelEventArgs e)
        {
            EditorMapPart part = GetSelectedPart();

            if (part != null)
            {
                part.GraphicsMap.Save(SaveScreenshotDlg.FileName, ImageFormat.Png);
            }
        }

        private void RightPanelProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            GridItem item = e.ChangedItem;

            // Get the underlying object that has been edited by this propertygrid
            while (item.Parent != null)
            {
                item = item.Parent;
            }

            if (item.Value is EditorBrush && e.ChangedItem.Label == "Comment")
            {
                EditorBrush value = item.Value as EditorBrush;
                
                // Sanitize the new comment
                value.Comment = GetBrushComment(value);

                // Get the node associated with the selected brush
                TreeNode node = m_brushNodeMap.FirstOrDefault(map => map.Value == value).Key;

                if (node != null)
                    node.Text = (string) e.ChangedItem.Value;

                // Update all other applicable things with the new brush info
                SetSelectedBrush(value);
            }
            else if (item.Value is EditorMapPart && e.ChangedItem.Label == "Name")
            {
                DungeonPart value = item.Value as DungeonPart;

                if (value != null)
                {
                    // Sanitize the new comment
                    if (String.IsNullOrWhiteSpace(value.Name))
                        value.Name = "Untitled";

                    UpdatePartName((string) e.OldValue, value.Name);

                    // Get the node associated with the selected brush
                    TreeNode node = m_mapNodeMap.FirstOrDefault(map => map.Value == value).Key;

                    if (node != null)
                        node.Text = (string) e.ChangedItem.Value;
                }
            }
        }

        private void assetBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AssetBrowser assetBrowser = new AssetBrowser();
            assetBrowser.HideSelectButton(true);
            assetBrowser.ShowDialog();
        }

        private void renamePartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ( PartTreeView.SelectedNode != null )
                PartTreeView.SelectedNode.BeginEdit();
        }

        private void renameBrushToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (BrushesTreeView.SelectedNode != null)
                BrushesTreeView.SelectedNode.BeginEdit();
        }

        private void newBrushToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HandleNewBrush();
        }

        private bool IsUniqueColour(Color? colour)
        {
            // Return false if the colour is null, or if there already exists a brush with the same colour
            return colour != null && !m_parent.BrushMap.Values.Any(brush => brush.Colour.R == colour.Value.R &&
                                                                           brush.Colour.G == colour.Value.G &&
                                                                           brush.Colour.B == colour.Value.B &&
                                                                           brush.Colour.A == colour.Value.A);
        }

        private void BrushImportedCallback(EditorBrush brush)
        {
            m_parent.ActiveFile.BlockMap.Add(brush);

            Type fileType = m_parent.ActiveFile.GetType();

            if (fileType == typeof (StarboundDungeon))
            {
                StarboundDungeon dungeon = (StarboundDungeon) m_parent.ActiveFile;
                dungeon.Tiles.Add((DungeonBrush) brush);
            }
            else if (fileType == typeof (StarboundShip))
            {
                StarboundShip ship = (StarboundShip) m_parent.ActiveFile;
                ship.Brushes.Add((ShipBrush) brush);
            }

            // Update the brush list
            PopulateBrushList();
        }

        private void resizePartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditorMap part = m_mapNodeMap[PartTreeView.SelectedNode];

            ResizePart resize = new ResizePart(part.Width, part.Height);
            resize.ShowDialog();

            if (resize.Dimensions != null && (resize.Dimensions.Value.x != part.Width || resize.Dimensions.Value.y != part.Height))
            {
                part.Resize(resize.Dimensions.Value.x, resize.Dimensions.Value.y);
                SelectedMap = part;
            }
        }

        private void newPartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HandleNewPart();
        }

        private void deletePartToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void cloneBrushToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void deleteBrushToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ButtonImportBrush_Click(object sender, EventArgs e)
        {
            HandleNewBrush();
        }

        private void ButtonImportPart_Click(object sender, EventArgs e)
        {
            HandleNewPart();
        }

        private void HandleNewBrush()
        {
            Random rng = new Random();

            Color? defaultColour = null;

            // Kinda hacky way to do this but whatever, it works
            for (int attempts = 0; attempts <= 200; ++attempts)
            {
                Color temp = Color.FromArgb(255, rng.Next(255), rng.Next(255), rng.Next(255));

                if (IsUniqueColour(temp))
                {
                    defaultColour = temp;
                    break;
                }
            }

            ImportBrush importBrush = new ImportBrush(m_parent.ActiveFile.GetType(), BrushImportedCallback, defaultColour);
            importBrush.ShowDialog();
        }

        private void HandleNewPart(string type = null)
        {
            NewPart partDialog = new NewPart();
            partDialog.ShowDialog();

            if (partDialog.PartName == null || partDialog.Dimensions == null) return;

            DungeonPart newPart = new DungeonPart();

            newPart.Name = partDialog.PartName;
            newPart.Width = partDialog.Dimensions.Value.x;
            newPart.Height = partDialog.Dimensions.Value.y;
            newPart.GraphicsMap = new Bitmap(newPart.Width * Editor.DEFAULT_GRID_FACTOR, newPart.Width * Editor.DEFAULT_GRID_FACTOR);
            newPart.Definition = new List<object>();
            newPart.Rules = new List<List<object>>();

            Bitmap baseBitmap = new Bitmap(newPart.Width, newPart.Height);
            Graphics gfx = Graphics.FromImage(baseBitmap);
            gfx.Clear(Editor.Settings.ResizeBackgroundColour);
            gfx.Dispose();

            List<string> layerNames = new List<string>()
            {
                newPart.Name + "-backgroundtiles" + ".png",
                newPart.Name + "-foregroundtiles" + ".png",
                newPart.Name + "-objects" + ".png",
                newPart.Name + "-wires" + ".png"
            };

            newPart.Definition.Add("image");
            newPart.Definition.Add(layerNames);

            foreach (string newLayerName in layerNames)
            {
                EditorMapLayer newLayer = new EditorMapLayer(newLayerName,
                    (Bitmap)baseBitmap.Clone(), m_parent.BrushMap, newPart);

                newLayer.Changed = true;
                newPart.Layers.Add(newLayer);
            }

            StarboundDungeon dungeon = (StarboundDungeon)m_parent.ActiveFile;
            dungeon.ReadableParts.Add(newPart);
            dungeon.Parts.Add(newPart);

            PopulatePartTreeView();
        }
    }
}