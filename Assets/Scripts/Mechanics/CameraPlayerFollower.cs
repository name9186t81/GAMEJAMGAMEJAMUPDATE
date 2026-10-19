using Architecture;
using Mechanics;
using System;
using System.Collections;
using UnityEngine;

public class CameraPlayerFollower : MonoBehaviour
{
    public enum ShakeType
    {
        Constant,
        Fading
    }

    [Serializable]
    private struct ShakeCurve
    {
        public AnimationCurve Curve;
        public ShakeType Type;
    }

    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private int _maxShakes;
    [SerializeField] private float _maxStrength;
    [SerializeField] private float _maxDuration;
    [SerializeField] private float _maxRadius;
    [SerializeField] private ShakeCurve[] _curves;
    [SerializeField] private AnimationCurve _shakesCount;

    private void Start()
    {
        if (MiscEvents.Exists)
        {
            MiscEvents.Instance.OnEarthQuake += StartEarthQuake;
        }
    }

    private void StartEarthQuake()
    {
        StartShaking(15f, ShakeType.Fading);
        Debug.Log("QUAKE");
    }

    void FixedUpdate()
    {
        if (PlayerEvents.Exists && PlayerEvents.Instance.Player != null)
        {
            var player = PlayerEvents.Instance.Player;
            Vector2 pos;
            if (player.ChargingProjector || player.FocusingCamera)
            {
                pos = player.Camera.ExpectedPos;
            }
            else
            {
                pos = player.transform.position;
            }

            var lerp = Vector2.Lerp(transform.position, pos, 0.04f);

            transform.position = new Vector3(lerp.x, lerp.y, -10f);
        }
    }


    private void StartShaking(float strength, ShakeType type)
    {
        float delta = Mathf.Clamp01(strength / _maxStrength);
        float radius = delta * _maxRadius;
        float duration = delta * _maxDuration;

        int shakes = (int)(_shakesCount.Evaluate(delta) * _maxShakes);

        AnimationCurve curve = _shakesCount;

        for (int i = 0; i < _curves.Length; i++)
        {
            if (_curves[i].Type == type)
            {
                curve = _curves[i].Curve;
                break;
            }
        }

        StartCoroutine(ShakingRoutine(shakes, radius, duration, curve));
    }

    private IEnumerator ShakingRoutine(int shakes, float radius, float duration, AnimationCurve curve)
    {
        float timePerShake = duration / shakes;

        for (int i = 0; i < shakes; ++i)
        {
            Vector2 pos = _cameraTransform.position;
            float angle = (UnityEngine.Random.Range(0, Mathf.PI * 2));
            Vector2 vec = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 shakePos = pos + vec * radius * curve.Evaluate((float)i / _maxShakes);
            _cameraTransform.position = (Vector3)shakePos + Vector3.forward * _cameraTransform.position.z;
            yield return new WaitForSeconds(timePerShake);
        }
    }
}
