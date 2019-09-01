namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public abstract class PhaseBase
    {
        protected CataclysmBossFightController Controller { get; }

        protected PhaseBase(CataclysmBossFightController controller)
        {
            Controller = controller;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public abstract BossPhase FixedUpdate();
    }
}