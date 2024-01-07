using System;
using System.Diagnostics;
using HyPlayer.LyricRenderer.Abstraction.Render;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media.Animation;

namespace HyPlayer.LyricRenderer.RollingCalculators;

public class ElasticEaseRollingCalculator : LineRollingCalculator
{
    public ElasticEaseRollingCalculator()
    {
    }
    public const long AnimationDuration = 300;

    public override double CalculateCurrentY(double fromY, double targetY, int gap, long startTime, long currentTime)
    {
        double normalizedTime = 1;
        var theoryTime = AnimationDuration * (Math.Log10(Math.Max(gap, 1)) + 1);
        if (gap > 0)
        {
            if (currentTime - startTime <= theoryTime)
            {

                normalizedTime = (currentTime - startTime) / theoryTime;
                if (fromY >= targetY)
                {
                    var expo = (Math.Exp(3 * normalizedTime) - 1.0) / (Math.Exp(3) - 1.0);
                    normalizedTime = expo * (Math.Sin(Math.PI * 0.5 * normalizedTime));

                }
            }
        }
        else
        {
            normalizedTime = (currentTime - startTime) * 1.0 / AnimationDuration;
        }
        if (normalizedTime < 0) normalizedTime = 1;
        return fromY + (targetY - fromY) * Math.Clamp(normalizedTime, 0, 1);
    }
}