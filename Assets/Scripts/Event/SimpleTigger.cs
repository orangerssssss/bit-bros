using UnityEngine;

public class SimpleTrigger : MonoBehaviour
{
    [Tooltip("1=山洞任务, 3=出口任务（与你的 storyProcess 对应）")]
    public int targetTaskId = 1;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // 当玩家进入触发器，且尚未触发过
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;

            // 根据任务ID调用 MainSceneStory 中对应的回调
            if (targetTaskId == 1)
            {

                MainSceneStory.Instance.CompleteCaveTask();
            }
            else if (targetTaskId == 3)
            {
                MainSceneStory.Instance.CompleteExitTask();
            }
        }
    }
}