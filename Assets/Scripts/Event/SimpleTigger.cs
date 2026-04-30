using UnityEngine;

public class SimpleTrigger : MonoBehaviour
{
    [Tooltip("触发器的任务类型：1=山洞任务, 2=进入村庄, 3=出口任务")]
    public int targetTaskId = 1;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // 当玩家进入触发器，且尚未触发过
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;

            // 根据targetTaskId来区分场景和任务
            if (targetTaskId == 2)
            {
                // 村庄场景：任务2 - 进入村庄
                if (VillageSceneStory.Instance != null)
                {
                    VillageSceneStory.Instance.OnVillageEntranceReached();
                    return;
                }
            }
            else if (targetTaskId == 1)
            {
                // 山洞场景：任务1
                if (MainSceneStory.Instance != null)
                {
                    MainSceneStory.Instance.CompleteCaveTask();
                }
            }
            else if (targetTaskId == 3)
            {
                // 出口任务
                if (VillageSceneStory.Instance != null)
                {
                    VillageSceneStory.Instance.OnVillageExitReached();
                }
                else if (MainSceneStory.Instance != null)
                {
                    MainSceneStory.Instance.CompleteExitTask();
                }
            }
        }
    }
}
