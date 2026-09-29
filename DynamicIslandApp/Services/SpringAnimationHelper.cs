using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace DynamicIslandApp.Services;

public static class SpringAnimationHelper
{
    public static void AnimateDouble(
        UIElement element,
        DependencyProperty property,
        double toValue,
        double bounceFactor = 100,
        double durationMs = 280,
        Action? completed = null)
    {
        IEasingFunction easing;
        if (bounceFactor > 5)
        {
            easing = new BackEase
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = Math.Clamp((bounceFactor / 100.0) * 0.28, 0.05, 0.6)
            };
        }
        else
        {
            easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        }

        var animation = new DoubleAnimation
        {
            To = toValue,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = easing
        };

        Timeline.SetDesiredFrameRate(animation, 60);

        if (completed != null)
        {
            animation.Completed += (s, e) => completed();
        }

        element.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
    }

    public static void AnimateOpacity(
        UIElement element,
        double toValue,
        double durationMs = 180,
        Action? completed = null)
    {
        var animation = new DoubleAnimation
        {
            To = toValue,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Timeline.SetDesiredFrameRate(animation, 60);

        if (completed != null)
        {
            animation.Completed += (s, e) => completed();
        }

        element.BeginAnimation(UIElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }
}
