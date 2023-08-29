using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bloxel
{
    [System.Serializable]
    public class ThreadSafeAnimationCurve : ISerializationCallbackReceiver
    {
        [SerializeField, Tooltip("Resolution in hundreds"), Range(1, 16)]
        private int _precision = 10;
        [SerializeField]
        private AnimationCurve _curve;
        private float[] _precalculatedValues;

        /// <summary>
        /// Returns the Y value of the curve at X = time. Time must be between 0 - 1.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public float Evaluate(float time)
        {
            time = Mathf.Clamp01(time);
            return _precalculatedValues[Mathf.FloorToInt(time * (float)_precision * (float)100)];
        }

        /// <summary>
        /// Assign new animation curve
        /// </summary>
        /// <param name="curve"></param>
        public void SetCurve(AnimationCurve curve)
        {
            _curve = curve;
            RefreshValues();
        }

        /// <summary>
        /// Refresh internal cache
        /// </summary>
        public void RefreshValues()
        {
            _precalculatedValues = new float[_precision * 100];

            if (_curve == null)
                return;

            for (int i = 0; i < _precision * 100; i++)
                _precalculatedValues[i] = _curve.Evaluate(i / (float)(_precision * 100));
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            RefreshValues();
        }
    }
}
