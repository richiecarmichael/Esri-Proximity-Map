/* ----------------------------------------------- 
 * Copyright © 2013 Esri Inc. All Rights Reserved. 
 * ----------------------------------------------- */

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace ESRI.PrototypeLab.ProximityMap {
    public class ProximityMapEnvironment : DependencyObject, INotifyPropertyChanged {
        private static ProximityMapEnvironment instance = null;
        private string _message = null;
        private bool _isSpinning = false;
        private static readonly object padlock = new object();
        private ProximityMapEnvironment() {
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Center;
        }
        public static ProximityMapEnvironment Default {
            get {
                lock (padlock) {
                    if (instance == null) {
                        instance = new ProximityMapEnvironment();
                    }
                    return instance;
                }
            }
        }
        public async Task Log(string message, bool isSpinning) {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () => {
                    this._message = message;
                    this._isSpinning = isSpinning;

                    this.NotifyPropertyChanged("Message");
                    this.NotifyPropertyChanged("IsSpinning");
                }
            );
        }
        public string Message {
            get { return this._message; }
        }
        public bool IsSpinning {
            get { return this._isSpinning; }
        }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
