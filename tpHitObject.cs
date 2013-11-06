using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

using osu.GameplayElements.HitObjects;

namespace AiModtpDifficultyCalculator
{
    class tpHitObject
    {
        

        // Factor by how much speed / aim strain decays per second. Those values are results of tweaking a lot and taking into account general feedback.
        public static readonly double[] DECAY_BASE = { 0.3, 0.15 }; // Opinionated observation: Speed is easier to maintain than accurate jumps.

        private const double ALMOST_DIAMETER = 90; // Almost the normed diameter of a circle (104 osu pixel). That is -after- position transforming.

        // Pseudo threshold values to distinguish between "singles" and "streams". Of course the border can not be defined clearly, therefore the algorithm
        // has a smooth transition between those values. They also are based on tweaking and general feedback.
        private const double STREAM_SPACING_TRESHOLD = 110;
        private const double SINGLE_SPACING_TRESHOLD = 125;

        // Scaling values for weightings to keep aim and speed difficulty in balance. Found from testing a very large map pool (containing all ranked maps) and keeping the
        // average values the same.
        private static readonly double[] SPACING_WEIGHT_SCALING = { 1400, 26.25 };

        // In milliseconds. The smaller the value, the more accurate sliders are approximated. 0 leads to an infinite loop, so use something bigger.
        private const int LAZY_SLIDER_STEP_LENGTH = 1;

        public tpHitObject(HitObjectBase BaseHitObject, float CircleRadius)
        {
            this.BaseHitObject = BaseHitObject;

            // We will scale everything by this factor, so we can assume a uniform CircleSize among beatmaps.
            float ScalingFactor = (52.0f / CircleRadius);
            NormalizedStartPosition = BaseHitObject.Position * ScalingFactor;
            

            // Calculate approximation of lazy movement on the slider
            if ((BaseHitObject.Type & HitObjectType.Slider) > 0)
            {
                float SliderFollowCircleRadius = CircleRadius * 3; // Not sure if this is correct, but here we do not need 100% exact values. This comes pretty darn close in my tests.

                int SegmentLength = BaseHitObject.Length / BaseHitObject.SegmentCount;
                int SegmentEndTime = BaseHitObject.StartTime + SegmentLength;

                // For simplifying this step we use actual osu! coordinates and simply scale the length, that we obtain by the ScalingFactor later
                Vector2 CursorPos = BaseHitObject.Position; // 

                // Actual computation of the first lazy curve
                for (int Time = BaseHitObject.StartTime + LAZY_SLIDER_STEP_LENGTH; Time < SegmentEndTime; Time += LAZY_SLIDER_STEP_LENGTH)
                {
                    Vector2 Difference = BaseHitObject.PositionAtTime(Time) - CursorPos;
                    float Distance = Difference.Length();

                    // Did we move away too far?
                    if (Distance > SliderFollowCircleRadius)
                    {
                        // Yep, we need to move the cursor
                        Difference.Normalize(); // Obtain the direction of difference. We do no longer need the actual difference
                        Distance -= SliderFollowCircleRadius;
                        CursorPos += Difference * Distance; // We move the cursor just as far as needed to stay in the follow circle
                        LazySliderLengthFirst += Distance;
                    }
                }

                LazySliderLengthFirst *= ScalingFactor;
                // If we have an odd amount of repetitions the current position will be the end of the slider. Note that this will -always- be triggered if
                // BaseHitObject.SegmentCount <= 1, because BaseHitObject.SegmentCount can not be smaller than 1. Therefore NormalizedEndPosition will always be initialized
                if (BaseHitObject.SegmentCount % 2 == 1)
                {
                    NormalizedEndPosition = CursorPos * ScalingFactor;
                }

                // If we have more than one segment, then we also need to compute the length ob subsequent lazy curves. They are different from the first one, since the first
                // one starts right at the beginning of the slider.
                if(BaseHitObject.SegmentCount > 1)
                {
                    // Use the next segment
                    SegmentEndTime += SegmentLength;

                    for (int Time = SegmentEndTime - SegmentLength + LAZY_SLIDER_STEP_LENGTH; Time < SegmentEndTime; Time += LAZY_SLIDER_STEP_LENGTH)
                    {
                        Vector2 Difference = BaseHitObject.PositionAtTime(Time) - CursorPos;
                        float Distance = Difference.Length();

                        // Did we move away too far?
                        if (Distance > SliderFollowCircleRadius)
                        {
                            // Yep, we need to move the cursor
                            Difference.Normalize(); // Obtain the direction of difference. We do no longer need the actual difference
                            Distance -= SliderFollowCircleRadius;
                            CursorPos += Difference * Distance; // We move the cursor just as far as needed to stay in the follow circle
                            LazySliderLengthSubsequent += Distance;
                        }
                    }

                    LazySliderLengthSubsequent *= ScalingFactor;
                    // If we have an even amount of repetitions the current position will be the end of the slider
                    if (BaseHitObject.SegmentCount % 2 == 1)
                    {
                        NormalizedEndPosition = CursorPos * ScalingFactor;
                    }
                }
            }
            // We have a normal HitCircle or a spinner
            else
            {
                NormalizedEndPosition = BaseHitObject.EndPosition * ScalingFactor;
            }
        }

