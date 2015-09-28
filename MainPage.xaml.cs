/* ----------------------------------------------- 
 * Copyright © 2013 Esri Inc. All Rights Reserved. 
 * ----------------------------------------------- */

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// 10.151.171.30 (L)
// 10.151.171.18 (R)

namespace ESRI.PrototypeLab.ProximityMap {
    public sealed partial class MainPage : Page, IDisposable {
        private const double EXTENT_REFRESH_RATE = 50d;
        private const double INK_REFRESH_RATE = 20d;
        private readonly ObservableCollection<MapPeer> _peers = new ObservableCollection<MapPeer>();
        private bool _ispressed = false;
        private DateTime _lastSend = DateTime.MinValue;
        private ManipulationModes _modes;
        private MapInkManager _inkManager = null;
        //
        // CONSTRUCTOR
        //
        public MainPage() {
            this.InitializeComponent();

            // Assign data context
            this.DataContext = ProximityMapEnvironment.Default;

            // Toggles - checked
            this.ToggleButtonConnect.Checked += this.ToggleButton_Checked;
            this.ToggleButtonSketch.Checked += this.ToggleButton_Checked;
            this.ToggleButtonNatGeo.Checked += this.ToggleButton_Checked;
            this.ToggleButtonTopo.Checked += this.ToggleButton_Checked;
            this.ToggleButtonSatellite.Checked += this.ToggleButton_Checked;
            this.ToggleButtonStreet.Checked += this.ToggleButton_Checked;

            // Toggles - unchecked
            this.ToggleButtonConnect.Unchecked += this.ToggleButton_Unchecked;
            this.ToggleButtonSketch.Unchecked += this.ToggleButton_Unchecked;

            // Button - click
            this.ButtonClearSketch.Click += this.Button_Click;
            this.ButtonScreenPosition.Click += this.Button_Click;

            // Map events
            this.Map.ExtentChanged += async (s, e) => {
                await this.SendExtent();
            };
            this.Map.PointerPressed += (s, e) => {
                this._ispressed = true;
            };
            this.Map.PointerReleased += (s, e) => {
                this._ispressed = false;
            };

            //
            this._inkManager = new MapInkManager(this.Map, this.InkCanvas);
            this._inkManager.LineChanged += async (s, e) => {
                if (DateTime.Now.Subtract(this._lastSend) < TimeSpan.FromMilliseconds(INK_REFRESH_RATE)) { return; }
                this._lastSend = DateTime.Now;
                await this.SendInk(e.MapInkLine);
            };
            this._inkManager.LineClosed += async (s, e) => {
                await this.SendInk(e.MapInkLine);
            };
            this.SketchControl.DataContext = this._inkManager;
        }
        //private async void Map_Tapped(object sender, TappedRoutedEventArgs e) {
        //    // Create graphic for tap location
        //    Graphic graphic = new Graphic() {
        //        Symbol = App.Current.Resources["TappedMarkerSymbol"] as Symbol,
        //        Geometry = this.Map.ScreenToMap(e.GetPosition(this.Map))
        //    };

        //    // Add to local map
        //    GraphicsLayer layer = this.Map.Layers["markers"] as GraphicsLayer;
        //    layer.Graphics.Add(graphic);

