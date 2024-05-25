using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "ThreadSafeCurve", menuName = "ProceduralStages/ThreadSafeCurve", order = 10)]
    public class ThreadSafeCurve : ScriptableObject
    {
        public AnimationCurve curve;
        public int sampleCount = 100;

        public float[] _samples;
        public float _min;
        public float _max;
        public float _inverseRange;
        public float _step;

        private void Awake()
        {
            ResetCache();
        }

        private void OnValidate()
        {
            ResetCache();
        }

        private void ResetCache()
        {
            _samples = new float[sampleCount];

            if (curve.keys.Length == 0)
            {
                return;
            }

            _min = curve.keys[0].time;
            _max = curve.keys[curve.keys.Length - 1].time;
            float range = _max - _min;
            _inverseRange = range == 0
                ? 0
                : 1 / range;

            _step = range / (sampleCount - 1);

            for (int i = 0; i < sampleCount; i++)
            {
                float time = _step * i + _min;
                _samples[i] = curve.Evaluate(time);
            }
        }

        public float Evaluate(float time)
        {
            float clampedTime = Mathf.Clamp(time, _min, _max);

            float index = (clampedTime - _min) * _inverseRange * sampleCount;
            int floorIndex = HGMath.Clamp(Mathf.FloorToInt(index), 0, sampleCount - 1);
            int ceilIndex = HGMath.Clamp(Mathf.CeilToInt(index), 0, sampleCount - 1);

            return Mathf.LerpUnclamped(_samples[floorIndex], _samples[ceilIndex], index - floorIndex);
        }

        public float Derivative(float time)
        {
            float clampedTime = Mathf.Clamp(time, _min, _max);
            float index = (clampedTime - _min) * _inverseRange * sampleCount;
            int floorIndex = HGMath.Clamp(Mathf.FloorToInt(index), 0, sampleCount - 1);
            int ceilIndex = HGMath.Clamp(Mathf.CeilToInt(index), 0, sampleCount - 1);

            return (_samples[ceilIndex] - _samples[floorIndex]) / _step;
        }
    }
}
