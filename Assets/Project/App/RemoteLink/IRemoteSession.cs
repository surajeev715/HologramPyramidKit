//One side of the remote link: this device is either the host or a remote.
public interface IRemoteSession
{
    //Called once, when the libraries are loaded and connections can be opened.
    void OnReady();

    void HandleEvent(PeerEvent peerEvent, float now);

    //Called every frame after the events: timeouts and pings.
    void Tick(float now);

    //Called every frame after the local sensors wrote the context.
    void Apply(SensorContext context, float now);
}