        //    // Send to remote peer
        //    if (this._peers.Count == 0) { return; }
        //    foreach (MapPeer peer in this._peers) {
        //        // Send extent message
        //        await this.PeerSend(peer, MessageType.Ink, graphic.ToJson());
        //    }
        //}
        //
        // METHODS
        //
        private async void ToggleButton_Checked(object sender, RoutedEventArgs e) {
            if (sender == this.ToggleButtonConnect) {
                // Open toast
                this.PopupNotifications.IsOpen = true;

                // Check if wifi-direct is support
                bool supported = (PeerFinder.SupportedDiscoveryTypes & PeerDiscoveryTypes.Browse) == PeerDiscoveryTypes.Browse;
                if (!supported) {
                    await ProximityMapEnvironment.Default.Log("This device does not supported Wifi Direct", false);
                    await Task.Delay(2000);
                    this.PopupNotifications.IsOpen = false;
                    return;
                }
                // Start listening for proximate peers
                PeerFinder.Start();

                // Be available for future connections
                PeerFinder.ConnectionRequested += this.PeerConnectionRequested;

                // Find peers
                await this.ConnectToPeers();
            }
            else if (sender == this.ToggleButtonSketch) {
                this.SketchControl.Height = Window.Current.Bounds.Height;
                this._modes = this.Map.ManipulationMode;
                this.Map.ManipulationMode = ManipulationModes.None;
                this._inkManager.IsEnabled = true;
                this.PopupSketch.IsOpen = true;
            }
            else if (sender == this.ToggleButtonNatGeo) {
                this.Map.Layers["natgeo_"].Visibility = Visibility.Visible;
                this.Map.Layers["topo___"].Visibility = Visibility.Collapsed;
                this.Map.Layers["imagery"].Visibility = Visibility.Collapsed;
                this.Map.Layers["streets"].Visibility = Visibility.Collapsed;
                this.ToggleButtonTopo.IsChecked = false;
                this.ToggleButtonSatellite.IsChecked = false;
                this.ToggleButtonStreet.IsChecked = false;
            }
            else if (sender == this.ToggleButtonTopo) {
                this.Map.Layers["natgeo_"].Visibility = Visibility.Collapsed;
                this.Map.Layers["topo___"].Visibility = Visibility.Visible;
                this.Map.Layers["imagery"].Visibility = Visibility.Collapsed;
                this.Map.Layers["streets"].Visibility = Visibility.Collapsed;
                this.ToggleButtonNatGeo.IsChecked = false;
                this.ToggleButtonSatellite.IsChecked = false;
                this.ToggleButtonStreet.IsChecked = false;
            }
            else if (sender == this.ToggleButtonSatellite) {
                this.Map.Layers["natgeo_"].Visibility = Visibility.Collapsed;
                this.Map.Layers["topo___"].Visibility = Visibility.Collapsed;
                this.Map.Layers["imagery"].Visibility = Visibility.Visible;
                this.Map.Layers["streets"].Visibility = Visibility.Collapsed;
                this.ToggleButtonNatGeo.IsChecked = false;
                this.ToggleButtonTopo.IsChecked = false;
                this.ToggleButtonStreet.IsChecked = false;
            }
            else if (sender == this.ToggleButtonStreet) {
                this.Map.Layers["natgeo_"].Visibility = Visibility.Collapsed;
                this.Map.Layers["topo___"].Visibility = Visibility.Collapsed;
                this.Map.Layers["imagery"].Visibility = Visibility.Collapsed;
                this.Map.Layers["streets"].Visibility = Visibility.Visible;
                this.ToggleButtonNatGeo.IsChecked = false;
                this.ToggleButtonTopo.IsChecked = false;
                this.ToggleButtonSatellite.IsChecked = false;
            }
        }
        private async void ToggleButton_Unchecked(object sender, RoutedEventArgs e) {
            if (sender == this.ToggleButtonConnect) {
                // Start listening for proximate peers
                PeerFinder.Stop();

                // Be available for future connections
                PeerFinder.ConnectionRequested -= this.PeerConnectionRequested;

                // Dispose of connection and reconnect
                this.Dispose();

                // Update toast
                await ProximityMapEnvironment.Default.Log("Disconnecting...", true);
                this.PopupNotifications.IsOpen = false;
            }
            else if (sender == this.ToggleButtonSketch) {
                this.Map.ManipulationMode = this._modes;
                this._inkManager.IsEnabled = false;
                this.PopupSketch.IsOpen = false;
            }
        }
        private async Task SendExtent() {
            if (!this.ToggleButtonConnect.IsChecked.Value) { return; }
            if (!this.Map.Layers[0].IsInitialized) { return; }
            if (!this._ispressed) { return; }
            if (this._peers.Count == 0) { return; }

            if (DateTime.Now.Subtract(this._lastSend) < TimeSpan.FromMilliseconds(EXTENT_REFRESH_RATE)) { return; }
            this._lastSend = DateTime.Now;

            // Get current extent
            Envelope env = this.Map.Extent.Clone();

            // Offset envelope (if necessary)
            switch (ProximityMapEnvironment.Default.VerticalAlignment) {
                case VerticalAlignment.Bottom:
                    env.YMin += this.Map.Extent.Height;
                    env.YMax += this.Map.Extent.Height;
                    break;
                case VerticalAlignment.Top:
                    env.YMin -= this.Map.Extent.Height;
                    env.YMax -= this.Map.Extent.Height;
                    break;
            }
            switch (ProximityMapEnvironment.Default.HorizontalAlignment) {
                case HorizontalAlignment.Left:
                    env.XMin += this.Map.Extent.Width;
                    env.XMax += this.Map.Extent.Width;
                    break;
                case HorizontalAlignment.Right:
                    env.XMin -= this.Map.Extent.Width;
                    env.XMax -= this.Map.Extent.Width;
                    break;
            }

            foreach (MapPeer peer in this._peers) {
                // Send extent message
                await this.PeerSend(peer, MessageType.PanningExtent, env.ToJson());
            }
        }
        private async Task SendInk(MapInkLine line) {
            if (!this.ToggleButtonConnect.IsChecked.Value) { return; }
            if (!this.Map.Layers[0].IsInitialized) { return; }
            if (this._peers.Count == 0) { return; }

            foreach (MapPeer peer in this._peers) {
                // Send extent message
                await this.PeerSend(peer, MessageType.Ink, line.ToJson());
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            try {
                if (sender == this.ButtonClearSketch) {
                    //Exception x = this.Map.Layers[0].InitializationException;
                    this._inkManager.Lines.Clear();
                    this._inkManager.Render();
                }
                else if (sender == this.ButtonScreenPosition) {
                    this.ScreenPositionControl.Height = Window.Current.Bounds.Height;
                    this.PopupScreenPosition.IsOpen = true;
                }
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private async Task ConnectToPeers() {
            // Find peers
            await ProximityMapEnvironment.Default.Log("Search for peers...", true);
            IReadOnlyList<PeerInformation> peers = await PeerFinder.FindAllPeersAsync();

            // No peers found?
            if (peers == null || peers.Count == 0) { return; }

            // Connect to each peer
            foreach (PeerInformation peer in peers) {
                // Log
                await ProximityMapEnvironment.Default.Log(string.Format("Connecting to: {0}", peer.DisplayName), true);

                // Connect to remote peer
                StreamSocket socket = null;
                try {
                    socket = await PeerFinder.ConnectAsync(peer);
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                }
                if (socket == null) {
                    await ProximityMapEnvironment.Default.Log("Connection failed", true);
                    continue;
                }

                await this.PeerConnect(peer, socket);
            }
        }
        private async void PeerConnectionRequested(object sender, ConnectionRequestedEventArgs e) {
            try {
                // Log
                await ProximityMapEnvironment.Default.Log(string.Format("Connecting to: {0}", e.PeerInformation.DisplayName), true);

                // Get socket
                StreamSocket socket = null;
                try {
                    socket = await PeerFinder.ConnectAsync(e.PeerInformation);
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                }

                if (socket == null) {
                    await Task.Delay(TimeSpan.FromSeconds(1d));
                    await ProximityMapEnvironment.Default.Log("Search for peers...", true);
                    return;
                }

                // Accept connection
                await this.PeerConnect(e.PeerInformation, socket);
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private async Task PeerConnect(PeerInformation peer, StreamSocket socket) {
            // Store socket
            DataWriter writer = new DataWriter(socket.OutputStream);
            DataReader reader = new DataReader(socket.InputStream);

            MapPeer mapPeer = new MapPeer() {
                PeerInformation = peer,
                StreamSocket = socket,
                DataReader = reader,
                DataWriter = writer
            };
            this._peers.Add(mapPeer);

            // Listening
            await ProximityMapEnvironment.Default.Log("Listening...", true);

            // Commence send/recieve loop
            await this.PeerReceive(mapPeer);
        }
        private async Task PeerReceive(MapPeer peer) {
            try {
                // Get body size
                uint bytesRead = await peer.DataReader.LoadAsync(sizeof(uint));
                if (bytesRead == 0) {
                    await this.RemovePeer(peer);
                    return;
                }
                uint length = peer.DataReader.ReadUInt32();

                // Get message type
                uint bytesRead1 = await peer.DataReader.LoadAsync(sizeof(uint));
                if (bytesRead1 == 0) {
                    await this.RemovePeer(peer);
                    return;
                }
                uint type = peer.DataReader.ReadUInt32();
                MessageType messageType = (MessageType)Enum.Parse(typeof(MessageType), type.ToString());

                // Get body
                uint bytesRead2 = await peer.DataReader.LoadAsync(length);
                if (bytesRead2 == 0) {
                    await this.RemovePeer(peer);
                    return;
                }

                // Get message
                string message = peer.DataReader.ReadString(length);

                // Process message
                switch (messageType) {
                    case MessageType.PanningExtent:
                        await this.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            () => {
                                Envelope env = (Envelope)Envelope.FromJson(message);
                                this.ProcessExtent(env);
                            }
                        );
                        break;
                    case MessageType.Ink:
                        await this.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            () => {
                                MapInkLine line = MapInkLine.FromJson(message);
                                this.ProcessInk(line);
                            }
                        );
                        break;
                }

                // Wait for next message
                await this.PeerReceive(peer);
            }
            catch (Exception ex) {
                Debug.WriteLine("Reading from socket failed: " + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
                //this.RemovePeer(peer);
            }
        }     
        private async Task PeerSend(MapPeer peer, MessageType messagetype, string text) {
            if (peer.StreamSocket == null) { return; }
            if (peer.DataWriter == null) { return; }
            if (string.IsNullOrWhiteSpace(text)) { return; }

            uint length = peer.DataWriter.MeasureString(text);
            uint type = (uint)messagetype;
            peer.DataWriter.WriteUInt32(length);
            peer.DataWriter.WriteUInt32(type);
            peer.DataWriter.WriteString(text);
            uint numBytesWritten = await peer.DataWriter.StoreAsync();

            if (numBytesWritten == 0) {
                // Socket error when sending
                await this.RemovePeer(peer);
            }
        }
        private void ClosePeer(MapPeer peer) {
            if (peer.StreamSocket != null) {
                peer.StreamSocket.Dispose();
                peer.StreamSocket = null;
            }
            if (peer.DataWriter != null) {
                peer.DataWriter.Dispose();
                peer.DataWriter = null;
            }
            if (peer.DataReader != null) {
                peer.DataReader.Dispose();
                peer.DataReader = null;
            }
        }
        private async Task RemovePeer(MapPeer peer) {
            this.ClosePeer(peer);
            if (this._peers.Contains(peer)) {
                this._peers.Remove(peer);
            }

            //
            await ProximityMapEnvironment.Default.Log("Search for peers...", true);
        }
        public void Dispose() {
            foreach (MapPeer peer in this._peers) {
                this.ClosePeer(peer);
            }
            this._peers.Clear();
        }
        private void ProcessExtent(Envelope extent) {
            if (this.Map == null || this.Map.Extent == null) { return; }
            //if (this.Map.Is) { return; }

            Envelope env = extent.Expand(96d / DisplayInformation.GetForCurrentView().LogicalDpi); //  DisplayProperties.LogicalDpi);

            // Offset envelope (if necessary)
            switch (ProximityMapEnvironment.Default.VerticalAlignment) {
                case VerticalAlignment.Bottom:
                    env.YMin -= this.Map.Extent.Height;
                    env.YMax -= this.Map.Extent.Height;
                    break;
                case VerticalAlignment.Top:
                    env.YMin += this.Map.Extent.Height;
                    env.YMax += this.Map.Extent.Height;
                    break;
            }
            switch (ProximityMapEnvironment.Default.HorizontalAlignment) {
                case HorizontalAlignment.Left:
                    env.XMin -= this.Map.Extent.Width;
                    env.XMax -= this.Map.Extent.Width;
                    break;
                case HorizontalAlignment.Right:
                    env.XMin += this.Map.Extent.Width;
                    env.XMax += this.Map.Extent.Width;
                    break;
            }

            // Zoom/pan to new extent
            this.Map.ZoomTo(env, TimeSpan.Zero);
        }
        private void ProcessInk(MapInkLine line) {
            MapInkLine l = this._inkManager.Lines.FirstOrDefault(k => k.UniqueId == line.UniqueId);
            if (l == null) {
                this._inkManager.Lines.Add(line);
            }
            else {
                this._inkManager.Lines.Remove(l);
                this._inkManager.Lines.Add(line);
                this._inkManager.Render();
            }
        }
    }
}

 