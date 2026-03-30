using Fusion;

public struct PlayerInputData : INetworkInput
{
    public float moveX;
    public float moveY;
    public NetworkBool isShooting;
    public NetworkBool isShield;
}