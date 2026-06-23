
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 敌人管理器
public class EnemyManager
{
    public static EnemyManager Instance = new EnemyManager();

    private List<Enemy> enemyList; // 存储战斗中的敌人

    // 保持 private，通过公共属性访问
    private string currentLevelId = "10001";

    // 公共只读属性，供外部访问
    public string CurrentLevelId
    {
        get { return currentLevelId; }
    }

    private int currentStageIndex = 0; // 当前是第几关（0-3）

    // 关卡流程配置：4个关卡，每个关卡可选的ID列表
    private string[][] levelFlow = new string[][]
    {
        new string[] { "10001" },                    // 第1关：固定
        new string[] { "10002", "10004" },           // 第2关：2选1随机
        new string[] { "10003", "10005", "10006" },  // 第3关：3选1随机
        new string[] { "10007" }                     // 第4关：固定（通关）
    };

    /// <summary>
    /// 初始化关卡流程（在战斗开始时调用）
    /// </summary>
    public void InitLevelFlow()
    {
        currentStageIndex = 0;
        currentLevelId = GetLevelIdForStage(0);
        Debug.Log($"[EnemyManager] 关卡流程初始化，第1关固定为: {currentLevelId}");
    }

    /// <summary>
    /// 根据关卡阶段获取关卡ID（支持随机）
    /// </summary>
    private string GetLevelIdForStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= levelFlow.Length)
        {
            Debug.LogError($"[EnemyManager] 关卡阶段索引越界: {stageIndex}");
            return "10001";
        }

        string[] options = levelFlow[stageIndex];
        if (options == null || options.Length == 0)
        {
            Debug.LogError($"[EnemyManager] 关卡 {stageIndex} 没有配置可选ID！");
            return "10001";
        }

        // 随机选择一个
        string selectedId = options[Random.Range(0, options.Length)];
        Debug.Log($"[EnemyManager] 第{stageIndex + 1}关 从 [{string.Join(",", options)}] 中随机选择: {selectedId}");
        return selectedId;
    }

    /// <summary>
    /// 加载指定关卡的敌人
    /// </summary>
    public void LoadRes(string id)
    {
        currentLevelId = id;
        enemyList = new List<Enemy>();

        // 1. 读取关卡配置表
        Dictionary<string, string> levelData = GameConfigManager.Instance.GetLevelById(id);
        if (levelData == null)
        {
            Debug.LogError($"[EnemyManager] 未找到关卡配置 ID: {id}");
            return;
        }

        // 2. 检查关键字段是否存在
        if (!levelData.ContainsKey("EnemyIds") || !levelData.ContainsKey("Pos"))
        {
            Debug.LogError($"[EnemyManager] 关卡 {id} 配置缺少 EnemyIds 或 Pos 字段！");
            return;
        }

        string[] enemyIds = levelData["EnemyIds"].Split('=');
        string[] enemyPos = levelData["Pos"].Split('=');

        // 3. 校验数组长度是否一致
        if (enemyIds.Length != enemyPos.Length)
        {
            Debug.LogError($"[EnemyManager] 关卡 {id} 配置错误：EnemyIds 数量 ({enemyIds.Length}) 与 Pos 数量 ({enemyPos.Length}) 不匹配！");
        }

        int count = Mathf.Min(enemyIds.Length, enemyPos.Length);

        for (int i = 0; i < count; i++)
        {
            string eId = enemyIds[i].Trim();
            string posStr = enemyPos[i].Trim();

            // 解析坐标
            string[] posArr = posStr.Split(',');
            if (posArr.Length < 3)
            {
                Debug.LogError($"[EnemyManager] 坐标格式错误: {posStr} (需要 x,y,z)");
                continue;
            }

            if (!float.TryParse(posArr[0], out float x) ||
                !float.TryParse(posArr[1], out float y) ||
                !float.TryParse(posArr[2], out float z))
            {
                Debug.LogError($"[EnemyManager] 坐标数值解析失败: {posStr}");
                continue;
            }

            Vector3 spawnPos = new Vector3(x, y, z);

            // 4. 自动防重叠逻辑
            if (i > 0 && enemyList.Count > 0)
            {
                Enemy lastEnemy = enemyList[enemyList.Count - 1];
                float distance = Vector3.Distance(lastEnemy.transform.position, spawnPos);

                if (distance < 1.5f)
                {
                    Vector3 direction = (spawnPos - lastEnemy.transform.position).normalized;
                    if (direction.magnitude < 0.1f)
                    {
                        direction = Vector3.forward;
                    }
                    spawnPos = lastEnemy.transform.position + direction * 2.0f;
                    Debug.LogWarning($"[EnemyManager] 检测到敌人 [{i}] 位置过近，已自动修正间距。");
                }
            }

            // 5. 读取单个敌人配置
            Dictionary<string, string> enemyData = GameConfigManager.Instance.GetEnemyById(eId);
            if (enemyData == null)
            {
                Debug.LogError($"[EnemyManager] 未找到敌人配置 ID: {eId}");
                continue;
            }

            // 6. 实例化模型
            string modelPath = enemyData["Model"];
            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError($"[EnemyManager] 敌人 {eId} 的 Model 路径为空！");
                continue;
            }

            Object prefabObj = Resources.Load(modelPath);
            if (prefabObj == null)
            {
                Debug.LogError($"[EnemyManager] 资源加载失败: {modelPath}");
                continue;
            }

            GameObject obj = Object.Instantiate(prefabObj) as GameObject;
            if (obj == null)
            {
                Debug.LogError($"[EnemyManager] 实例化对象失败: {modelPath}");
                continue;
            }

            // 7. 初始化组件
            Enemy enemy = obj.AddComponent<Enemy>();
            enemy.Init(enemyData);
            enemyList.Add(enemy);

            // 8. 设置最终位置
            obj.transform.position = spawnPos;
            obj.layer = LayerMask.NameToLayer("Enemy");
            obj.name = $"Enemy_{eId}_{i}";
        }

        Debug.Log($"[EnemyManager] 关卡 {id} (第{currentStageIndex + 1}关) 加载完成，实际生成敌人数量: {enemyList.Count}");
    }

    /// <summary>
    /// 移除单个敌人
    /// </summary>
    public void DeleteEnemy(Enemy enemy)
    {
        if (enemyList.Contains(enemy))
        {
            enemyList.Remove(enemy);
        }

        // 判断是否全灭
        if (enemyList.Count == 0)
        {
            Debug.Log("[EnemyManager] 所有敌人已消灭！检查下一关...");
            TryEnterNextLevel();
        }
    }

    /// <summary>
    /// 尝试进入下一关
    /// </summary>
    private void TryEnterNextLevel()
    {
        // 进入下一关阶段
        currentStageIndex++;

        // 检查是否已通关（4关全部完成）
        if (currentStageIndex >= levelFlow.Length)
        {
            Debug.Log("[游戏通关] 所有关卡已完成！恭喜胜利！");
            FightManager.Instance.ChangeType(FightType.Win);
            return;
        }

        // 获取下一关的关卡ID（支持随机）
        string nextLevelId = GetLevelIdForStage(currentStageIndex);

        Debug.Log($"[胜利] 准备进入第 {currentStageIndex + 1} 关: {nextLevelId}");
        FightManager.Instance.StartCoroutine(LoadNextLevelCoroutine(nextLevelId));
    }

    /// <summary>
    /// 协程：平滑过渡到下一关
    /// </summary>
  private IEnumerator LoadNextLevelCoroutine(string nextLevelId)
    {
        yield return new WaitForSeconds(1.5f);

        // 1. 清理场景残留敌人
        ClearAllEnemies();

        // 2. 重置玩家状态
        ResetPlayerState();

        // 3. 加载新关卡敌人
        LoadRes(nextLevelId);

        // 4. 提示 UI
        UIManager.Instance.ShowTip($"进入第 {currentStageIndex + 1} 关", Color.green, null);

        // 5. 切换到玩家回合
        FightManager.Instance.ChangeType(FightType.Player);
    }
 

    /// <summary>
    /// 清理场景中所有敌人对象
    /// </summary>
    private void ClearAllEnemies()
    {
#pragma warning disable CS0618
        Enemy[] allEnemies = Object.FindObjectsOfType<Enemy>();
#pragma warning restore CS0618
        foreach (var e in allEnemies)
        {
            Object.Destroy(e.gameObject);
        }
        enemyList.Clear();
    }

    /// <summary>
    /// 重置玩家状态
    /// </summary>
    private void ResetPlayerState()
    {
        var fm = FightManager.Instance;
        var ui = UIManager.Instance.GetUI<FightUI>("FightUI");

        Debug.Log($"[状态重置] 玩家保留血量: {fm.CurHp}/{fm.MaxHp}");

        fm.CurPowerCount = fm.MaxPowerCount;
        fm.DefenseCount = 0;
        FightCardManager.Instance.ResetForNextLevel();

        if (ui != null)
        {
            ui.RemoveAllCards();
            ui.UpdateHp();
            ui.UpdatePower();
            ui.UpdateDefense();
            ui.UpdateCardCount();
            ui.UpdateUsedCardCount();
        }
    }

    /// <summary>
    /// 执行所有存活敌人的行动
    /// </summary>
    public IEnumerator DoAllEnemyAction()
    {
        for (int i = 0; i < enemyList.Count; i++)
        {
            if (enemyList[i] != null)
            {
                yield return FightManager.Instance.StartCoroutine(enemyList[i].DoAction());
            }
        }

        for (int i = 0; i < enemyList.Count; i++)
        {
            if (enemyList[i] != null)
            {
                enemyList[i].SetRandomAction();
            }
        }

        FightManager.Instance.ChangeType(FightType.Player);
    }

    /// <summary>
    /// 获取当前关卡阶段（用于UI显示）
    /// </summary>
    public int GetCurrentStageIndex()
    {
        return currentStageIndex;
    }

    /// <summary>
    /// 获取总关卡数
    /// </summary>
    public int GetTotalStages()
    {
        return levelFlow.Length;
    }
}