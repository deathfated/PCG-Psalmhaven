using UnityEngine;

public class Scr_TextureSwitchTimer : MonoBehaviour
{
    [System.Serializable]
    public class TextureSwitch
    {
        public Texture texture;
        public float duration;
    }

    public TextureSwitch[] textures;
    public bool applyToEmission = false;

    private Renderer _renderer;
    private int _currentTextureIndex = 0;
    private float _timer;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("Renderer component missing!");
            enabled = false;
            return;
        }

        if (textures.Length > 0)
        {
            _renderer.material.mainTexture = textures[0].texture;
            if (applyToEmission)
            {
                _renderer.material.SetTexture("_EmissionMap", textures[0].texture);
                _renderer.material.SetColor("_EmissionColor", Color.white);
                _renderer.material.EnableKeyword("_EMISSION");
            }
            _timer = textures[0].duration;
        }
        else
        {
            Debug.LogError("No textures set in TextureSwitchTimer.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (textures.Length == 0) return;

        _timer -= Time.deltaTime;

        if (_timer <= 0)
        {
            _currentTextureIndex = (_currentTextureIndex + 1) % textures.Length;
            _renderer.material.mainTexture = textures[_currentTextureIndex].texture;
            if (applyToEmission)
            {
                _renderer.material.SetTexture("_EmissionMap", textures[_currentTextureIndex].texture);
                _renderer.material.SetColor("_EmissionColor", Color.white);
                _renderer.material.EnableKeyword("_EMISSION");
                _renderer.UpdateGIMaterials();
            }
            _timer = textures[_currentTextureIndex].duration;
        }
    }
}