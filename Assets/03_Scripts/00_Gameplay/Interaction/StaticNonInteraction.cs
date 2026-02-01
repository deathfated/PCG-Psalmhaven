using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class StaticNonInteraction : BaseInteraction
{
    [Header("UI Component")]
    [SerializeField] private CanvasGroup _uiPanel;
    [SerializeField] private CanvasGroup _textPanel;
    [SerializeField] private TextMeshProUGUI _textUI;

    [Header("Data")]
    [SerializeField] private DialogData[] dialogData;
    private int _indexDialog;
    private DialogData _currentDialog;
    private bool _hasInteract;

    private Coroutine activeCoroutine;

    public UnityEvent OnEnterDialog;
    public UnityEvent OnInteractedDialog;

    private void Start()
    {
        _uiPanel.alpha = 0f;
        _textPanel.alpha = 0f;
    }

    public override void TriggerEnter(Collider other)
    {
        if (_hasInteract)
            return;

        OnEnterDialog?.Invoke();
        _hasInteract = true;
        _indexDialog = 0;
        ClearCoroutine();
        activeCoroutine = StartCoroutine(PlayDialog());
    }

    public override void TriggerExit(Collider other)
    {
        if (!_hasInteract)
            return;

        _hasInteract = false;
        base.TriggerExit(other);
        ClearCoroutine();
        activeCoroutine = StartCoroutine(Fade(_uiPanel, 1, 0));
    }

    private void ClearCoroutine()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
    }

    private void ShowDialog()
    {
        _currentDialog = dialogData[_indexDialog];
        _textUI.text = _currentDialog.textValue;
    }

    private IEnumerator PlayDialog()
    {
        ShowDialog();
        _textPanel.alpha = 1f;
        yield return Fade(_uiPanel, 0f, 1f);
        bool isInitial = true;

        foreach (var data in dialogData)
        {
            if (!isInitial)
            {
                yield return Fade(_textPanel, 0, 1, 0.25f);
            }

            isInitial = false;

            yield return new WaitForSeconds(_currentDialog.showDuration);

            _indexDialog++;

            if(_indexDialog >= dialogData.Length)
            {
                yield return Fade(_uiPanel, 1, 0);
                _hasInteract = false;
                OnInteractedDialog?.Invoke();
                yield break;
            }
            else
            {
                yield return Fade(_textPanel, 1, 0, 0.25f);
                ShowDialog();
            }
        }
    }
    
}

[System.Serializable]
public class DialogData
{
    public string textValue;
    public float showDuration;
}
