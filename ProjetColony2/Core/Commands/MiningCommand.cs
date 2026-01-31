using ProjetColony2.Core.Entities;

namespace ProjetColony2.Core.Commands;

public class MiningCommand : ICommand
{
    public int EntityId { get; }
    public int TargetX { get; }
    public int TargetY { get; }
    public int TargetZ { get; }

    public MiningCommand(int entityId, int targetX, int targetY, int targetZ)
    {
        EntityId = entityId;
        TargetX = targetX;
        TargetY = targetY;
        TargetZ = targetZ;
    }

    public void Execute(Entity entity)
    {
        entity.Intent.IsMining = true;
        entity.Intent.TargetBlockX = TargetX;
        entity.Intent.TargetBlockY = TargetY;
        entity.Intent.TargetBlockZ = TargetZ;
    }
}