using Shared;
using Shared.PlayerData;

namespace RiftOfTheNecroDancerPlaylists.Extension;

internal static class PlayerDataUtilExt
{
    // The original one give the same results as HasLevelBeenAttempted, it feels like a bug
    public static bool HasLevelBeenCompleted(string levelId)
    {
        PlayerDataUtil.TryGetLevelData(levelId, out LevelSaveData levelSaveData);
        return levelSaveData.HasCompletedOnDifficulty(Difficulty.Easy)
            || levelSaveData.HasCompletedOnDifficulty(Difficulty.Medium)
            || levelSaveData.HasCompletedOnDifficulty(Difficulty.Hard)
            || levelSaveData.HasCompletedOnDifficulty(Difficulty.Impossible);
    }

    public static bool HasLevelBeenCompletedOnDifficulty(string levelId, Difficulty difficulty)
    {
        PlayerDataUtil.TryGetLevelData(levelId, out LevelSaveData levelSaveData);
        return levelSaveData.HasCompletedOnDifficulty(difficulty);
    }
}
