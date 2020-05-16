namespace BrunoMikoski.SpriteAuditor
{
    public interface IProjectUpdateLoopListener
    {
        void OnProjectAwake();
        void OnProjectUpdate();
    }
}