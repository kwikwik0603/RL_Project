using UnityEngine;

public class S_GameManager : MonoBehaviour
{
    [SerializeField] private Transform respawnLocation;
    [SerializeField] private GameObject player;
    private InputSystem_Actions actions;

    private void Awake()
    {
        actions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        actions.Player.Enable();
    }

    private void OnDisable()
    {
        actions.Player.Disable();
    }

    void Start()
    {
        
    }


    void Update()
    {

    }
}
