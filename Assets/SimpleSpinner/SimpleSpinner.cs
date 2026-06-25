using UnityEngine;
using UnityEngine.UI;

namespace Assets.SimpleSpinner
{
    [RequireComponent(typeof(Image))]
    public class SimpleSpinner : MonoBehaviour
    {
        [Header("Rotation")]
        public bool Rotation = true;
        [Range(-10, 10), Tooltip("Value in Hz (revolutions per second).")]
        public float RotationSpeed = 1;
        public AnimationCurve RotationAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Rainbow")]
        public bool Rainbow = true;
        [Range(-10, 10), Tooltip("Value in Hz (revolutions per second).")]
        public float RainbowSpeed = 0.5f;
        [Range(0, 1)]
        public float RainbowSaturation = 1f;
        public AnimationCurve RainbowAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Options")]
        public bool RandomPeriod = true;

        [Header("Pokeball Sequence")]
        public bool PokeballSequence = true;
        public float sequenceSpeed = 0.5f;

        private Image _image;
        private float _period;

        private Color[] pokeballColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f),    // Pokeball — rojo
            new Color(0.1f, 0.1f, 0.8f),  // Superball — azul
            new Color(1f, 0.85f, 0.1f),   // Ultraball — amarillo/dorado
        };

        public void Start()
        {
            _image = GetComponent<Image>();
            _period = RandomPeriod ? Random.Range(0f, 1f) : 0;
        }

        public void Update()
        {
            if (Rotation)
            {
                transform.localEulerAngles = new Vector3(0, 0, -360 * RotationAnimationCurve.Evaluate((RotationSpeed * Time.time + _period) % 1));
            }

            if (PokeballSequence)
            {
                int index = Mathf.FloorToInt(Time.time * sequenceSpeed) % pokeballColors.Length;
                _image.color = pokeballColors[index];
            }
            else if (Rainbow)
            {
                _image.color = Color.HSVToRGB(RainbowAnimationCurve.Evaluate((RainbowSpeed * Time.time + _period) % 1), RainbowSaturation, 1);
            }
        }
    }
}