using UnityEngine;

public sealed class PlayerSetupIntegration : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        if (playerPrefab == null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) return;

        playerObj = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerObj.tag = "Player";
        playerObj.name = "Player";

        if (playerObj.GetComponent<PlayerTopDownController>() == null)
            playerObj.AddComponent<PlayerTopDownController>();

        if (playerObj.GetComponent<PlayerCombatController>() == null)
            playerObj.AddComponent<PlayerCombatController>();

        if (playerObj.GetComponent<PlayerHealth>() == null)
            playerObj.AddComponent<PlayerHealth>();

        if (playerObj.GetComponent<PlayerSpellCaster>() == null)
            playerObj.AddComponent<PlayerSpellCaster>();

        if (playerObj.GetComponent<PlayerBuffReceiver>() == null)
            playerObj.AddComponent<PlayerBuffReceiver>();

        if (playerObj.GetComponent<VisibilitySystem>() == null)
            playerObj.AddComponent<VisibilitySystem>();

        if (playerObj.GetComponent<ResourceGatherer>() == null)
            playerObj.AddComponent<ResourceGatherer>();
    }
}