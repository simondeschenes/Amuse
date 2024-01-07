using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Amuse.UI.Behaviors
{
    public class MediaElementBehavior : Behavior<MediaElement>
    {

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>
        /// Override this to hook up functionality to the AssociatedObject.
        /// </remarks>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MediaEnded += AssociatedObject_MediaEnded;
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
        }


        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>
        /// Override this to unhook functionality from the AssociatedObject.
        /// </remarks>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.MediaEnded -= AssociatedObject_MediaEnded;
            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MediaElement mediaElement)
                return;

            mediaElement.LoadedBehavior = MediaState.Play;
            mediaElement.ScrubbingEnabled = true;
            mediaElement.Position = TimeSpan.FromSeconds(1);
            mediaElement.LoadedBehavior = MediaState.Pause;
        }

        private void AssociatedObject_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (sender is not MediaElement mediaElement)
                return;

            mediaElement.Position = TimeSpan.FromMilliseconds(1);
        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not MediaElement mediaElement)
                return;

            mediaElement.LoadedBehavior = mediaElement.LoadedBehavior == MediaState.Pause
                 ? MediaState.Play
                 : MediaState.Pause;
        }


    }
}
