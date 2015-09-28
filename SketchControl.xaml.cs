/* ----------------------------------------------- 
 * Copyright © 2013 Esri Inc. All Rights Reserved. 
 * ----------------------------------------------- */

using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace ESRI.PrototypeLab.ProximityMap {
    public sealed partial class SketchControl : UserControl {
        public SketchControl() {
            this.InitializeComponent();
            this.ListView.ItemsSource = new Color[] {
                Colors.Red,
                Colors.Blue,
                Colors.Green,
                Colors.Yellow,
                Colors.Black,
                Colors.Magenta,
                Colors.White,
                Colors.Brown,
                Colors.PowderBlue,
            };
        }
    }
}
