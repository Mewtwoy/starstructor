﻿/*Starstructor, the Starbound Toolet 
Copyright (C) 2013-2014 Chris Stamford
Contact: cstamford@gmail.com

Source file contributers:
 Chris Stamford     contact: cstamford@gmail.com

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
using System.Windows.Forms;
using Starstructor.EditorObjects;
using Starstructor.StarboundTypes;
using Starstructor.StarboundTypes.Dungeons;
using Starstructor.StarboundTypes.Ships;

namespace Starstructor.GUI
{
    public partial class ImportBrush : Form
    {
        private EditorBrush m_newBrush;

        public ImportBrush(Type type)
        {
            InitializeComponent();

            if (type == typeof(StarboundDungeon)) m_newBrush = new DungeonBrush();
            else if (type == typeof(StarboundShip)) m_newBrush = new ShipBrush();

            WizardTabs.TabIndex = GetTabOffset() + 1;
        }

        private int GetTabOffset()
        {
            return m_newBrush is DungeonBrush ? 0 : 4;
        }

        private void ButtonPrev_Click(object sender, System.EventArgs e)
        {
            int offset = GetTabOffset();

            if (WizardTabs.TabIndex >= offset + 1) WizardTabs.TabIndex--;
            if (WizardTabs.TabIndex == offset + 1) ButtonPrev.Enabled = false;
            if (WizardTabs.TabIndex < offset + 4) ButtonNext.Enabled = true;
        }

        private void ButtonNext_Click(object sender, System.EventArgs e)
        {
            int offset = GetTabOffset();

            if (WizardTabs.TabIndex <= offset + 4) WizardTabs.TabIndex++;
            if (WizardTabs.TabIndex > offset + 1) ButtonPrev.Enabled = true;
            if (WizardTabs.TabIndex == offset + 4) ButtonNext.Enabled = false;
        }

        private void ButtonFinish_Click(object sender, System.EventArgs e)
        {

        }

        private void ButtonCancel_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void AssetSelectedCallback(StarboundAsset selected)
        {
            
        }
    }
}
