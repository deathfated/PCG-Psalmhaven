using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CircleFade : MonoBehaviour
{
    [SerializeField] private Canvas canvasParent;
    RectTransform rectTransform;
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        SceneManager.sceneLoaded += (_, _) =>
        {
            rectTransform.localScale = Vector3.zero;
            canvasParent.gameObject.SetActive(false);
        };
    }
    public void StartFade(Vector3 startingScale, Vector3 endScale, float duration = 0.5f)
    {
        canvasParent.gameObject.SetActive(true);
        rectTransform.localScale = startingScale;
        rectTransform.DOScale(endScale, duration).SetUpdate(true);
    }
}