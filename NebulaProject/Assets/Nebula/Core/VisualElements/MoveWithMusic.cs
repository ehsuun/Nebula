using UnityEngine;
using Nebula;

namespace Nebula.VisualElements
{
    public class MoveWithMusic : AudioReactiveElement
    {
        [System.Serializable]
        public class AxisMovement
        {
            public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
            public float amplitude = 1f;
        }

        public AxisMovement xMovement = new AxisMovement();
        public AxisMovement yMovement = new AxisMovement();
        public AxisMovement zMovement = new AxisMovement();

        [Range(1, 8)]
        public int barsPerCycle = 1;

        [Range(1, 8)]
        public int beatsPerBar = 4;

        private Vector3 initialPosition;
        private float currentXOffset = 0f;

        protected override void Start()
        {
            base.Start();
            initialPosition = transform.localPosition;
        }

        protected override void ReactToMusic()
        {
            if (musicProcessor == null) return;

            float barPosition = musicProcessor.GetBarPosition(beatsPerBar);
            float cyclePosition = (barPosition + (musicProcessor.GetMusicTime() / (60f / musicProcessor.GetSmoothBPM() * beatsPerBar * barsPerCycle))) % 1f;

            Vector3 newPosition = initialPosition;
            newPosition.x += xMovement.curve.Evaluate(cyclePosition) * xMovement.amplitude + currentXOffset;
            newPosition.y += yMovement.curve.Evaluate(cyclePosition) * yMovement.amplitude;
            newPosition.z += zMovement.curve.Evaluate(cyclePosition) * zMovement.amplitude;

            transform.localPosition = newPosition;
        }

        // New method to move the object along the X-axis
        public void MoveX(float value)
        {
            //just move the object along the x axis by the value
            transform.localPosition = new Vector3(transform.localPosition.x + value, transform.localPosition.y, transform.localPosition.z);
        }
    }
}