        public HitObjectBase BaseHitObject;
        public double[] Strains = {1, 1};
        private Vector2 NormalizedStartPosition;
        private Vector2 NormalizedEndPosition;
        public float LazySliderLengthFirst = 0;
        public float LazySliderLengthSubsequent = 0;
        

        
        public void CalculateStrains(tpHitObject PreviousHitObject)
        {
            CalculateSpecificStrain(PreviousHitObject, AiModtpDifficulty.DifficultyType.Speed);
            CalculateSpecificStrain(PreviousHitObject, AiModtpDifficulty.DifficultyType.Aim);
        }


        // Caution: The subjective values are strong with this one
        private static double SpacingWeight(double distance, AiModtpDifficulty.DifficultyType Type)
        {

            switch(Type)
            {
                case AiModtpDifficulty.DifficultyType.Speed:

                    {
                        double Weight;

                        if (distance > SINGLE_SPACING_TRESHOLD)
                        {
                            Weight = 2.5;
                        }
                        else if (distance > STREAM_SPACING_TRESHOLD)
                        {
                            Weight = 1.6 + 0.9 * (distance - STREAM_SPACING_TRESHOLD) / (SINGLE_SPACING_TRESHOLD - STREAM_SPACING_TRESHOLD);
                        }
                        else if (distance > ALMOST_DIAMETER)
                        {
                            Weight = 1.2 + 0.4 * (distance - ALMOST_DIAMETER) / (STREAM_SPACING_TRESHOLD - ALMOST_DIAMETER);
                        }
                        else if (distance > ALMOST_DIAMETER / 2)
                        {
                            Weight = 0.95 + 0.25 * (distance - (ALMOST_DIAMETER / 2)) / (ALMOST_DIAMETER / 2);
                        }
                        else
                        {
                            Weight = 0.95;
                        }

                        return Weight;
                    }


                case AiModtpDifficulty.DifficultyType.Aim:

                    return Math.Pow(distance, 0.99);


                    // Should never happen. 
                default:
                    return 0;
            }
        }




        private void CalculateSpecificStrain(tpHitObject PreviousHitObject, AiModtpDifficulty.DifficultyType Type)
        {
            double Addition = 0;
            double TimeElapsed = BaseHitObject.StartTime - PreviousHitObject.BaseHitObject.StartTime;
            double Decay = Math.Pow(DECAY_BASE[(int)Type], TimeElapsed / 1000);

            if ((BaseHitObject.Type & HitObjectType.Spinner) > 0)
            {
                // Do nothing for spinners
            }
            else if ((BaseHitObject.Type & HitObjectType.Slider) > 0)
            {
                switch(Type)
                {
                    case AiModtpDifficulty.DifficultyType.Speed:

                        // For speed strain we treat the whole slider as a single spacing entity, since "Speed" is about how hard it is to click buttons fast.
                        // The spacing weight exists to differentiate between being able to easily alternate or having to single.
                        Addition =
                            SpacingWeight(PreviousHitObject.LazySliderLengthFirst +
                                          PreviousHitObject.LazySliderLengthSubsequent * (PreviousHitObject.BaseHitObject.SegmentCount - 1) +
                                          DistanceTo(PreviousHitObject), Type) *
                            SPACING_WEIGHT_SCALING[(int)Type];
                        break;


                    case AiModtpDifficulty.DifficultyType.Aim:

                        // For Aim strain we treat each slider segment and the jump after the end of the slider as separate jumps, since movement-wise there is no difference
                        // to multiple jumps.
                        Addition =
                            (
                                SpacingWeight(PreviousHitObject.LazySliderLengthFirst, Type) +
                                SpacingWeight(PreviousHitObject.LazySliderLengthSubsequent, Type) * (PreviousHitObject.BaseHitObject.SegmentCount - 1) +
                                SpacingWeight(DistanceTo(PreviousHitObject), Type)
                            ) *
                            SPACING_WEIGHT_SCALING[(int)Type];
                        break;
                }
                
            }
            else if ((BaseHitObject.Type & HitObjectType.Normal) > 0)
            {
                Addition = SpacingWeight(DistanceTo(PreviousHitObject), Type) * SPACING_WEIGHT_SCALING[(int)Type];
            }

            // Scale addition by the time, that elapsed. Filter out HitObjects that are too close to be played anyway to avoid crazy values by division through close to zero.
            // You will never find maps that require this amongst ranked maps.
            Addition /= Math.Max(TimeElapsed, 50);

            Strains[(int)Type] = PreviousHitObject.Strains[(int)Type] * Decay + Addition;
        }

      

        private double DistanceTo(tpHitObject other)
        {
            // Scale the distance by circle size.
            return (NormalizedStartPosition - other.NormalizedEndPosition).Length();
        }
    }
}
