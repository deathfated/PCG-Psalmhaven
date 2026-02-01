using UnityEngine;
using System.Collections;

public class Script_ThunderEvent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Script_FakeLight targetLight;
    [SerializeField, Min(0)] private Vector2 intervalRange = new Vector2(2f, 8f);
    
    [Header("Flash Configuration")]
    [SerializeField, Min(0)] private float flashDuration = 0.4f;
    [SerializeField, Min(0)] private float flashIntensity = 300f;
    [SerializeField] private AnimationCurve flashCurve = new AnimationCurve(
        new Keyframe(0f, 0f), 
        new Keyframe(0.1f, 1f), 
        new Keyframe(0.3f, 0.2f), 
        new Keyframe(0.5f, 0.8f), 
        new Keyframe(1f, 0f)
    );

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip thunderSound;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;

    private float _timer;
    private float _baseIntensity;
    private AudioSource _audioSource;

    private void Start()
    {
        if (!targetLight)
        {
            Debug.LogWarning("[Script_ThunderEvent] No target light assigned.", this);
            enabled = false;
            return;
        }

        _baseIntensity = targetLight.intensity;
        _timer = Random.Range(intervalRange.x, intervalRange.y);

        if (thunderSound)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 2D Sound
        }
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            StartCoroutine(FlashRoutine());
            _timer = Random.Range(intervalRange.x, intervalRange.y);
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (_audioSource && thunderSound)
            _audioSource.PlayOneShot(thunderSound, audioVolume);

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);
            float curveValue = flashCurve.Evaluate(t);
            
            targetLight.intensity = Mathf.Lerp(_baseIntensity, flashIntensity, curveValue);
            targetLight.UpdateMaterial();
            
            yield return null;
        }

        // Reset
        targetLight.intensity = _baseIntensity;
        targetLight.UpdateMaterial();
    }
}
