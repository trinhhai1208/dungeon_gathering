using DungeonGathering.Combat;

namespace DungeonGathering.Play
{
    /// <summary>
    /// FSM của Player trên khung dùng chung <see cref="StateMachine{TOwner}"/>.
    /// Luật toàn cục (ưu tiên giảm dần): chết → Dead · tắt input → Spawn · run đã chốt → Sealed · bị khống chế → Stunned ·
    /// đang lướt → Dash. Mỗi trạng thái tự về Active khi điều kiện của nó hết. Hành vi mỗi frame nằm ở
    /// <see cref="PlayerController.DriveFrame"/>; trạng thái chỉ quyết định có đọc input / cho đánh hay không.
    /// </summary>
    public static class PlayerStates
    {
        public static StateMachine<PlayerController> Build(PlayerController p)
        {
            StateMachine<PlayerController> fsm = null;
            fsm = new StateMachine<PlayerController>(p)
                .Add(new SpawnState())
                .Add(new ActiveState())
                .Add(new DashState())
                .Add(new SealedState())
                .Add(new StunnedState<PlayerController>(() => fsm, x => x.DriveFrame(readInput: false, allowAttack: false)))
                .Add(new DeadState<PlayerController>())
                .AddGlobalRule(x => x.IsDead, ActorStateId.Dead)
                .AddGlobalRule(x => !x.InputEnabled, ActorStateId.Spawn)
                .AddGlobalRule(x => !x.CombatEnabled, ActorStateId.Sealed, from: s => s != ActorStateId.Spawn)
                .AddGlobalRule(x => x.IsCrowdControlled, ActorStateId.Stunned, from: s => s == ActorStateId.Active || s == ActorStateId.Dash)
                .AddGlobalRule(x => x.Movement.IsDashing, ActorStateId.Dash, from: s => s == ActorStateId.Active);
            return fsm;
        }

        /// <summary>Trước khi bật input (hoặc input bị tắt): đứng yên, không đánh.</summary>
        sealed class SpawnState : IState<PlayerController>
        {
            public ActorStateId Id => ActorStateId.Spawn;
            public void Enter(PlayerController p) { }
            public void Tick(PlayerController p)
            {
                p.DriveFrame(readInput: false, allowAttack: false);
                if (p.InputEnabled) p.States.ChangeState(ActorStateId.Active);
            }
            public void Exit(PlayerController p) { }
        }

        /// <summary>Chơi bình thường: di chuyển, ngắm, đánh (skill / đổi vũ khí / tương tác qua sự kiện input).</summary>
        sealed class ActiveState : IState<PlayerController>
        {
            public ActorStateId Id => ActorStateId.Active;
            public void Enter(PlayerController p) { }
            public void Tick(PlayerController p) => p.DriveFrame(readInput: true, allowAttack: true);
            public void Exit(PlayerController p) { }
        }

        /// <summary>Đang lướt: khóa hướng + bất tử do PlayerMovement / PlayerStats lo; vẫn ngắm / đánh.</summary>
        sealed class DashState : IState<PlayerController>
        {
            public ActorStateId Id => ActorStateId.Dash;
            public void Enter(PlayerController p) { }
            public void Tick(PlayerController p)
            {
                p.DriveFrame(readInput: true, allowAttack: true);
                if (!p.Movement.IsDashing) p.States.ChangeState(ActorStateId.Active);
            }
            public void Exit(PlayerController p) { }
        }

        /// <summary>Run đã chốt thắng (combat tắt): đi lại tới Portal, không đánh / skill.</summary>
        sealed class SealedState : IState<PlayerController>
        {
            public ActorStateId Id => ActorStateId.Sealed;
            public void Enter(PlayerController p) => p.Weapons?.CancelAttack();
            public void Tick(PlayerController p)
            {
                p.DriveFrame(readInput: true, allowAttack: false);
                if (p.CombatEnabled) p.States.ChangeState(ActorStateId.Active);
            }
            public void Exit(PlayerController p) { }
        }
    }
}
