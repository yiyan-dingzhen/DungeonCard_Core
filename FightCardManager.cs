
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 战斗卡牌管理器
public class FightCardManager
{
    public static FightCardManager Instance = new FightCardManager();

    public List<string> cardList;      // 手牌堆 (Draw Pile)
    public List<string> usedCardList;  // 弃牌堆 (Discard Pile)

    /// <summary>
    /// 初始化 (仅在战斗开始时调用)
    /// 从玩家配置加载初始卡组并洗牌
    /// </summary>
    public void Init()
    {
        cardList = new List<string>();
        usedCardList = new List<string>();

        List<string> tempList = new List<string>();

        // 获取玩家初始卡组 (假设 RoleManager 存在)
        if (RoleManager.Instance != null && RoleManager.Instance.cardList != null)
        {
            tempList.AddRange(RoleManager.Instance.cardList);
        }
        else
        {
            Debug.LogWarning("[FightCardManager] 未找到玩家卡组数据，使用空卡组测试。");
            // 测试用：如果没有数据，给几张测试卡
            tempList.Add("1001");
            tempList.Add("1002");
            tempList.Add("1001");
        }

        // 洗牌并填入 cardList
        Shuffle(tempList);
        cardList.AddRange(tempList);

        Debug.Log($"[FightCardManager] 战斗初始化完成。初始牌堆数量: {cardList.Count}");
    }

    /// <summary>
    /// 核心抽卡方法
    /// 若手牌堆为空，自动将弃牌堆洗回手牌堆
    /// </summary>
    public string DrawCard()
    {
        // 1. 检查手牌堆是否为空
        if (cardList.Count == 0)
        {
            Debug.LogWarning("[FightCardManager] 手牌堆为空，正在将弃牌堆洗牌并入...");

            if (usedCardList.Count == 0)
            {
                Debug.LogError("[FightCardManager] 手牌和弃牌堆都为空！无法抽卡！");
                return null;
            }

            // 2. 移回并清空
            cardList.AddRange(usedCardList);
            usedCardList.Clear();

            // 3. 重新洗牌
            Shuffle(cardList);

            Debug.Log($"[FightCardManager] 洗牌完成，新牌堆数量: {cardList.Count}");
        }

        // 4. 抽卡 (从末尾取)
        string id = cardList[cardList.Count - 1];
        cardList.RemoveAt(cardList.Count - 1);

        return id;
    }

    /// <summary>
    /// 将卡牌加入弃牌堆
    /// </summary>
    public void DiscardCard(string cardId)
    {
        if (!string.IsNullOrEmpty(cardId))
        {
            usedCardList.Add(cardId);
        }
    }

    /// <summary>
    /// 【关键】为下一关重置牌堆状态
    /// 逻辑：将当前弃牌堆强制洗回主牌堆，清空弃牌堆计数
    /// 注意：不重新加载初始卡组，保留当前卡组构成 (Roguelike 模式)
    /// </summary>
    public void ResetForNextLevel()
    {
        Debug.Log("[FightCardManager] >>> 正在为下一关重置牌堆...");

        if (usedCardList.Count > 0)
        {
            cardList.AddRange(usedCardList);
            usedCardList.Clear();
            Shuffle(cardList);
            Debug.Log($"[FightCardManager] 弃牌堆已洗入主牌堆。当前主牌堆数量: {cardList.Count}");
        }
        else
        {
            Debug.Log("[FightCardManager] 弃牌堆为空，无需操作。");
        }
    }

    public int GetUsedCardCount() => usedCardList.Count;
    public int GetCardListCount() => cardList.Count;

    // Fisher-Yates 洗牌算法
    private void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            string temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}