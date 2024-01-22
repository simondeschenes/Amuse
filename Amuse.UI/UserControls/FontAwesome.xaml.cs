﻿using OnnxStack.StableDiffusion.Enums;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Amuse.UI.UserControls
{
    /// <summary>
    /// Interaction logic for FontAwesome.xaml
    /// </summary>
    public partial class FontAwesome : UserControl
    {
        private readonly Storyboard _spinAnimation;
        public FontAwesome()
        {
            InitializeComponent();
            _spinAnimation = FindResource("SpinAnimation") as Storyboard;
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(int), typeof(FontAwesome), new PropertyMetadata(16));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(FontAwesome), new PropertyMetadata("\uf004"));

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Brush), typeof(FontAwesome), new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty IconStyleProperty =
            DependencyProperty.Register("IconStyle", typeof(FontAwesomeIconStyle), typeof(FontAwesome), new PropertyMetadata(FontAwesomeIconStyle.Regular));

        public static readonly DependencyProperty IsSpinnerProperty =
            DependencyProperty.Register("IsSpinner", typeof(bool), typeof(FontAwesome)
            , new PropertyMetadata((d, e) => { if (d is FontAwesome control) control.OnIsSpinnerChanged(); }));
    

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }


        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public int Size
        {
            get { return (int)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }


        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public Brush Color
        {
            get { return (Brush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }


        /// <summary>
        /// Gets or sets the icon style.
        /// </summary>
        public FontAwesomeIconStyle IconStyle
        {
            get { return (FontAwesomeIconStyle)GetValue(IconStyleProperty); }
            set { SetValue(IconStyleProperty, IconStyle); }
        }


        /// <summary>
        /// Gets or sets a value indicating whether this instance is spinning icon.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is spinner; otherwise, <c>false</c>.
        /// </value>
        public bool IsSpinner
        {
            get { return (bool)GetValue(IsSpinnerProperty); }
            set { SetValue(IsSpinnerProperty, value); }
        }


        private void OnIsSpinnerChanged()
        {
            if (_spinAnimation is null)
                return;

            if (IsSpinner)
                _spinAnimation.Begin();
            else if (!IsSpinner)
                _spinAnimation.Stop();
        }
    }

    public enum FontAwesomeIconStyle
    {
        Regular,
        Light,
        Solid,
        Brands,
        Duotone
    }
}
