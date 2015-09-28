/* ----------------------------------------------- 
 * Copyright © 2013 Esri Inc. All Rights Reserved. 
 * ----------------------------------------------- */

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace ESRI.PrototypeLab.ProximityMap {
    public sealed partial class ScreenPositionControl : UserControl {
        public ScreenPositionControl() {
            this.InitializeComponent();

            // Select the current position
            int index = 0;
            switch (ProximityMapEnvironment.Default.VerticalAlignment) {
                case VerticalAlignment.Top:
                    break;
                case VerticalAlignment.Center:
                    index += 3;
                    break;
                case VerticalAlignment.Bottom:
                    index += 6;
                    break;
            }
            switch (ProximityMapEnvironment.Default.HorizontalAlignment) {
                case HorizontalAlignment.Left:
                    break;
                case HorizontalAlignment.Center:
                    index += 1;
                    break;
                case HorizontalAlignment.Right:
                    index += 2;
                    break;
            }
            this.ListView.SelectedIndex = index;

            // Update the position when an item is selected
            this.ListView.SelectionChanged += (s, e) => {
                switch (this.ListView.SelectedIndex) {
                    case 0:
                    case 1:
                    case 2:
                        ProximityMapEnvironment.Default.VerticalAlignment = VerticalAlignment.Top;
                        break;
                    case 3:
                    case 4:
                    case 5:
                        ProximityMapEnvironment.Default.VerticalAlignment = VerticalAlignment.Center;
                        break;
                    case 6:
                    case 7:
                    case 8:
                        ProximityMapEnvironment.Default.VerticalAlignment = VerticalAlignment.Bottom;
                        break;
                }
                switch (this.ListView.SelectedIndex) {
                    case 0:
                    case 3:
                    case 6:
                        ProximityMapEnvironment.Default.HorizontalAlignment = HorizontalAlignment.Left;
                        break;
                    case 1:
                    case 4:
                    case 7:
                        ProximityMapEnvironment.Default.HorizontalAlignment = HorizontalAlignment.Center;
                        break;
                    case 2:
                    case 5:
                    case 8:
                        ProximityMapEnvironment.Default.HorizontalAlignment = HorizontalAlignment.Right;
                        break;
                }
            };
        }
    }
}
