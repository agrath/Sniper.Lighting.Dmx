using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sniper.Lighting.DMX
{
    public static class Easing
    {
        // Adapted from source : http://www.robertpenner.com/easing/

        public static float Ease(double linearStep, float acceleration, EasingType type)
        {
            float easedStep = acceleration > 0 ? EaseIn(linearStep, type) :
                              acceleration < 0 ? EaseOut(linearStep, type) :
                              (float)linearStep;

            return MathHelper.Lerp(linearStep, easedStep, Math.Abs(acceleration));
        }

        public static float EaseIn(double linearStep, EasingType type)
        {
            switch (type)
            {
                case EasingType.Step: return linearStep < 0.5 ? 0 : 1;
                case EasingType.Linear: return (float)linearStep;
                case EasingType.Sine: return Sine.EaseIn(linearStep);
                case EasingType.Quadratic: return Power.EaseIn(linearStep, 2);
                case EasingType.Cubic: return Power.EaseIn(linearStep, 3);
                case EasingType.Quartic: return Power.EaseIn(linearStep, 4);
                case EasingType.Quintic: return Power.EaseIn(linearStep, 5);
                //case EasingType.Bounce: return 0;
                //case EasingType.Elastic: return 0;
            }
            throw new NotImplementedException();
        }

        public static float EaseOut(double linearStep, EasingType type)
        {
            switch (type)
            {
                case EasingType.Step: return linearStep < 0.5 ? 0 : 1;
                case EasingType.Linear: return (float)linearStep;
                case EasingType.Sine: return Sine.EaseOut(linearStep);
                case EasingType.Quadratic: return Power.EaseOut(linearStep, 2);
                case EasingType.Cubic: return Power.EaseOut(linearStep, 3);
                case EasingType.Quartic: return Power.EaseOut(linearStep, 4);
                case EasingType.Quintic: return Power.EaseOut(linearStep, 5);
                //case EasingType.Bounce: return 0;
                //case EasingType.Elastic: return 0;
            }
            throw new NotImplementedException();
        }

        public static float EaseInOut(double linearStep, EasingType easeInType, EasingType easeOutType)
        {
            return linearStep < 0.5 ? EaseInOut(linearStep, easeInType) : EaseInOut(linearStep, easeOutType);
        }
        public static float EaseInOut(double linearStep, EasingType type)
        {
            switch (type)
            {
                case EasingType.Step: return linearStep < 0.5 ? 0 : 1;
                case EasingType.Linear: return (float)linearStep;
                case EasingType.Sine: return Sine.EaseInOut(linearStep);
                case EasingType.Quadratic: return Power.EaseInOut(linearStep, 2);
                case EasingType.Cubic: return Power.EaseInOut(linearStep, 3);
                case EasingType.Quartic: return Power.EaseInOut(linearStep, 4);
                case EasingType.Quintic: return Power.EaseInOut(linearStep, 5);
                //case EasingType.Bounce: return Bounce.EaseInOut(linearStep, begin, delta, duration);
                //case EasingType.Elastic: return 0;
            }
            throw new NotImplementedException();
        }

        static class Sine
        {
            public static float EaseIn(double s)
            {
                return (float)Math.Sin(s * MathHelper.HalfPi - MathHelper.HalfPi) + 1;
            }
            public static float EaseOut(double s)
            {
                return (float)Math.Sin(s * MathHelper.HalfPi);
            }
            public static float EaseInOut(double s)
            {
                return (float)(Math.Sin(s * MathHelper.Pi - MathHelper.HalfPi) + 1) / 2;
            }
        }

        static class Power
        {
            public static float EaseIn(double s, int power)
            {
                return (float)Math.Pow(s, power);
            }
            public static float EaseOut(double s, int power)
            {
                var sign = power % 2 == 0 ? -1 : 1;
                return (float)(sign * (Math.Pow(s - 1, power) + sign));
            }
            public static float EaseInOut(double s, int power)
            {
                s *= 2;
                if (s < 1) return EaseIn(s, power) / 2;
                var sign = power % 2 == 0 ? -1 : 1;
                return (float)(sign / 2.0 * (Math.Pow(s - 2, power) + sign * 2));
            }
        }
        //static class Elastic
        //{
        //    //time=current step, begin=start value, change=delta (amount of change e.g. start@50, end@95 = 45), duration=number of steps
        //    public static float EaseIn(double t, double b, double c, double d, double a, double p)
        //    {
        //        if (t == 0)
        //            return (float)b;
        //        if ((t /= d) == 1)
        //            return (float)(b + c);
        //        if (p == 0)
        //            p = d * .3;
        //        float s = 0;
        //        if (a == 0 || a < Math.Abs(c))
        //        {
        //            a = c;
        //            s = (float)(p / 4);
        //        }
        //        else
        //        {
        //            s = (float)(p / (2 * Math.PI) * Math.Asin(c / a));
        //        }
        //        return (float)(-(a * Math.Pow(2, 10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p)) + b);
        //    }
        //    public static float EaseOut(double t, double b, double c, double d, double a, double p)
        //    {
        //        if (t == 0)
        //            return (float)b;
        //        if ((t /= d) == 1)
        //            return (float)(b + c);
        //        if (p == 0)
        //            p = d * .3;
        //        float s = 0;
        //        if (a == 0 || a < Math.Abs(c))
        //        {
        //            a = c;
        //            s = (float)(p / 4);
        //        }
        //        else
        //        {
        //            s = (float)(p / (2 * Math.PI) * Math.Asin(c / a));
        //        }
        //        return (float)(a * Math.Pow(2, -10 * t) * Math.Sin((t * d - s) * (2 * Math.PI) / p) + c + b);
        //    }
        //    public static float EaseInOut(double t, double b, double c, double d, double a, double p)
        //    {
        //        if (t == 0)
        //            return (float)(b);
        //        if ((t /= d / 2) == 2)
        //            return (float)(b + c);
        //        if (p == 0)
        //            p = d * (.3 * 1.5);
        //        float s = 0;
        //        if (a == 0 || a < Math.Abs(c))
        //        {
        //            a = c;
        //            s = (float)(p / 4);
        //        }
        //        else
        //        {
        //            s = (float)(p / (2 * Math.PI) * Math.Asin(c / a));
        //        }
        //        if (t < 1)
        //        {
        //            return (float)(-.5 * (a * Math.Pow(2, 10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p)) + b);
        //        }
        //        return (float)(a * Math.Pow(2, -10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p) * .5 + c + b);
        //    }
        //}
        //static class Bounce
        //{
        //    //time=current step, begin=start value, change=delta (amount of change e.g. start@50, end@95 = 45), duration=number of steps
        //    public static float EaseIn(double t, double b, double c, double d)
        //    {
        //        return (float)(c - Bounce.EaseOut(d - t, 0, c, d) + b);
        //    }
        //    public static float EaseOut(double t, double b, double c, double d)
        //    {
        //        if ((t /= d) < (1 / 2.75))
        //        {
        //            return (float)(c * (7.5625 * t * t) + b);
        //        }
        //        else if (t < (2 / 2.75))
        //        {
        //            return (float)(c * (7.5625 * (t -= (1.5 / 2.75)) * t + .75) + b);
        //        }
        //        else if (t < (2.5 / 2.75))
        //        {
        //            return (float)(c * (7.5625 * (t -= (2.25 / 2.75)) * t + .9375) + b);
        //        }
        //        else
        //        {
        //            return (float)(c * (7.5625 * (t -= (2.625 / 2.75)) * t + .984375) + b);
        //        }
        //    }
        //    public static float EaseInOut(double t, double b, double c, double d)
        //    {
        //        if (t < d / 2) return (float)(Bounce.EaseIn(t * 2, 0, c, d) * .5 + b);
        //        else return (float)(Bounce.EaseOut(t * 2 - d, 0, c, d) * .5 + c * .5 + b);
        //    }
        //}
    }

    
    

   

}
