using UnityEngine;

public class EnemyTrigger : MonoBehaviour
{
    private BaseEnemyController _controller;

    public void Initialize(BaseEnemyController controller)
    {
        _controller = controller;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(_controller != null)
        {
            _controller.EnemyTriggerEnter(true);
        }
    }

    public void OnTriggerEvent()
    {
        _controller.EnemyTriggerEnter(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_controller != null)
        {
            _controller.EnemyTriggerEnter(false);
        }
    }
}
