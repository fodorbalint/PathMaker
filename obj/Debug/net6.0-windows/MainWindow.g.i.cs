﻿#pragma checksum "..\..\..\MainWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "138E55795FF959AE6D9D30271D51BF0E08473E7E"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace OneWayLabyrinth {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 45 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid MainGrid;
        
        #line default
        #line hidden
        
        
        #line 46 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button FocusButton;
        
        #line default
        #line hidden
        
        
        #line 48 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal SkiaSharp.Views.WPF.SKElement Canvas;
        
        #line default
        #line hidden
        
        
        #line 50 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox Size;
        
        #line default
        #line hidden
        
        
        #line 51 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox LoadCheck;
        
        #line default
        #line hidden
        
        
        #line 52 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox SaveCheck;
        
        #line default
        #line hidden
        
        
        #line 53 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox ContinueCheck;
        
        #line default
        #line hidden
        
        
        #line 54 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox KeepLeftCheck;
        
        #line default
        #line hidden
        
        
        #line 55 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button FastRunButton;
        
        #line default
        #line hidden
        
        
        #line 56 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button StartStopButton;
        
        #line default
        #line hidden
        
        
        #line 57 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button ReloadButton;
        
        #line default
        #line hidden
        
        
        #line 62 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label CurrentCoords;
        
        #line default
        #line hidden
        
        
        #line 64 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock PossibleCoords;
        
        #line default
        #line hidden
        
        
        #line 68 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label CoordinateLabel;
        
        #line default
        #line hidden
        
        
        #line 69 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label MessageLine;
        
        #line default
        #line hidden
        
        
        #line 70 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button OKButton;
        
        #line default
        #line hidden
        
        
        #line 73 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid SettingsGrid;
        
        #line default
        #line hidden
        
        
        #line 75 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid SettingsContentGrid;
        
        #line default
        #line hidden
        
        
        #line 76 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox DisplayFutureCheck;
        
        #line default
        #line hidden
        
        
        #line 77 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox DisplayAreaCheck;
        
        #line default
        #line hidden
        
        
        #line 78 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox MakeStatsCheck;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "7.0.10.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/OneWayLabyrinth;V1.0.0.0;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "7.0.10.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 7 "..\..\..\MainWindow.xaml"
            ((OneWayLabyrinth.MainWindow)(target)).PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.MWindow_PreviewKeyDown);
            
            #line default
            #line hidden
            
            #line 7 "..\..\..\MainWindow.xaml"
            ((OneWayLabyrinth.MainWindow)(target)).PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler(this.Window_PreviewMouseDown);
            
            #line default
            #line hidden
            return;
            case 2:
            this.MainGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 3:
            this.FocusButton = ((System.Windows.Controls.Button)(target));
            return;
            case 4:
            this.Canvas = ((SkiaSharp.Views.WPF.SKElement)(target));
            
            #line 48 "..\..\..\MainWindow.xaml"
            this.Canvas.PaintSurface += new System.EventHandler<SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs>(this.SKElement_PaintSurface);
            
            #line default
            #line hidden
            
            #line 48 "..\..\..\MainWindow.xaml"
            this.Canvas.MouseMove += new System.Windows.Input.MouseEventHandler(this.Canvas_MouseMove);
            
            #line default
            #line hidden
            
            #line 48 "..\..\..\MainWindow.xaml"
            this.Canvas.MouseEnter += new System.Windows.Input.MouseEventHandler(this.Canvas_MouseEnter);
            
            #line default
            #line hidden
            
            #line 48 "..\..\..\MainWindow.xaml"
            this.Canvas.MouseLeave += new System.Windows.Input.MouseEventHandler(this.Canvas_MouseLeave);
            
            #line default
            #line hidden
            return;
            case 5:
            this.Size = ((System.Windows.Controls.TextBox)(target));
            return;
            case 6:
            this.LoadCheck = ((System.Windows.Controls.CheckBox)(target));
            
            #line 51 "..\..\..\MainWindow.xaml"
            this.LoadCheck.Click += new System.Windows.RoutedEventHandler(this.SaveSettings);
            
            #line default
            #line hidden
            return;
            case 7:
            this.SaveCheck = ((System.Windows.Controls.CheckBox)(target));
            
            #line 52 "..\..\..\MainWindow.xaml"
            this.SaveCheck.Click += new System.Windows.RoutedEventHandler(this.SaveSettings);
            
            #line default
            #line hidden
            return;
            case 8:
            this.ContinueCheck = ((System.Windows.Controls.CheckBox)(target));
            
            #line 53 "..\..\..\MainWindow.xaml"
            this.ContinueCheck.Click += new System.Windows.RoutedEventHandler(this.SaveSettings);
            
            #line default
            #line hidden
            return;
            case 9:
            this.KeepLeftCheck = ((System.Windows.Controls.CheckBox)(target));
            
            #line 54 "..\..\..\MainWindow.xaml"
            this.KeepLeftCheck.Click += new System.Windows.RoutedEventHandler(this.SaveSettings);
            
            #line default
            #line hidden
            return;
            case 10:
            this.FastRunButton = ((System.Windows.Controls.Button)(target));
            
            #line 55 "..\..\..\MainWindow.xaml"
            this.FastRunButton.Click += new System.Windows.RoutedEventHandler(this.FastRun_Click);
            
            #line default
            #line hidden
            return;
            case 11:
            this.StartStopButton = ((System.Windows.Controls.Button)(target));
            
            #line 56 "..\..\..\MainWindow.xaml"
            this.StartStopButton.Click += new System.Windows.RoutedEventHandler(this.StartStop_Click);
            
            #line default
            #line hidden
            return;
            case 12:
            this.ReloadButton = ((System.Windows.Controls.Button)(target));
            
            #line 57 "..\..\..\MainWindow.xaml"
            this.ReloadButton.Click += new System.Windows.RoutedEventHandler(this.Reload_Click);
            
            #line default
            #line hidden
            return;
            case 13:
            
            #line 58 "..\..\..\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Save_Click);
            
            #line default
            #line hidden
            return;
            case 14:
            
            #line 59 "..\..\..\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Previous_Click);
            
            #line default
            #line hidden
            return;
            case 15:
            
            #line 60 "..\..\..\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Next_Click);
            
            #line default
            #line hidden
            return;
            case 16:
            this.CurrentCoords = ((System.Windows.Controls.Label)(target));
            return;
            case 17:
            this.PossibleCoords = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 18:
            this.CoordinateLabel = ((System.Windows.Controls.Label)(target));
            return;
            case 19:
            this.MessageLine = ((System.Windows.Controls.Label)(target));
            return;
            case 20:
            this.OKButton = ((System.Windows.Controls.Button)(target));
            
            #line 70 "..\..\..\MainWindow.xaml"
            this.OKButton.Click += new System.Windows.RoutedEventHandler(this.OK_Click);
            
            #line default
            #line hidden
            return;
            case 21:
            
            #line 71 "..\..\..\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Rules_Click);
            
            #line default
            #line hidden
            return;
            case 22:
            
            #line 72 "..\..\..\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.OpenSettings_Click);
            
            #line default
            #line hidden
            return;
            case 23:
            this.SettingsGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 24:
            this.SettingsContentGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 25:
            this.DisplayFutureCheck = ((System.Windows.Controls.CheckBox)(target));
            
            #line 76 "..\..\..\MainWindow.xaml"
            this.DisplayFutureCheck.Click += new System.Windows.RoutedEventHandler(this.SaveSettings);
            
            #line default
            #line hidden
            return;
            case 26:
            this.DisplayAreaCheck = ((System.Windows.Controls.CheckBox)(target));
            
            #line 77 "..\..\..\MainWindow.xaml"
            this.DisplayAreaCheck.Click += new System.Windows.RoutedEventHandler(this.SaveSettings);
            
            #line default
            #line hidden
            return;
            case 27:
            this.MakeStatsCheck = ((System.Windows.Controls.CheckBox)(target));
            
            #line 78 "..\..\..\MainWindow.xaml"
            this.MakeStatsCheck.Click += new System.Windows.RoutedEventHandler(this.SaveSettings);
            
            #line default
            #line hidden
            return;
            case 28:
            
            #line 79 "..\..\..\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.CloseSettings_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

